using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Compose.Net;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;
using static Compose.Net.ComposeFunctions;
using Color = Compose.Net.Color;

namespace Compose.Net.Unity.Tests;

public abstract partial class GestureModifierFixture : UITestFixture
{
    protected readonly bool _scroll, _horizontal;
    protected ComposeView _view = null!;
    protected IMutableState<bool> _enabled = null!, _reverse = null!, _visible = null!, _present = null!, _immediate = null!;
    protected IMutableState<int> _revision = null!, _factor = null!;
    protected IMutableState<IMutableInteractionSource> _source = null!;
    protected IDraggableState _drag = null!;
    protected IScrollableState _state = null!;
    protected IScrollState _outerState = null!;
    protected CancellationTokenSource _collect = null!;
    protected readonly List<IInteraction> _events = new();
    protected readonly List<float> _stops = new();
    protected readonly List<Offset> _starts = new();
    protected float _value, _parentValue, _limit;
    protected bool _nested, _clickable, _childClick, _nativeParent, _fling;
    protected int _clicks;
    protected SynchronizationContext? _previous;
    protected readonly TestUiContext _context = new();
    protected Orientation Axis => _horizontal ? Orientation.Horizontal : Orientation.Vertical;
    protected Vector2 Along(float delta) => _horizontal ? new Vector2(delta, 0) : new Vector2(0, delta);
    protected VisualElement Target => _view.Q("target");
    protected int Pointer => _scroll ? UnityEngine.UIElements.PointerId.touchPointerIdBase : UnityEngine.UIElements.PointerId.mousePointerId;
    protected GestureModifierFixture(bool scroll, bool horizontal) { _scroll = scroll; _horizontal = horizontal; }
    [SetUp] public void Setup()
    {
        RecreatePanel(); panelSize = new Vector2(700, 500); simulate.timePerSimulatedFrameMs = 16;
        _previous = SynchronizationContext.Current; SynchronizationContext.SetSynchronizationContext(_context);
        _view = new ComposeView(); _view.style.flexGrow = 1; rootVisualElement.Add(_view);
        _enabled = MutableStateOf(true); _reverse = MutableStateOf(false); _visible = MutableStateOf(true);
        _present = MutableStateOf(true); _immediate = MutableStateOf(false); _revision = MutableStateOf(0); _factor = MutableStateOf(1);
        _source = MutableStateOf<IMutableInteractionSource>(MutableInteractionSource());
        _outerState = new ScrollState();
        _value = _parentValue = 0; _limit = float.PositiveInfinity; _clicks = 0;
        _nested = _clickable = _childClick = _nativeParent = _fling = false;
        _events.Clear(); _stops.Clear(); _starts.Clear();
        _collect = new CancellationTokenSource(); _ = _source.Value.Interactions.Collect(_events.Add, _collect.Token);
        simulate.MouseMove(new Vector2(680, 480));
    }
    [TearDown] public void Teardown()
    {
        _view.Dispose(); _context.Drain(); _collect.Cancel(); _collect.Dispose();
        SynchronizationContext.SetSynchronizationContext(_previous); simulate.ResetTimePerSimulatedFrameToDefault();
    }
    [Composable] private void Content()
    {
        var unused = _revision.Value; var factor = _factor.Value;
        _drag = RememberDraggableState(delta => _value += delta * factor);
        _state = RememberScrollableState(delta => { var used = Math.Clamp(_value + delta * factor, -_limit, _limit) - _value; _value += used; return used / factor; });
        Action<Offset> started = position => _starts.Add(position);
        Action<float> stopped = speed => _stops.Add(speed * factor);
        var parent = RememberScrollableState(delta => { _parentValue += delta; return delta; });
        var outer = Modifier.Size(450.Dp(), 350.Dp()).TestTag("outer");
        if (_nativeParent) outer = _horizontal ? outer.HorizontalScroll(_outerState, overscrollEffect: null) : outer.VerticalScroll(_outerState, overscrollEffect: null);
        if (_nested) outer = outer.Scrollable(parent, Axis, flingBehavior: new FlingBehavior(0));
        Column(modifier: outer, content: () =>
        {
            if (!_visible.Value) return;
            var modifier = Modifier.Size(240.Dp(), 160.Dp()).TestTag("target").Background(Color.DodgerBlue);
            if (_clickable) modifier = modifier.Clickable(() => _clicks++);
            if (_present.Value) modifier = _scroll
                ? modifier.Scrollable(_state, Axis, _enabled.Value, _reverse.Value, new FlingBehavior(_fling ? .135f : 0), _source.Value)
                : modifier.Draggable(_drag, Axis, _enabled.Value, _source.Value, _immediate.Value,
                    started, stopped, _reverse.Value);
            Column(modifier: modifier, content: () =>
            {
                Text("child", modifier: _childClick ? Modifier.Size(120.Dp(), 45.Dp()).TestTag("child").Clickable(() => _clicks++) : Modifier.TestTag("child"));
            });
            if (_nativeParent) Box(modifier: Modifier.Size(900.Dp(), 700.Dp()), content: () => { });
        });
    }
    protected IEnumerator Mount()
    {
        _view.SetContent(Content); yield return Settle(); Assert.That(Target.layout.width, Is.GreaterThan(100));
    }
    protected void Flush(int milliseconds = 16)
    {
        _context.Drain(); ComposeInvalidatorHolder.ComposeInvalidator.Tick(); simulate.FrameUpdateMs(milliseconds); _context.Drain();
    }
    protected IEnumerator Settle() { for (var i = 0; i < 5; i++) { Flush(); yield return null; } }
    protected IEnumerator Wait(Func<bool> predicate)
    {
        var deadline = DateTime.UtcNow.AddSeconds(3);
        while (!predicate() && DateTime.UtcNow < deadline) { Flush(); yield return null; }
        Assert.That(predicate(), Is.True, "Timed out: " + string.Join(", ", _events));
    }
    protected void Down(Vector2 position) { if (_scroll) Touch(TouchPhase.Began, position); else simulate.MouseDown(position); Flush(); }
    protected void Move(Vector2 position) { if (_scroll) Touch(TouchPhase.Moved, position); else simulate.MouseMove(position); Flush(); }
    protected void Up(Vector2 position) { if (_scroll) Touch(TouchPhase.Ended, position); else simulate.MouseUp(position); Flush(); }
    protected void Touch(TouchPhase phase, Vector2 position) => TouchFinger(phase, position, 0);
    protected void TouchFinger(TouchPhase phase, Vector2 position, int finger)
    {
        var touch = new Touch { fingerId = finger, position = position, phase = phase, pressure = 1 };
        EventBase evt = phase == TouchPhase.Began ? PointerDownEvent.GetPooled(touch, EventModifiers.None)
            : phase == TouchPhase.Moved ? PointerMoveEvent.GetPooled(touch, EventModifiers.None) : PointerUpEvent.GetPooled(touch, EventModifiers.None);
        using (evt) { var element = Target != null && Target.HasPointerCapture(UnityEngine.UIElements.PointerId.touchPointerIdBase + finger) ? Target : rootVisualElement.panel.Pick(position); evt.target = element; element.SendEvent(evt); }
    }
    protected IDragInteraction.Start[] Starts => _events.OfType<IDragInteraction.Start>().ToArray();
    protected IDragInteraction.Stop[] Stops => _events.OfType<IDragInteraction.Stop>().ToArray();
    protected IDragInteraction.Cancel[] Cancels => _events.OfType<IDragInteraction.Cancel>().ToArray();

