using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Compose.Net;
using Compose.Net.Unity.Runtime.Extensions;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Scripting.LifecycleManagement;
using PointerId = Compose.Net.PointerId;
using PointerType = Compose.Net.PointerType;

namespace Compose.Net.Unity.Runtime.Adapters.Modifiers.Input.Pointer;

/// <summary>Dispatches the three Compose passes independently of UI Toolkit propagation/consumption.</summary>
[AutoStaticsCleanup]
internal sealed partial class PointerEventDispatcher
{
    private static readonly ConditionalWeakTable<VisualElement, PointerEventDispatcher> Dispatchers = new();
    private readonly VisualElement _root;
    private readonly List<AwaitPointerEventScopeImpl> _scopes = new();
    private readonly Dictionary<PointerId, (PointerInputChange Change, VisualElement Target)> _pressed = new();

    private PointerEventDispatcher(VisualElement root)
    {
        _root = root;
        root.RegisterCallback<PointerDownEvent>(OnDown, TrickleDown.TrickleDown);
        root.RegisterCallback<PointerMoveEvent>(OnMove, TrickleDown.TrickleDown);
        root.RegisterCallback<PointerUpEvent>(OnUp, TrickleDown.TrickleDown);
        root.RegisterCallback<PointerCancelEvent>(OnCancel, TrickleDown.TrickleDown);
        root.RegisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
        root.RegisterCallback<PointerEnterEvent>(OnEnter, TrickleDown.TrickleDown);
        root.RegisterCallback<PointerLeaveEvent>(OnExit, TrickleDown.TrickleDown);
        root.RegisterCallback<WheelEvent>(OnWheel, TrickleDown.TrickleDown);
    }

    internal static PointerEventDispatcher Register(AwaitPointerEventScopeImpl scope)
    {
        var root = scope.Element.panel?.visualTree ?? scope.Element;
        while (root.parent != null) root = root.parent;
        var dispatcher = Dispatchers.GetValue(root, element => new PointerEventDispatcher(element));
        dispatcher._scopes.Add(scope);
        return dispatcher;
    }

    internal void Remove(AwaitPointerEventScopeImpl scope)
    {
        _scopes.Remove(scope);
        if (_scopes.Count != 0) return;
        _root.UnregisterCallback<PointerDownEvent>(OnDown, TrickleDown.TrickleDown);
        _root.UnregisterCallback<PointerMoveEvent>(OnMove, TrickleDown.TrickleDown);
        _root.UnregisterCallback<PointerUpEvent>(OnUp, TrickleDown.TrickleDown);
        _root.UnregisterCallback<PointerCancelEvent>(OnCancel, TrickleDown.TrickleDown);
        _root.UnregisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
        _root.UnregisterCallback<PointerEnterEvent>(OnEnter, TrickleDown.TrickleDown);
        _root.UnregisterCallback<PointerLeaveEvent>(OnExit, TrickleDown.TrickleDown);
        _root.UnregisterCallback<WheelEvent>(OnWheel, TrickleDown.TrickleDown);
        foreach (var (id, state) in _pressed)
        {
            UntrackCapturedTarget(state.Target);
            state.Target.ReleasePointer((int)id.Value);
        }
        _pressed.Clear();
        Dispatchers.Remove(_root);
    }

    private void OnDown(PointerDownEvent evt) => DispatchPointer(evt, PointerInputChange.Create(evt), PointerEventType.Press);
    private void OnMove(PointerMoveEvent evt) => DispatchPointer(evt, PointerInputChange.Create(evt), PointerEventType.Move);
    private void OnUp(PointerUpEvent evt) => DispatchPointer(evt, PointerInputChange.Create(evt), PointerEventType.Release);
    private void OnCancel(PointerCancelEvent evt) => DispatchPointer(evt, PointerInputChange.Create(evt), PointerEventType.Release);
    private void OnCaptureOut(PointerCaptureOutEvent evt)
    {
        var id = new PointerId(evt.pointerId);
        if (!_pressed.TryGetValue(id, out var state) || evt.target != state.Target) return;
        _pressed.Remove(id);
        if (!_pressed.Values.Any(item => item.Target == state.Target)) UntrackCapturedTarget(state.Target);
        // A scroll ancestor took capture: end the child's press before pointer-up
        // can be dispatched back to its original target as a click.
        var cancelled = state.Change with
        {
            Pressed = false,
            PreviousPressed = true,
            IsCancelled = true,
            Event = evt,
            Consumed = new PointerInputChange.Consumption()
        };
        Dispatch(new() { (cancelled, state.Target) }, PointerEventType.Release, default, default);
    }
    private void OnEnter(PointerEnterEvent evt) => DispatchHover(evt, PointerEventType.Enter);
    private void OnExit(PointerLeaveEvent evt) => DispatchHover(evt, PointerEventType.Exit);

