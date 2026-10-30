using System;
using System.Threading;
using System.Threading.Tasks;
using Compose.Net;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Runtime.Adapters.Modifiers.Input.Pointer;

internal sealed class AwaitPointerEventScopeImpl : IAwaitPointerEventScope, IDisposable
{
    private PointerEventDispatcher? _dispatcher;
    private readonly Compose.Net.PointerEventDispatcher _events;
    private readonly Compose.Net.PointerEventDispatcher.EventScope _scope;
    private readonly CancellationTokenRegistration _registration;
    private bool _disposed;

    public AwaitPointerEventScopeImpl(VisualElement element, CancellationToken token)
    {
        Element = element;
        _events = new Compose.Net.PointerEventDispatcher(() => Size, () => ExtendedTouchPadding);
        _scope = _events.CreateScope(token);
        _dispatcher = PointerEventDispatcher.Register(this);
        element.RegisterCallback<AttachToPanelEvent>(OnAttach);
        element.RegisterCallback<DetachFromPanelEvent>(OnDetach);
        _registration = token.Register(Dispose);
    }

    public Size Size => Element.contentRect.size.ToSize();
    public Size ExtendedTouchPadding => new(
        Math.Max(0f, ViewConfiguration.Default.MinimumTouchTargetSize.Width - Size.Width) / 2f,
        Math.Max(0f, ViewConfiguration.Default.MinimumTouchTargetSize.Height - Size.Height) / 2f);
    public CancellationToken Token => _scope.Token;
    public VisualElement Element { get; }
    public Compose.Net.PointerEvent CurrentEvent => _scope.CurrentEvent;
    public Task<Compose.Net.PointerEvent> AwaitPointerEvent(PointerEventPass pass = PointerEventPass.Main) => _scope.AwaitPointerEvent(pass);
    public Task<T> WithTimeout<T>(long timeMillis, Func<IAwaitPointerEventScope, Task<T>> block) => _scope.WithTimeout(timeMillis, block);
    public Task<T?> WithTimeoutOrNull<T>(long timeMillis, Func<IAwaitPointerEventScope, Task<T>> block) => _scope.WithTimeoutOrNull(timeMillis, block);

    internal void Dispatch(Compose.Net.PointerEvent evt, PointerEventPass pass)
    {
        if (!_disposed) _events.Dispatch(evt, pass);
    }

    private void OnAttach(AttachToPanelEvent evt)
    {
        if (evt.target != Element || _disposed) return;
        _dispatcher?.Remove(this);
        _dispatcher = PointerEventDispatcher.Register(this);
    }

    private void OnDetach(DetachFromPanelEvent evt)
    {
        if (evt.target != Element) return;
        _dispatcher?.Remove(this);
        _dispatcher = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Element.UnregisterCallback<AttachToPanelEvent>(OnAttach);
        Element.UnregisterCallback<DetachFromPanelEvent>(OnDetach);
        _dispatcher?.Remove(this);
        _dispatcher = null;
        _scope.Dispose();
        _registration.Dispose();
    }
}