    [UnityTest] public IEnumerator AxisDeltaAndSlopDoNotMoveOrClipContent()
    {
        yield return Mount(); var bounds = Target.worldBound; var child = _view.Q("child").worldBound;
        var point = bounds.center; Down(point); Move(point + Along(40)); Up(point + Along(40));
        Assert.That(_value, Is.EqualTo(40 - (_scroll ? 8 : .5f)).Within(.1));
        Assert.That(Target.worldBound, Is.EqualTo(bounds)); Assert.That(_view.Q("child").worldBound, Is.EqualTo(child));
        Assert.That(Target.Q<ScrollView>(), Is.Null); Assert.That(Target.resolvedStyle.overflow, Is.EqualTo(Overflow.Visible));
        yield return Wait(() => Stops.Length == 1); Assert.That(Stops[0].Start, Is.SameAs(Starts.Single()));
        Assert.That(Cancels, Is.Empty); Assert.That(Target.HasPointerCapture(Pointer), Is.False);
    }
    [UnityTest] public IEnumerator ReverseChangesOnlyInputDirection()
    {
        _reverse.Value = true; yield return Mount(); var point = Target.worldBound.center;
        Down(point); Move(point + Along(30)); Up(point + Along(30)); Assert.That(_value, Is.LessThan(0));
        var before = _value; if (_scroll) _state.DispatchRawDelta(5); else _drag.DispatchRawDelta(5);
        Assert.That(_value, Is.EqualTo(before + 5)); if (!_scroll) Assert.That(_stops.Single(), Is.LessThan(0));
    }
    [UnityTest] public IEnumerator BelowSlopDoesNotStartGesture()
    {
        yield return Mount(); var point = Target.worldBound.center;
        Down(point); Move(point + Along(_scroll ? 4 : .2f)); Up(point);
        yield return Settle(); Assert.That(_value, Is.Zero); Assert.That(Starts, Is.Empty);
    }
    [UnityTest] public IEnumerator PerpendicularMovementDoesNotStartGesture()
    {
        yield return Mount(); var point = Target.worldBound.center;
        Down(point); Move(point + (_horizontal ? new Vector2(0, 35) : new Vector2(35, 0))); Up(point);
        yield return Settle(); Assert.That(_value, Is.Zero); Assert.That(Starts, Is.Empty);
    }
    [UnityTest] public IEnumerator DisabledIgnoresInputButStateStillWorks()
    {
        _enabled.Value = false; yield return Mount(); var point = Target.worldBound.center;
        Down(point); Move(point + Along(40)); Up(point); yield return Settle(); Assert.That(_value, Is.Zero); Assert.That(Starts, Is.Empty);
        if (_scroll) _state.DispatchRawDelta(12); else _drag.DispatchRawDelta(12);
        Assert.That(_value, Is.EqualTo(12));
        _enabled.Value = true; yield return Settle(); Down(point); Move(point + Along(40)); Up(point); Assert.That(_value, Is.GreaterThan(12));
    }
    [UnityTest] public IEnumerator DisableActiveGestureCancelsExactlyOnce()
    {
        yield return Mount(); var point = Target.worldBound.center;
        Down(point); Move(point + Along(30)); _enabled.Value = false; yield return Settle();
        yield return Wait(() => Cancels.Length == 1); var before = _value;
        Move(point + Along(60)); Up(point + Along(60)); yield return Settle();
        Assert.That(_value, Is.EqualTo(before)); Assert.That(Cancels[0].Start, Is.SameAs(Starts.Single())); Assert.That(Stops, Is.Empty);
        if (!_scroll) Assert.That(_stops, Is.EqualTo(new[] { 0f })); else Assert.That(_state.IsScrollInProgress, Is.False);
    }
    [UnityTest] public IEnumerator ModifierRemovalCancelsAndReleasesCapture()
    {
        yield return Mount(); var point = Target.worldBound.center;
        Down(point); Move(point + Along(30)); _present.Value = false; yield return Settle();
        yield return Wait(() => Cancels.Length == 1); Assert.That(Target.HasPointerCapture(Pointer), Is.False);
    }
    [UnityTest] public IEnumerator ElementRemovalCancelsAndReleasesCapture()
    {
        yield return Mount(); var target = Target; var point = target.worldBound.center;
        Down(point); Move(point + Along(30)); _visible.Value = false; yield return Settle();
        yield return Wait(() => Cancels.Length == 1); Assert.That(target.HasPointerCapture(Pointer), Is.False);
    }
    [UnityTest] public IEnumerator PanelDetachCancelsGesture()
    {
        yield return Mount(); var point = Target.worldBound.center;
        Down(point); Move(point + Along(30)); _view.RemoveFromHierarchy(); yield return Settle();
        yield return Wait(() => Cancels.Length == 1);
    }
    [UnityTest] public IEnumerator CaptureLossCancelsGesture()
    {
        yield return Mount(); var point = Target.worldBound.center;
        Down(point); Move(point + Along(30)); rootVisualElement.CapturePointer(Pointer);
        Move(point + Along(40)); Flush();
        yield return Wait(() => Cancels.Length == 1); rootVisualElement.ReleasePointer(Pointer);
    }
    [UnityTest] public IEnumerator CallbackRecompositionKeepsGestureAndUsesLatestCallback()
    {
        yield return Mount(); var point = Target.worldBound.center;
        Down(point); Move(point + Along(30)); var old = _value;
        _factor.Value = 2; _revision.Value++; yield return Settle(); Move(point + Along(50)); Up(point + Along(50));
        Assert.That(_value, Is.EqualTo(old + 40).Within(.1)); yield return Wait(() => Stops.Length == 1); Assert.That(Cancels, Is.Empty);
    }
    [UnityTest] public IEnumerator ReverseChangeCancelsInFlightGesture()
    {
        yield return Mount(); var point = Target.worldBound.center;
        Down(point); Move(point + Along(30)); _reverse.Value = true; yield return Settle(); yield return Wait(() => Cancels.Length == 1);
    }
    [UnityTest] public IEnumerator SourceReplacementClosesOldInteractionWithoutRestartingDrag()
    {
        yield return Mount(); var point = Target.worldBound.center;
        Down(point); Move(point + Along(30)); var before = _value;
        _source.Value = MutableInteractionSource(); yield return Settle(); yield return Wait(() => Cancels.Length == 1);
        Move(point + Along(50)); Up(point + Along(50)); Assert.That(_value, Is.GreaterThan(before)); Assert.That(Stops, Is.Empty);
    }
    [UnityTest] public IEnumerator CaptureKeepsDraggingOutsideBounds()
    {
        yield return Mount(); var point = Target.worldBound.center;
        Down(point); Move(point + Along(30)); Move(point + Along(300)); Up(point + Along(300));
        Assert.That(_value, Is.GreaterThan(250)); yield return Wait(() => Stops.Length == 1);
    }
    [UnityTest] public IEnumerator TapStillClicksAndDragCancelsClick()
    {
        _clickable = true; yield return Mount(); var point = Target.worldBound.center;
        Down(point); Up(point); yield return Settle(); Assert.That(_clicks, Is.EqualTo(1));
        Down(point); Move(point + Along(30)); Up(point + Along(30)); yield return Settle(); Assert.That(_clicks, Is.EqualTo(1));
        yield return Wait(() => Stops.Length == 1); Assert.That(Cancels, Is.Empty);
    }
    [UnityTest] public IEnumerator ClickableDescendantCanYieldToDrag()
    {
        _childClick = true; yield return Mount(); var point = _view.Q("child").worldBound.center;
        Down(point); Move(point + Along(30)); Up(point + Along(30)); yield return Settle();
        Assert.That(_value, Is.GreaterThan(0)); Assert.That(_clicks, Is.Zero);
    }
    [UnityTest] public IEnumerator PreventUserInputMutationRejectsGesture()
    {
        yield return Mount(); using var cancel = new CancellationTokenSource();
        var task = _scroll ? _state.Scroll(scope => Task.Delay(-1, scope.Token), MutatePriority.PreventUserInput, cancel.Token)
            : _drag.Drag(scope => Task.Delay(-1, scope.Token), MutatePriority.PreventUserInput, cancel.Token);
        var point = Target.worldBound.center; Down(point); Move(point + Along(30)); Up(point + Along(30));
        Assert.That(_value, Is.Zero); cancel.Cancel(); yield return Settle(); Assert.That(task.IsCanceled, Is.True);
    }
    [UnityTest] public IEnumerator MouseRightButtonNeverDrags()
    {
        yield return Mount(); var point = Target.worldBound.center;
        simulate.MouseDown(point, MouseButton.RightMouse); simulate.MouseMove(point + Along(40)); simulate.MouseUp(point + Along(40), MouseButton.RightMouse);
        yield return Settle(); Assert.That(_value, Is.Zero); Assert.That(Starts, Is.Empty);
    }

