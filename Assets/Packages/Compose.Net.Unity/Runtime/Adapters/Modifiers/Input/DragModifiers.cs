using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace Compose.Net.Unity;

internal record DraggableModifierImpl(IDraggableState State, Orientation Orientation, bool Enabled,
    IMutableInteractionSource? InteractionSource, bool StartDragImmediately, Action<Offset>? OnDragStarted,
    Action<float>? OnDragStopped, bool ReverseDirection) : ModifierNodeElement<GestureModifierNode>
{
    public override GestureModifierNode Create() => new(this, null);
    public override void Update(GestureModifierNode node) => node.Update(this, null);
    public override object GetTarget(IReusableComposeNode node) => node.VisualElement();
}

internal record ScrollableModifierImpl(IScrollableState State, Orientation Orientation, bool Enabled,
    bool ReverseDirection, FlingBehavior? FlingBehavior, IMutableInteractionSource? InteractionSource)
    : ModifierNodeElement<GestureModifierNode>
{
    public override GestureModifierNode Create() => new(null, this);
    public override void Update(GestureModifierNode node) => node.Update(null, this);
    public override object GetTarget(IReusableComposeNode node) => node.VisualElement();
}

internal sealed class GestureModifierNode(DraggableModifierImpl? drag, ScrollableModifierImpl? scroll) : IModifier.Node
{
    private AxisGestureManipulator? _manipulator;
    public override void OnAttach()
    {
        var element = (VisualElement)Target;
        if (drag?.Enabled ?? scroll!.Enabled) element.PickingMode().Increment();
        _manipulator = new AxisGestureManipulator(drag, scroll);
        element.AddManipulator(_manipulator);
    }
    internal void Update(DraggableModifierImpl? nextDrag, ScrollableModifierImpl? nextScroll)
    {
        var wasEnabled = drag?.Enabled ?? scroll!.Enabled;
        var enabled = nextDrag?.Enabled ?? nextScroll!.Enabled;
        if (_manipulator != null && wasEnabled != enabled)
        {
            var element = (VisualElement)Target;
            if (enabled) element.PickingMode().Increment(); else element.PickingMode().Decrement();
        }
        drag = nextDrag; scroll = nextScroll;
        _manipulator?.Update(drag, scroll);
    }
    public override void OnDetach()
    {
        var element = (VisualElement)Target;
        if (_manipulator != null) element.RemoveManipulator(_manipulator);
        if (drag?.Enabled ?? scroll!.Enabled) element.PickingMode().Decrement();
        _manipulator = null;
    }
}

// A pooled, bubbling event lets ancestors consume the unconsumed drag/fling distance.
// It uses UI Toolkit's event route and lifetime rather than a separate input dispatcher.
internal sealed class NestedScrollDeltaEvent : EventBase<NestedScrollDeltaEvent>
{
    public float Remaining;
    public Orientation Orientation;
    protected override void Init() { base.Init(); bubbles = true; tricklesDown = false; Remaining = 0; }
}

internal sealed class AxisGestureManipulator : PointerManipulator
{
    private DraggableModifierImpl? _drag;
    private ScrollableModifierImpl? _scroll;
    private int _pointer = -1;
    private Vector2 _down, _last;
    private float _slop;
    private bool _active, _claiming, _ending;
    private PointerCaptureOutEvent? _pressCaptureOut;
    private VisualElement? _root, _capturedChild;
    private IDragInteraction.Start? _interaction;
    private IMutableInteractionSource? _interactionSource;
    private IDragScope? _dragScope;
    private IScrollScope? _scrollScope;
    private CancellationTokenSource? _mutation;
    private TaskCompletionSource<bool>? _finished;
    private ValueAnimation<float>? _fling;
    private readonly Queue<(long time, float position)> _samples = new();
    private readonly Dictionary<int, Vector2> _touches = new();
    private float _samplePosition;
    private static readonly float WheelSize = new ScrollView().mouseWheelScrollSize;
    private Orientation Axis => _drag?.Orientation ?? _scroll!.Orientation;
    private bool Enabled => (_drag?.Enabled ?? _scroll!.Enabled) && target.enabledInHierarchy;
    private float Sign => (_drag?.ReverseDirection ?? _scroll!.ReverseDirection) ? -1 : 1;
    private float Component(Vector2 point) => Axis == Orientation.Horizontal ? point.x : point.y;
    private float Cross(Vector2 point) => Axis == Orientation.Horizontal ? point.y : point.x;

