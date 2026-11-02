using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Compose.Net;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Runtime.Adapters.Modifiers.Input.Pointer;

internal sealed class AwaitPointerEventScopeImpl : IAwaitPointerEventScope, IDisposable
{
    private readonly VisualElement _element;
    private readonly Queue<IPointerInputChange> _pending = new();
    private TaskCompletionSource<IPointerInputChange>? _waiter;
    private readonly HashSet<int> _captured = new();
    private readonly CancellationTokenRegistration _registration;
    private bool _disposed;

    public AwaitPointerEventScopeImpl(VisualElement element, CancellationToken token)
    {
        _element = element;
        Token = token;

        _element.RegisterCallback<PointerDownEvent>(OnDown);
        _element.RegisterCallback<PointerMoveEvent>(OnMove);
        _element.RegisterCallback<PointerUpEvent>(OnUp);
        _element.RegisterCallback<PointerCancelEvent>(OnCancel);
        _element.RegisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
        _registration = token.Register(Cancel);
    }

    public Size Size => _element.layout.size.ToSize();
    public CancellationToken Token { get; }
    public VisualElement Element => _element;

    public Task<IPointerInputChange> AwaitFirstDown()
    {
        return Wait(downOnly: true);
    }

    public Task<IPointerInputChange> AwaitPointerInputChange()
    {
        return Wait(downOnly: false);
    }

    private async Task<IPointerInputChange> Wait(bool downOnly)
    {
        while (true)
        {
            var change = await Next();
            if (!downOnly || change.ChangedToPressed())
                return change;
        }
    }

    private Task<IPointerInputChange> Next()
    {
        Token.ThrowIfCancellationRequested();
        if (_pending.Count > 0)
            return Task.FromResult(_pending.Dequeue());

        // Continue during UI dispatch so Consume can stop bubbling before
        // an ancestor handles the same down/up.
        _waiter = new TaskCompletionSource<IPointerInputChange>();
        return _waiter.Task;
    }

    private void OnDown(PointerDownEvent evt)
    {
        if (!_element.enabledInHierarchy) return;
        _captured.Add(evt.pointerId);
        _element.CapturePointer(evt.pointerId);
        Push(PointerInputChange.Create(evt));
    }

    private void OnMove(PointerMoveEvent evt)
    {
        Push(PointerInputChange.Create(evt));
    }

    private void OnUp(PointerUpEvent evt)
    {
        _captured.Remove(evt.pointerId);
        _element.ReleasePointer(evt.pointerId);
        Push(PointerInputChange.Create(evt));
    }

    private void OnCancel(PointerCancelEvent evt)
    {
        _captured.Remove(evt.pointerId);
        _element.ReleasePointer(evt.pointerId);
        Push(PointerInputChange.Create(evt));
    }

    private void OnCaptureOut(PointerCaptureOutEvent evt)
    {
        if (_captured.Remove(evt.pointerId))
            Push(PointerInputChange.Cancel(evt.pointerId, evt));
    }

    public void Dispose()
    {
        Cancel();
        _registration.Dispose();
    }

    private void Push(IPointerInputChange change)
    {
        if (_waiter != null)
        {
            var waiter = _waiter;
            _waiter = null;
            waiter.TrySetResult(change);
            return;
        }

        _pending.Enqueue(change);
    }

    private void Cancel()
    {
        if (_disposed) return;
        _disposed = true;
        _element.UnregisterCallback<PointerDownEvent>(OnDown);
        _element.UnregisterCallback<PointerMoveEvent>(OnMove);
        _element.UnregisterCallback<PointerUpEvent>(OnUp);
        _element.UnregisterCallback<PointerCancelEvent>(OnCancel);
        _element.UnregisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
        foreach (var pointer in _captured)
            _element.ReleasePointer(pointer);
        _captured.Clear();
        _pending.Clear();
        _waiter?.TrySetCanceled();
        _waiter = null;
    }
}