    [UnityTest] public IEnumerator ScrollableWheelHasNoDragInteractionsAndDraggableIgnoresWheel()
    {
        yield return Mount(); simulate.ScrollWheel(Target, _horizontal ? new Vector2(0, 2) : new Vector2(0, 2)); yield return Settle();
        Assert.That(_value, _scroll ? Is.LessThan(0) : Is.EqualTo(0)); Assert.That(Starts, Is.Empty);
    }




    [UnityTest] public IEnumerator TouchHandsOffToRemainingFingerWithoutEndingInteraction()
    {
        yield return Mount(); var point=Target.worldBound.center;
        TouchFinger(TouchPhase.Began,point,0); Flush();
        TouchFinger(TouchPhase.Moved,point+Along(30),0); Flush();
        TouchFinger(TouchPhase.Began,point,1); Flush();
        TouchFinger(TouchPhase.Ended,point+Along(30),0); Flush();
        yield return Wait(()=>Starts.Length==1); Assert.That(Stops,Is.Empty);
        var value=_value; TouchFinger(TouchPhase.Moved,point+Along(20),1); Flush();
        TouchFinger(TouchPhase.Ended,point+Along(20),1); Flush();
        Assert.That(_value,Is.EqualTo(value+20).Within(.1)); yield return Wait(()=>Stops.Length==1); Assert.That(Cancels,Is.Empty);
    }
    [UnityTest] public IEnumerator ScaledElementReportsLocalDelta()
    {
        yield return Mount(); Target.style.scale = new Scale(new Vector2(2,2)); yield return Settle();
        var point=Target.worldBound.center; Down(point); Move(point+Along(40)); Up(point+Along(40));
        Assert.That(_value,Is.EqualTo(20-(_scroll?8:.5f)).Within(.2));
    }