    internal AxisGestureManipulator(DraggableModifierImpl? drag, ScrollableModifierImpl? scroll)
    { _drag = drag; _scroll = scroll; activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse }); }

    internal void Update(DraggableModifierImpl? drag, ScrollableModifierImpl? scroll)
    {
        // Callback/source replacement does not restart a gesture. Configuration and state changes do.
        if (!ReferenceEquals(_drag?.State, drag?.State) || !ReferenceEquals(_scroll?.State, scroll?.State) ||
            (_drag?.Orientation ?? _scroll!.Orientation) != (drag?.Orientation ?? scroll!.Orientation) ||
            (_drag?.ReverseDirection ?? _scroll!.ReverseDirection) != (drag?.ReverseDirection ?? scroll!.ReverseDirection) ||
            !((drag?.Enabled ?? scroll!.Enabled))) Cancel();
        if (!ReferenceEquals(_drag?.InteractionSource ?? _scroll?.InteractionSource, drag?.InteractionSource ?? scroll?.InteractionSource))
            EndInteraction(true);
        _drag = drag; _scroll = scroll;
    }

    protected override void RegisterCallbacksOnTarget()
    {
        target.RegisterCallback<PointerDownEvent>(OnDown, TrickleDown.TrickleDown);
        target.RegisterCallback<PointerMoveEvent>(OnActiveMove, TrickleDown.TrickleDown);
        target.RegisterCallback<PointerMoveEvent>(OnMove);
        target.RegisterCallback<PointerUpEvent>(OnUp, TrickleDown.TrickleDown);
        target.RegisterCallback<PointerCancelEvent>(OnCancel);
        target.RegisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
        target.RegisterCallback<PointerCaptureEvent>(OnCapture);
        target.RegisterCallback<DetachFromPanelEvent>(OnDetach);
        target.RegisterCallback<WheelEvent>(OnWheel);
        target.RegisterCallback<KeyDownEvent>(OnKey);
        target.RegisterCallback<NestedScrollDeltaEvent>(OnNestedScroll);
    }
    protected override void UnregisterCallbacksFromTarget()
    {
        Cancel();
        target.UnregisterCallback<PointerDownEvent>(OnDown, TrickleDown.TrickleDown);
        target.UnregisterCallback<PointerMoveEvent>(OnActiveMove, TrickleDown.TrickleDown);
        target.UnregisterCallback<PointerMoveEvent>(OnMove);
        target.UnregisterCallback<PointerUpEvent>(OnUp, TrickleDown.TrickleDown);
        target.UnregisterCallback<PointerCancelEvent>(OnCancel);
        target.UnregisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
        target.UnregisterCallback<PointerCaptureEvent>(OnCapture);
        target.UnregisterCallback<DetachFromPanelEvent>(OnDetach);
        target.UnregisterCallback<WheelEvent>(OnWheel);
        target.UnregisterCallback<KeyDownEvent>(OnKey);
        target.UnregisterCallback<NestedScrollDeltaEvent>(OnNestedScroll);
    }
    private void OnDown(PointerDownEvent evt)
    {
        if (!Enabled || evt.button != 0) return;
        if (evt.pointerType == UnityEngine.UIElements.PointerType.touch) _touches[evt.pointerId] = evt.position;
        if (_pointer >= 0) return;
        // Compose scrollable handles touch/stylus dragging; a mouse scrolls with its wheel.
        if (_scroll != null && evt.pointerType == UnityEngine.UIElements.PointerType.mouse) return;
        var catchFling = _fling != null;
        FinishMutation();
        _pointer = evt.pointerId;
        _down = _last = evt.position;
        _slop = evt.pointerType == UnityEngine.UIElements.PointerType.mouse ? .5f : 8f;
        _samplePosition = 0; _samples.Clear(); AddSample(evt.timestamp, 0);
        _root = target.panel?.visualTree;
        _root?.RegisterCallback<PointerMoveEvent>(OnOutsideMove);
        _root?.RegisterCallback<PointerUpEvent>(OnOutsideUp);
        if (_drag?.StartDragImmediately == true || catchFling)
        {
            Begin(evt, evt.localPosition);
            if (_active) evt.StopImmediatePropagation();
        }
    }
    private void OnOutsideMove(PointerMoveEvent evt)
    {
        if (evt.target is VisualElement element && (ReferenceEquals(element, target) || target.Contains(element))) return;
        OnMove(evt);
    }
    private void OnOutsideUp(PointerUpEvent evt) { if (evt.pointerId == _pointer) OnUp(evt); }
    private void OnActiveMove(PointerMoveEvent evt) { if (_active) OnMove(evt); }
    private void OnMove(PointerMoveEvent evt)
    {
        if (_touches.ContainsKey(evt.pointerId)) _touches[evt.pointerId] = evt.position;
        if (evt.pointerId != _pointer || _pointer < 0 || evt.isPropagationStopped) return;
        if (!Enabled || (evt.pressedButtons & 1) == 0) { Cancel(); return; }
        Vector2 position = evt.position;
        var delta = Component(target.WorldToLocal(position) - target.WorldToLocal(_last));
        _samplePosition += delta;
        AddSample(evt.timestamp, _samplePosition);
        _last = position;
        if (!_active)
        {
            var localDistance = target.WorldToLocal(position) - target.WorldToLocal(_down);
            var distance = Component(localDistance);
            if (Mathf.Abs(Cross(localDistance)) > _slop && Mathf.Abs(Cross(localDistance)) > Mathf.Abs(distance))
            { ResetPointer(); return; }
            if (Mathf.Abs(distance) <= _slop) return;
            Begin(evt, target.WorldToLocal(position));
            if (!_active) return;
            delta = distance - Mathf.Sign(distance) * _slop;
        }
        ApplyDelta(delta);
        evt.StopImmediatePropagation();
    }
    private void Begin(IPointerEvent evt, Vector2 position)
    {
        _claiming = true;
        try
        {
            // Cancel press detectors on the same target before claiming the pointer.
            // CapturePointer itself cancels detectors attached to a different target.
            if (target.HasPointerCapture(_pointer))
            {
                using var cancel = PointerCaptureOutEvent.GetPooled(target, target, _pointer);
                _pressCaptureOut = cancel;
                cancel.target = target; target.SendEvent(cancel);
            }
            StartMutation();
            if (_dragScope == null && _scrollScope == null) { FinishMutation(); ResetPointer(); return; }
            _active = true;
            target.CapturePointer(_pointer);
            _interactionSource = _drag?.InteractionSource ?? _scroll?.InteractionSource;
            _interaction = new IDragInteraction.Start();
            _interactionSource?.TryEmit(_interaction);
            _drag?.OnDragStarted?.Invoke(new Offset(position.x, position.y));
        }
        finally { _claiming = false; }
    }
    private void OnUp(PointerUpEvent evt)
    {
        _touches.Remove(evt.pointerId);
        if (evt.pointerId != _pointer || evt.button != 0) return;
        if (_touches.Count > 0)
        {
            var old = _pointer;
            foreach (var touch in _touches) { _pointer = touch.Key; _down = _last = touch.Value; break; }
            _samples.Clear(); _samplePosition = 0; AddSample(evt.timestamp, 0);
            if (_active && target.HasPointerCapture(old)) target.ReleasePointer(old);
            if (_active) { target.CapturePointer(_pointer); evt.StopImmediatePropagation(); }
            return;
        }
        var active = _active;
        var velocity = Velocity(evt.timestamp);
        ResetPointer();
        if (!active) return;
        EndInteraction(false);
        _drag?.OnDragStopped?.Invoke(velocity * Sign);
        if (_scroll != null && Enabled) StartFling(velocity); else FinishMutation();
        evt.StopImmediatePropagation();
    }
    private void OnCancel(PointerCancelEvent evt) { if (!_claiming && evt.pointerId == _pointer) Cancel(); }
    private void OnCapture(PointerCaptureEvent evt)
    {
        if (_pointer != evt.pointerId || _active || evt.target is not VisualElement captured || ReferenceEquals(captured, target)) return;
        UnwatchCapturedChild();
        _capturedChild = captured;
        // Like ScrollView, observe a child's captured stream until touch slop is crossed.
        // Captured pointer events are dispatched directly and do not bubble to ancestors.
        captured.RegisterCallback<PointerMoveEvent>(OnMove);
        captured.RegisterCallback<PointerUpEvent>(OnUp);
    }
    private void OnCaptureOut(PointerCaptureOutEvent evt)
    {
        // This notification cancels co-located press detectors, not our newly claimed drag.
        // PointerCancelEvent cannot be used here: its native post-dispatch releases touch capture.
        if (ReferenceEquals(evt, _pressCaptureOut)) { _pressCaptureOut = null; return; }
        if (ReferenceEquals(evt.target, _capturedChild)) UnwatchCapturedChild();
        if (!_claiming && _active && ReferenceEquals(evt.target, target) && evt.pointerId == _pointer) Cancel();
    }
    private void UnwatchCapturedChild()
    {
        _capturedChild?.UnregisterCallback<PointerMoveEvent>(OnMove);
        _capturedChild?.UnregisterCallback<PointerUpEvent>(OnUp);
        _capturedChild = null;
    }
    private void OnDetach(DetachFromPanelEvent evt) => Cancel();
    private void Cancel()
    {
        if (_ending) return;
        _ending = true;
        try
        {
            var active = _active;
            ResetPointer(); EndInteraction(true); FinishMutation();
            if (active) _drag?.OnDragStopped?.Invoke(0);
        }
        finally { _ending = false; }
    }
    private void ResetPointer()
    {
        UnwatchCapturedChild(); _touches.Clear();
        var pointer = _pointer; var ownedCapture = _active; _pointer = -1; _active = false;
        _root?.UnregisterCallback<PointerMoveEvent>(OnOutsideMove);
        _root?.UnregisterCallback<PointerUpEvent>(OnOutsideUp); _root = null;
        if (ownedCapture && pointer >= 0 && target.HasPointerCapture(pointer)) target.ReleasePointer(pointer);
    }
    private void EndInteraction(bool cancel)
    {
        var interaction = _interaction; _interaction = null;
        if (interaction != null) _interactionSource?.TryEmit(cancel
            ? new IDragInteraction.Cancel(interaction) : new IDragInteraction.Stop(interaction));
        _interactionSource = null;
    }
    private void StartMutation()
    {
        FinishMutation();
        var source = new CancellationTokenSource(); _mutation = source;
        var finished = new TaskCompletionSource<bool>(); _finished = finished;
        ObserveMutation(source, finished);
    }
    private async void ObserveMutation(CancellationTokenSource source, TaskCompletionSource<bool> finished)
    {
        try
        {
            if (_drag != null) await _drag.State.Drag(async scope =>
            {
                if (!ReferenceEquals(_mutation, source)) throw new OperationCanceledException();
                _dragScope = scope;
                using (scope.Token.Register(() => { finished.TrySetCanceled(); if (ReferenceEquals(_mutation, source)) Cancel(); }))
                    await finished.Task;
            }, MutatePriority.UserInput, source.Token);
            else await _scroll!.State.Scroll(async scope =>
            {
                if (!ReferenceEquals(_mutation, source)) throw new OperationCanceledException();
                _scrollScope = scope;
                using (scope.Token.Register(() => { finished.TrySetCanceled(); if (ReferenceEquals(_mutation, source)) Cancel(); }))
                    await finished.Task;
            }, MutatePriority.UserInput, source.Token);
        }
        catch (OperationCanceledException) { }
        catch (Exception error) { Debug.LogException(error); }
        finally
        {
            if (ReferenceEquals(_mutation, source)) { _mutation = null; _dragScope = null; _scrollScope = null; }
            source.Dispose();
        }
    }
    private void FinishMutation()
    {
        var fling = _fling; _fling = null;
        if (fling != null) { fling.onAnimationCompleted = null; fling.Stop(); }
        var finished = _finished; _finished = null;
        var source = _mutation;
        _mutation = null; _dragScope = null; _scrollScope = null;
        source?.Cancel();
        finished?.TrySetResult(true);
    }
    private float ApplyDelta(float delta)
    {
        try
        {
            if (_dragScope != null) { _dragScope.DragBy(delta * Sign); return delta; }
            if (_scrollScope == null) return 0;
            var consumed = _scrollScope.ScrollBy(delta * Sign) * Sign;
            var remainder = delta - consumed;
            if (Mathf.Abs(remainder) > .001f && target.parent != null)
            {
                using var nested = NestedScrollDeltaEvent.GetPooled();
                nested.Orientation = Axis; nested.Remaining = remainder; nested.target = target.parent;
                target.parent.SendEvent(nested);
                consumed += remainder - nested.Remaining;
            }
            return consumed;
        }
        catch (OperationCanceledException) { Cancel(); return 0; }
    }
    private void OnNestedScroll(NestedScrollDeltaEvent evt)
    {
        if (_scroll == null || !Enabled || evt.Orientation != Axis) return;
        var consumed = ConsumeOnce(evt.Remaining);
        evt.Remaining -= consumed;
        if (Mathf.Abs(evt.Remaining) < .001f) evt.StopPropagation();
    }
    private float ConsumeOnce(float delta)
    {
        if (_scrollScope != null) return _scrollScope.ScrollBy(delta * Sign) * Sign;
        var consumed = 0f;
        using var attempt = new CancellationTokenSource();
        var task = _scroll!.State.Scroll(scope =>
        { consumed = scope.ScrollBy(delta * Sign) * Sign; return Task.CompletedTask; }, MutatePriority.UserInput, attempt.Token);
        // Native event propagation is synchronous. Never consume this same input later,
        // after an ancestor has already received the unconsumed event.
        if (!task.IsCompleted) attempt.Cancel();
        ObserveInput(task);
        return consumed;
    }
    private static async void ObserveInput(Task task)
    { try { await task; } catch (OperationCanceledException) { } catch (Exception error) { Debug.LogException(error); } }
    private void OnWheel(WheelEvent evt)
    {
        if (_scroll == null || !Enabled) return;
        var wheel = Axis == Orientation.Horizontal ? (evt.delta.x != 0 ? evt.delta.x : evt.delta.y) : evt.delta.y;
        if (wheel == 0) return;
        Cancel();
        // Wheel direction is opposite to pointer travel, as in Compose scrollable.
        var requested = -wheel * WheelSize;
        var consumed = ConsumeOnce(requested);
        if (Mathf.Abs(consumed) < .001f) return; // Native bubbling reaches a scrollable ancestor.
        var remaining = requested - consumed;
        if (Mathf.Abs(remaining) > .001f && target.parent != null)
        {
            var delta = evt.delta;
            if (Axis == Orientation.Horizontal && delta.x != 0) delta.x = -remaining / WheelSize;
            else delta.y = -remaining / WheelSize;
            var imgui = new Event { type = EventType.ScrollWheel, delta = delta, mousePosition = evt.mousePosition, modifiers = evt.modifiers };
            using var forwarded = WheelEvent.GetPooled(imgui);
            forwarded.target = target.parent; target.parent.SendEvent(forwarded);
        }
        evt.StopPropagation();
    }
    private void OnKey(KeyDownEvent evt)
    {
        if (_scroll == null || !Enabled || evt.altKey || evt.ctrlKey || evt.commandKey) return;
        if (evt.keyCode != KeyCode.PageDown && evt.keyCode != KeyCode.PageUp) return;
        var distance = Component(target.layout.size) * (evt.keyCode == KeyCode.PageDown ? -1 : 1);
        Cancel();
        if (ConsumeOnce(distance) != 0) evt.StopPropagation();
    }
    private void StartFling(float velocity)
    {
        var rate = _scroll?.FlingBehavior?.DecelerationRate ?? .135f;
        if (rate == 0 || Mathf.Abs(velocity) < 1) { FinishMutation(); return; }
        var decay = -Mathf.Log(rate);
        var duration = Mathf.Min(8, Mathf.Log(Mathf.Abs(velocity)) / decay);
        var tail = Mathf.Exp(-decay * duration);
        var distance = velocity * (1 - tail) / decay;
        var previous = 0f;
        _fling = target.experimental.animation.Start(0f, distance, Mathf.CeilToInt(duration * 1000), (_, value) =>
        {
            if (!Enabled || target.panel == null) { Cancel(); return; }
            var delta = value - previous; previous = value;
            if (Mathf.Abs(ApplyDelta(delta) - delta) > .1f) FinishMutation();
        }).Ease(t => (1 - Mathf.Exp(-decay * duration * t)) / (1 - tail));
        _fling.OnCompleted(FinishMutation);
    }
    private void AddSample(long time, float position)
    {
        _samples.Enqueue((time, position));
        while (_samples.Count > 2 && time - _samples.Peek().time > 100) _samples.Dequeue();
    }
    private float Velocity(long time)
    {
        if (_samples.Count < 2) return 0;
        var first = _samples.Peek(); var last = first;
        foreach (var sample in _samples) last = sample;
        if (time - last.time > 100 || last.time <= first.time) return 0;
        return Mathf.Clamp((last.position - first.position) * 1000f / (last.time - first.time), -10000, 10000);
    }
}
