using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;
using PointerId = Compose.Net.PointerId;
using PointerType = Compose.Net.PointerType;

namespace Compose.Net.Unity;

/// <summary>
/// UI Toolkit owns picking, transforms, pointer IDs and capture. This bridge adds Compose's
/// three synchronous passes (UI Toolkit has no Final pass), without retaining pooled events.
/// </summary>
internal sealed class PointerEventDispatcher : PointerManipulator
{
    private static readonly ConditionalWeakTable<VisualElement, PointerEventDispatcher> Dispatchers = new();
    private readonly Dictionary<VisualElement, List<PointerInputScopeImpl>> _handlers = new();
    private readonly Dictionary<int, PointerState> _pointers = new();
    private bool _registered;

    internal static PointerEventDispatcher Get(VisualElement root) => Dispatchers.GetValue(root, element => new PointerEventDispatcher { target = element });
    internal void Add(PointerInputScopeImpl handler)
    {
        if (!_registered) RegisterCallbacksOnTarget();
        if (!_handlers.TryGetValue(handler.Element, out var list))
        {
            _handlers.Add(handler.Element, list = new());
            if (handler.Element != target) Register(handler.Element);
        }
        list.Add(handler);
    }
    internal void Remove(PointerInputScopeImpl handler)
    {
        if (_handlers.TryGetValue(handler.Element, out var list))
        {
            list.Remove(handler);
            if (list.Count == 0)
            {
                _handlers.Remove(handler.Element);
                if (handler.Element != target) Unregister(handler.Element);
            }
        }
        foreach (var entry in _pointers.ToArray())
        {
            entry.Value.Path.Remove(handler);
            if (entry.Value.Path.Count != 0) continue;
            _pointers.Remove(entry.Key);
            Release(entry.Key, entry.Value);
        }
        if (_handlers.Count == 0) { UnregisterCallbacksFromTarget(); _pointers.Clear(); }
    }
    protected override void RegisterCallbacksOnTarget()
    {
        if (_registered) return;
        _registered = true; Register(target);
    }
    protected override void UnregisterCallbacksFromTarget()
    {
        if (!_registered) return;
        _registered = false; Unregister(target);
    }
    private void Register(VisualElement element)
    {
        // Native trickle-down guards run first. At the deepest eligible native bubble
        // callback, deliver all Compose passes synchronously. Capture bypasses ancestor
        // callbacks in UI Toolkit, so register the same callbacks on each input element.
        element.RegisterCallback<PointerDownEvent>(Down);
        element.RegisterCallback<PointerMoveEvent>(Move);
        element.RegisterCallback<PointerUpEvent>(Up);
        element.RegisterCallback<PointerCancelEvent>(Cancel);
        element.RegisterCallback<PointerEnterEvent>(Enter);
        element.RegisterCallback<PointerLeaveEvent>(Exit);
        element.RegisterCallback<WheelEvent>(Wheel);
        element.RegisterCallback<PointerCaptureOutEvent>(CaptureOut);
    }
    private void Unregister(VisualElement element)
    {
        element.UnregisterCallback<PointerDownEvent>(Down);
        element.UnregisterCallback<PointerMoveEvent>(Move);
        element.UnregisterCallback<PointerUpEvent>(Up);
        element.UnregisterCallback<PointerCancelEvent>(Cancel);
        element.UnregisterCallback<PointerEnterEvent>(Enter);
        element.UnregisterCallback<PointerLeaveEvent>(Exit);
        element.UnregisterCallback<WheelEvent>(Wheel);
        element.UnregisterCallback<PointerCaptureOutEvent>(CaptureOut);
    }
    private void Down(PointerDownEvent evt) => Pointer(evt, evt, PointerEventType.Press);
    private void Move(PointerMoveEvent evt) => Pointer(evt, evt, PointerEventType.Move);
    private void Up(PointerUpEvent evt) => Pointer(evt, evt, PointerEventType.Release);
    private void Cancel(PointerCancelEvent evt) => Pointer(evt, evt, PointerEventType.Release, true);
    private void Enter(PointerEnterEvent evt) => Pointer(evt, evt, PointerEventType.Enter);
    private void Exit(PointerLeaveEvent evt) => Pointer(evt, evt, PointerEventType.Exit);