    [UnityTest] public IEnumerator AsynchronousMutationCleanupDoesNotLeaveAWaitingGestureOrReplayWheel()
    {
        yield return Mount(); var cleanup=new TaskCompletionSource<bool>();
        var busy = _scroll ? _state.Scroll(async scope => { try { await Task.Delay(-1,scope.Token); } finally { await cleanup.Task; } })
            : _drag.Drag(async scope => { try { await Task.Delay(-1,scope.Token); } finally { await cleanup.Task; } });
        var point=Target.worldBound.center;
        if (_scroll) simulate.ScrollWheel(Target,new Vector2(0,2));
        else { Down(point); Move(point+Along(30)); Up(point+Along(30)); }
        cleanup.SetResult(true); yield return Settle();
        Assert.That(_value,Is.Zero); Assert.That(busy.IsCanceled,Is.True); Assert.That(Target.HasPointerCapture(Pointer),Is.False);
        if (_scroll) Assert.That(_state.IsScrollInProgress,Is.False);
        Down(point); Move(point+Along(30)); Up(point+Along(30)); Assert.That(_value,Is.GreaterThan(0));
    }

}
public sealed class TestUiContext : SynchronizationContext
{
    private readonly Queue<(SendOrPostCallback callback, object? state)> _queue = new();
    public override void Post(SendOrPostCallback callback, object? state) { lock (_queue) _queue.Enqueue((callback, state)); }
    internal void Drain()
    {
        for (var i = 0; i < 10000; i++)
        {
            (SendOrPostCallback callback, object? state) item;
            lock (_queue) { if (_queue.Count == 0) return; item = _queue.Dequeue(); }
            item.callback(item.state);
        }
        throw new AssertionException("Continuations did not settle");
    }
}

