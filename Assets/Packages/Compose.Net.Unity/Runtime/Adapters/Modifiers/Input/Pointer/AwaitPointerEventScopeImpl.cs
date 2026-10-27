using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Compose.Net;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Runtime.Adapters.Modifiers.Input.Pointer;

internal sealed class AwaitPointerEventScopeImpl : IAwaitPointerEventScope
{
    private readonly VisualElement _element;
    private readonly Queue<IPointerInputChange> _pending = new();
    private TaskCompletionSource<IPointerInputChange>? _waiter;

    public AwaitPointerEventScopeImpl(VisualElement element, CancellationToken token)
    {
        _element = element;
        Token = token;

        _element.RegisterCallback<PointerDownEvent>(OnDown);
        _element.RegisterCallback<PointerMoveEvent>(OnMove);
        _element.RegisterCallback<PointerUpEvent>(OnUp);
        _element.RegisterCallback<PointerCancelEvent>(OnCancel);
        token.Register(Cancel);
    }

    public Size Size => _element.contentRect.size.ToSize();
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
            if (!downOnly || !change.ChangedToUp())
                return change;
        }
    }

    private Task<IPointerInputChange> Next()
    {
        Token.ThrowIfCancellationRequested();
        if (_pending.Count > 0)
            return Task.FromResult(_pending.Dequeue());

        _waiter = new TaskCompletionSource<IPointerInputChange>(TaskCreationOptions.RunContinuationsAsynchronously);
        return _waiter.Task;
    }

    private void OnDown(PointerDownEvent evt)
    {
        _element.CapturePointer(evt.pointerId);
        Push(PointerInputChange.Create(evt));
    }

    private void OnMove(PointerMoveEvent evt)
    {
        Push(PointerInputChange.Create(evt));
    }

    private void OnUp(PointerUpEvent evt)
    {
        _element.ReleasePointer(evt.pointerId);
        Push(PointerInputChange.Create(evt));
    }

    private void OnCancel(PointerCancelEvent evt)
    {
        _element.ReleasePointer(evt.pointerId);
        Push(PointerInputChange.Create(evt));
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
        _element.UnregisterCallback<PointerDownEvent>(OnDown);
        _element.UnregisterCallback<PointerMoveEvent>(OnMove);
        _element.UnregisterCallback<PointerUpEvent>(OnUp);
        _element.UnregisterCallback<PointerCancelEvent>(OnCancel);
        _waiter?.TrySetCanceled(Token);
    }
}