    private List<PointerInputScopeImpl> Path(VisualElement? element, bool direct = false)
    {
        var path = new List<PointerInputScopeImpl>();
        for (var node = element; node != null; node = direct ? null : node.parent)
            if (_handlers.TryGetValue(node, out var handlers) && node.enabledInHierarchy)
                path.InsertRange(0, handlers);
        return path;
    }
    private void Pointer(EventBase evt, IPointerEvent pointer, PointerEventType type, bool canceled = false)
    {
        var hover = type == PointerEventType.Enter || type == PointerEventType.Exit;
        _pointers.TryGetValue(pointer.pointerId, out var previous);
        // UI Toolkit can defer CaptureOut until after the next dispatched event. Never
        // treat that event as a successful release if native capture has already moved.
        if (previous?.Pressed == true && !previous.Capture.HasPointerCapture(pointer.pointerId))
        {
            CancelPointer(pointer.pointerId, evt.timestamp, previous);
            previous = null;
        }
        if (hover && previous?.Pressed == true) return;
        if (type == PointerEventType.Move && pointer.button >= 0)
            type = (pointer.pressedButtons & (1 << pointer.button)) != 0 ? PointerEventType.Press : PointerEventType.Release;
        var pressed = !canceled && pointer.pressedButtons != 0;
        var path = !hover && previous?.Pressed == true ? new List<PointerInputScopeImpl>(previous.Path) : Path(evt.target as VisualElement, hover);
        if (path.Count == 0 || evt.currentTarget != path[path.Count - 1].Element) return;
        var position = new Offset(pointer.position.x, pointer.position.y);
        var device = pointer.pointerType == UnityEngine.UIElements.PointerType.mouse ? PointerType.Mouse :
            pointer.pointerType == UnityEngine.UIElements.PointerType.touch ? PointerType.Touch :
            pointer.pointerType == UnityEngine.UIElements.PointerType.pen ? PointerType.Stylus : PointerType.Unknown;
        var change = new PointerInputChange(new PointerId(pointer.pointerId), evt.timestamp, position, pressed,
            previous?.Time ?? evt.timestamp, previous?.Position ?? position, previous?.Pressed ?? false,
            canceled, device, pressure: pointer.pressure);
        var state = new PointerState(position, evt.timestamp, pressed, pointer.pressure, device, path,
            previous?.Pressed == true ? previous.Capture : path[path.Count - 1].Element);
        if (!hover) _pointers[pointer.pointerId] = state;
        if (pressed && previous?.Pressed != true) state.Capture.CapturePointer(pointer.pointerId);
        Dispatch(path, change, type, pointer.pressedButtons, pointer.modifiers);
        if (!hover && !pressed)
        {
            // Keep mouse history for hover, but release the native capture before the next gesture.
            if (device != PointerType.Mouse || canceled) _pointers.Remove(pointer.pointerId);
            Release(pointer.pointerId, state);
        }
        // All Compose observers have already seen the consumed event. Native controls must not
        // also act on a gesture claimed by a Compose detector (e.g. a drag inside a ScrollView).
        if (change.IsConsumed && !hover) evt.StopPropagation();
    }
    private void Wheel(WheelEvent evt)
    {
        var path = Path(evt.target as VisualElement);
        if (path.Count == 0 || evt.currentTarget != path[path.Count - 1].Element) return;
        _pointers.TryGetValue(0, out var previous);
        var position = new Offset(evt.mousePosition.x, evt.mousePosition.y);
        var change = new PointerInputChange(new PointerId(0), evt.timestamp, position, evt.pressedButtons != 0,
            previous?.Time ?? evt.timestamp, previous?.Position ?? position, previous?.Pressed ?? false,
            type: PointerType.Mouse, scrollDelta: new Offset(evt.delta.x, evt.delta.y));
        Dispatch(path, change, PointerEventType.Scroll, evt.pressedButtons, evt.modifiers);
        if (change.IsConsumed) evt.StopPropagation();
    }
    private void CaptureOut(PointerCaptureOutEvent evt)
    {
        if (!_pointers.TryGetValue(evt.pointerId, out var state) || !state.Pressed || evt.target != state.Capture) return;
        CancelPointer(evt.pointerId, evt.timestamp, state);
    }
    private void CancelPointer(int id, long timestamp, PointerState state)
    {
        _pointers.Remove(id);
        var change = new PointerInputChange(new PointerId(id), timestamp, state.Position, false,
            state.Time, state.Position, true, true, state.Type, pressure: state.Pressure);
        Dispatch(state.Path.ToList(), change, PointerEventType.Release, 0, EventModifiers.None);
    }
    private static void Release(int id, PointerState state)
    { if (state.Capture.HasPointerCapture(id)) state.Capture.ReleasePointer(id); }

    private void Dispatch(List<PointerInputScopeImpl> path, PointerInputChange changed, PointerEventType type, int buttons, EventModifiers modifiers)
    {
        var snapshots = new Dictionary<PointerInputScopeImpl, PointerEvent>();
        // Stationary pointers belong only to nodes on their own native hit/capture chain.
        var stationary = _pointers.Where(p => p.Key != changed.Id.Value && p.Value.Pressed)
            .Select(p => (State: p.Value, Change: new PointerInputChange(new PointerId(p.Key), changed.UptimeMillis,
                p.Value.Position, true, p.Value.Time, p.Value.Position, true, type: p.Value.Type, pressure: p.Value.Pressure))).ToArray();
        foreach (var handler in path)
        {
            var element = handler.Element;
            PointerInputChange Local(PointerInputChange c) => c.Copy(
                position: ToOffset(element.WorldToLocal(new Vector2(c.Position.X, c.Position.Y))),
                previousPosition: ToOffset(element.WorldToLocal(new Vector2(c.PreviousPosition.X, c.PreviousPosition.Y))));
            var changes = new List<IPointerInputChange> { Local(changed) };
            changes.AddRange(stationary.Where(p => p.State.Path.Contains(handler)).Select(p => Local(p.Change)));
            snapshots[handler] = new PointerEvent(changes, type, new PointerButtons(buttons), new PointerKeyboardModifiers(
                (modifiers & EventModifiers.Control) != 0, (modifiers & EventModifiers.Command) != 0,
                (modifiers & EventModifiers.Alt) != 0, (modifiers & EventModifiers.Shift) != 0,
                (modifiers & EventModifiers.CapsLock) != 0, (modifiers & EventModifiers.Numeric) != 0));
        }
        foreach (var handler in path) handler.Dispatch(snapshots[handler], PointerEventPass.Initial);
        for (var i = path.Count - 1; i >= 0; i--) path[i].Dispatch(snapshots[path[i]], PointerEventPass.Main);
        foreach (var handler in path) handler.Dispatch(snapshots[handler], PointerEventPass.Final);
    }
    private static Offset ToOffset(Vector2 value) => new(value.x, value.y);
    private sealed record PointerState(Offset Position, long Time, bool Pressed, float Pressure, PointerType Type,
        List<PointerInputScopeImpl> Path, VisualElement Capture);
}