[TestFixture(false), TestFixture(true)]
public sealed class DraggableModifierTests(bool horizontal) : GestureModifierFixture(false, horizontal)
{
    [UnityTest] public IEnumerator ImmediateDragClaimsDownAndReportsLocalPosition()
    {
        _immediate.Value = true; _clickable = true; yield return Mount(); var point = Target.LocalToWorld(new Vector2(25, 35));
        Down(point); Assert.That(_starts.Single().X, Is.EqualTo(25).Within(.1)); Assert.That(_starts.Single().Y, Is.EqualTo(35).Within(.1));
        Move(point + Along(.25f)); Up(point + Along(.25f)); yield return Settle(); Assert.That(_value, Is.EqualTo(.25f).Within(.1)); Assert.That(_clicks, Is.Zero);
    }
}
[TestFixture(false), TestFixture(true)]
public sealed class ScrollableModifierTests(bool horizontal) : GestureModifierFixture(true, horizontal)
{
    [UnityTest] public IEnumerator NestedWheelAndTouchForwardOnlyUnconsumedDistance()
    {
        _nested = true; _limit = 10; yield return Mount(); var point = Target.worldBound.center;
        Down(point); Move(point + Along(40)); Up(point + Along(40));
        Assert.That(_value, Is.EqualTo(10).Within(.1)); Assert.That(_parentValue, Is.EqualTo(22).Within(.1));
        _value = _parentValue = 0; simulate.ScrollWheel(Target, new Vector2(0, -2)); yield return Settle();
        Assert.That(_value, Is.EqualTo(10).Within(.1)); Assert.That(_parentValue, Is.GreaterThan(0));
    }
    [UnityTest] public IEnumerator ProgressCoversDragAndNativeFlingAndStopsOnDisable()
    {
        _fling = true; yield return Mount(); var point = Target.worldBound.center;
        Down(point); Move(point + Along(30)); Assert.That(_state.IsScrollInProgress, Is.True);
        Move(point + Along(60)); Up(point + Along(60)); var before = _value; Flush(80); Flush(80);
        Assert.That(_value, Is.GreaterThan(before)); Assert.That(_state.IsScrollInProgress, Is.True);
        _enabled.Value = false; yield return Settle(); before = _value; Flush(500);
        Assert.That(_value, Is.EqualTo(before)); Assert.That(_state.IsScrollInProgress, Is.False);
    }
    [UnityTest] public IEnumerator ScrollableMouseDragDoesNotScroll()
    {
        yield return Mount(); var point = Target.worldBound.center;
        simulate.MouseDown(point); simulate.MouseMove(point + Along(40)); simulate.MouseUp(point + Along(40)); yield return Settle(); Assert.That(_value, Is.Zero);
    }
    [UnityTest] public IEnumerator KeyboardPageScrollUsesViewportAndRespectsDisabled()
    {
        yield return Mount(); Target.focusable = true; Target.Focus(); simulate.KeyDown(KeyCode.PageDown); yield return Settle();
        Assert.That(_value, Is.EqualTo(-(_horizontal ? 240 : 160)).Within(.1));
        _enabled.Value = false; yield return Settle(); var before = _value; simulate.KeyDown(KeyCode.PageUp); yield return Settle(); Assert.That(_value, Is.EqualTo(before));
    }
    [UnityTest] public IEnumerator ResidualTouchScrollReachesNativeScrollView()
    {
        _nativeParent=true; _limit=10; yield return Mount(); var point=Target.worldBound.center;
        Down(point); Move(point-Along(40)); Up(point-Along(40)); yield return Settle();
        Assert.That(_value,Is.EqualTo(-10).Within(.1)); Assert.That(_outerState.Value,Is.EqualTo(22).Within(.5));
    }

}