    private void DispatchPointer<T>(PointerEventBase<T> evt, IPointerInputChange input, PointerEventType type)
        where T : PointerEventBase<T>, new()
    {
        var change = (PointerInputChange)input;
        var target = evt.target as VisualElement ?? _root;
        if (!_pressed.ContainsKey(change.Id) &&
            !_scopes.Any(scope => target == scope.Element || scope.Element.Contains(target))) return;
        var position = new Offset(evt.position.x, evt.position.y);
        var previous = _pressed.TryGetValue(change.Id, out var state) ? state.Change : null;
        if (previous != null) target = state.Target;
        change = change with
        {
            Position = position,
            PreviousPosition = previous?.Position ?? position - new Offset(evt.deltaPosition.x, evt.deltaPosition.y),
            PreviousUptimeMillis = previous?.UptimeMillis ?? evt.timestamp - (long)(evt.deltaTime * 1000),
            PreviousPressed = previous?.Pressed ?? change.PreviousPressed
        };
        var changes = new List<(PointerInputChange Change, VisualElement Target)>();
        foreach (var item in _pressed.Values)
        {
            if (item.Change.Id == change.Id) continue;
            changes.Add((item.Change with
            {
                UptimeMillis = change.UptimeMillis,
                PreviousUptimeMillis = item.Change.UptimeMillis,
                PreviousPosition = item.Change.Position,
                PreviousPressed = item.Change.Pressed,
                Consumed = new PointerInputChange.Consumption()
            }, item.Target));
        }
        changes.Add((change, target));
        if (change.Pressed) _pressed[change.Id] = (change, target);
        else _pressed.Remove(change.Id);
        if (type == PointerEventType.Press && target.panel != null)
        {
            // Captured native events are delivered only to the capturing element.
            if (target != _root)
            {
                target.RegisterCallback<PointerMoveEvent>(OnMove, TrickleDown.TrickleDown);
                target.RegisterCallback<PointerUpEvent>(OnUp, TrickleDown.TrickleDown);
                target.RegisterCallback<PointerCancelEvent>(OnCancel, TrickleDown.TrickleDown);
            }
            target.CapturePointer(evt.pointerId);
        }
        Dispatch(changes, type, new PointerButtons(evt.pressedButtons), Modifiers(evt.modifiers));
        if (!change.Pressed)
        {
            if (!_pressed.Values.Any(item => item.Target == target)) UntrackCapturedTarget(target);
            target.ReleasePointer(evt.pointerId);
        }
    }

    private void UntrackCapturedTarget(VisualElement target)
    {
        if (target == _root) return;
        target.UnregisterCallback<PointerMoveEvent>(OnMove, TrickleDown.TrickleDown);
        target.UnregisterCallback<PointerUpEvent>(OnUp, TrickleDown.TrickleDown);
        target.UnregisterCallback<PointerCancelEvent>(OnCancel, TrickleDown.TrickleDown);
    }

    private void DispatchHover<T>(PointerEventBase<T> evt, PointerEventType type)
        where T : PointerEventBase<T>, new()
    {
        var position = new Offset(evt.position.x, evt.position.y);
        var change = new PointerInputChange(new PointerId(evt.pointerId), evt.timestamp, position,
            evt.pressedButtons != 0, evt.pressure, evt.timestamp, position, evt.pressedButtons != 0,
            false, evt.pointerType.ToPointerType(), Offset.Zero, evt);
        Dispatch(new() { (change, evt.target as VisualElement ?? _root) }, type,
            new PointerButtons(evt.pressedButtons), Modifiers(evt.modifiers));
    }

    private void OnWheel(WheelEvent evt)
    {
        var position = new Offset(evt.mousePosition.x, evt.mousePosition.y);
        var change = new PointerInputChange(new PointerId(0), evt.timestamp, position,
            evt.pressedButtons != 0, 1f, evt.timestamp, position, evt.pressedButtons != 0,
            false, PointerType.Mouse, new Offset(evt.delta.x, evt.delta.y), evt);
        Dispatch(new() { (change, evt.target as VisualElement ?? _root) }, PointerEventType.Scroll,
            new PointerButtons(evt.pressedButtons), Modifiers(evt.modifiers));
    }

    private void Dispatch(List<(PointerInputChange Change, VisualElement Target)> changes, PointerEventType type,
        PointerButtons buttons, PointerKeyboardModifiers modifiers)
    {
        var handlers = new List<(AwaitPointerEventScopeImpl Scope, Compose.Net.PointerEvent Event)>();
        // Stable ordering preserves installation order for handlers on the same element.
        foreach (var scope in _scopes.OrderBy(scope => Depth(scope.Element)).ToArray())
        {
            var local = new List<IPointerInputChange>();
            foreach (var (change, target) in changes)
            {
                if (target != scope.Element && !scope.Element.Contains(target)) continue;
                var position = scope.Element.WorldToLocal(new Vector2(change.Position.X, change.Position.Y));
                var previous = scope.Element.WorldToLocal(new Vector2(change.PreviousPosition.X, change.PreviousPosition.Y));
                local.Add(change with { Position = new Offset(position.x, position.y), PreviousPosition = new Offset(previous.x, previous.y) });
            }
            if (local.Count > 0) handlers.Add((scope, new Compose.Net.PointerEvent(local, type, buttons, modifiers)));
        }
        foreach (var handler in handlers) handler.Scope.Dispatch(handler.Event, PointerEventPass.Initial);
        for (var i = handlers.Count - 1; i >= 0; i--) handlers[i].Scope.Dispatch(handlers[i].Event, PointerEventPass.Main);
        foreach (var handler in handlers) handler.Scope.Dispatch(handler.Event, PointerEventPass.Final);
    }

    private static int Depth(VisualElement element)
    {
        var depth = 0;
        while (element.parent != null) { depth++; element = element.parent; }
        return depth;
    }

    private static PointerKeyboardModifiers Modifiers(EventModifiers modifiers) => new(
        IsCtrlPressed: (modifiers & EventModifiers.Control) != 0,
        IsMetaPressed: (modifiers & EventModifiers.Command) != 0,
        IsAltPressed: (modifiers & EventModifiers.Alt) != 0,
        IsShiftPressed: (modifiers & EventModifiers.Shift) != 0,
        IsCapsLockOn: (modifiers & EventModifiers.CapsLock) != 0,
        IsFunctionPressed: (modifiers & EventModifiers.FunctionKey) != 0);
}
