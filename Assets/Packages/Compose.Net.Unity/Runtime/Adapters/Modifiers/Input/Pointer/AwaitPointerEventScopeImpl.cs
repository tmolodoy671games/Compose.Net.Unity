using System;
using System.Threading;
using System.Threading.Tasks;

namespace Compose.Net.Unity;

internal sealed class AwaitPointerEventScopeImpl : IAwaitPointerEventScope, IDisposable
{
    private readonly PointerInputScopeImpl _owner;
    private readonly CancellationTokenRegistration _lifetime;
    private readonly object _gate = new();
    private TaskCompletionSource<PointerEvent>? _waiter;
    private PointerEventPass _pass;
    private bool _disposed;
    public Size Size => _owner.Size;
    public Size ExtendedTouchPadding => _owner.ExtendedTouchPadding;
    public ViewConfiguration ViewConfiguration => _owner.ViewConfiguration;
    public CancellationToken Token { get; }
    public PointerEvent CurrentEvent => _owner.CurrentEvent;

    internal AwaitPointerEventScopeImpl(PointerInputScopeImpl owner, CancellationToken token)
    { _owner = owner; Token = token; _lifetime = token.Register(Cancel); }

    public Task<PointerEvent> AwaitPointerEvent(PointerEventPass pass = PointerEventPass.Main,
        CancellationToken cancellationToken = default)
    {
        Token.ThrowIfCancellationRequested(); cancellationToken.ThrowIfCancellationRequested();
        if (pass < PointerEventPass.Initial || pass > PointerEventPass.Final) throw new ArgumentOutOfRangeException(nameof(pass));
        TaskCompletionSource<PointerEvent> waiter;
        lock (_gate)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(AwaitPointerEventScopeImpl));
            if (_waiter != null) throw new InvalidOperationException("Use separate AwaitPointerEventScope blocks for concurrent listeners.");
            // Deliberately inline: consumption must be visible to the next handler/pass.
            waiter = _waiter = new TaskCompletionSource<PointerEvent>(); _pass = pass;
        }
        return Wait(waiter, cancellationToken);
    }
    private async Task<PointerEvent> Wait(TaskCompletionSource<PointerEvent> waiter, CancellationToken token)
    {
        using var registration = token.Register(() =>
        {
            lock (_gate) { if (ReferenceEquals(_waiter, waiter)) _waiter = null; }
            waiter.TrySetCanceled();
        });
        return await waiter.Task;
    }
    internal void Dispatch(PointerEvent evt, PointerEventPass pass)
    {
        TaskCompletionSource<PointerEvent>? waiter;
        lock (_gate)
        {
            if (_disposed || pass != _pass) return;
            waiter = _waiter; _waiter = null;
        }
        waiter?.TrySetResult(evt);
    }
    private void Cancel()
    {
        TaskCompletionSource<PointerEvent>? waiter;
        lock (_gate) { _disposed = true; waiter = _waiter; _waiter = null; }
        waiter?.TrySetCanceled();
    }
    public void Dispose() { Cancel(); _lifetime.Dispose(); }
}
