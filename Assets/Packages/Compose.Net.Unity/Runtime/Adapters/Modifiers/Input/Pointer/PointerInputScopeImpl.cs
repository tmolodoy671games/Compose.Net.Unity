using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal sealed class PointerInputScopeImpl : IPointerInputScope, IDisposable
{
    private Func<IPointerInputScope, Task> _body;
    private CancellationTokenSource _source = new();
    private readonly List<AwaitPointerEventScopeImpl> _scopes = new();
    private PointerEventDispatcher? _dispatcher;
    private bool _started;
    private bool _disposed;
    internal VisualElement Element { get; }
    internal PointerEvent CurrentEvent { get; private set; } = PointerEvent.Empty;
    public Size Size => Element.layout.size.ToSize();
    public Size ExtendedTouchPadding => new(0, 0);
    public ViewConfiguration ViewConfiguration { get; } = new();
    public CancellationToken Token => _source.Token;

    internal PointerInputScopeImpl(VisualElement element, Func<IPointerInputScope, Task> body)
    { Element = element; _body = body; }

    internal void Attach()
    {
        Element.RegisterCallback<AttachToPanelEvent>(OnAttach);
        Element.RegisterCallback<DetachFromPanelEvent>(OnDetach);
        if (Element.panel != null) Connect();
    }
    private void OnAttach(AttachToPanelEvent evt) { if (evt.target == Element) Connect(); }
    private void OnDetach(DetachFromPanelEvent evt) { if (evt.target == Element) Disconnect(); }
    private void Connect()
    {
        if (_dispatcher != null || _disposed) return;
        _dispatcher = PointerEventDispatcher.Get(Element.panel.visualTree);
        _dispatcher.Add(this);
    }
    private void Disconnect()
    {
        var dispatcher = _dispatcher; _dispatcher = null;
        dispatcher?.Remove(this);
        Reset();
    }
    internal void Update(Func<IPointerInputScope, Task> body, bool reset)
    { _body = body; if (reset) Reset(); }

    private void Reset()
    {
        var source = _source;
        _source = new CancellationTokenSource();
        _started = false;
        source.Cancel(); source.Dispose();
        CurrentEvent = PointerEvent.Empty;
    }

    internal void Dispatch(PointerEvent evt, PointerEventPass pass)
    {
        if (_disposed || _dispatcher == null) return;
        CurrentEvent = evt;
        if (!_started) { _started = true; _ = Run(_source.Token); }
        var scopes = _scopes.ToArray();
        if (pass == PointerEventPass.Main) Array.Reverse(scopes);
        foreach (var scope in scopes) scope.Dispatch(evt, pass);
    }
    private async Task Run(CancellationToken token)
    {
        try { await _body(new RunScope(this, token)); }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception exception) { Debug.LogException(exception); }
    }

    public Task AwaitPointerEventScope(Func<IAwaitPointerEventScope, Task> block) => AwaitScope(block, Token);
    private async Task AwaitScope(Func<IAwaitPointerEventScope, Task> block, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        using var scope = new AwaitPointerEventScopeImpl(this, token);
        _scopes.Add(scope);
        try { await block(scope); }
        finally { _scopes.Remove(scope); }
    }
    public Task<T> AwaitPointerEventScope<T>(Func<IAwaitPointerEventScope, Task<T>> block) => AwaitScope(block, Token);
    private async Task<T> AwaitScope<T>(Func<IAwaitPointerEventScope, Task<T>> block, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        using var scope = new AwaitPointerEventScopeImpl(this, token);
        _scopes.Add(scope);
        try { return await block(scope); }
        finally { _scopes.Remove(scope); }
    }
    private sealed class RunScope(PointerInputScopeImpl owner, CancellationToken token) : IPointerInputScope
    {
        public Size Size => owner.Size;
        public Size ExtendedTouchPadding => owner.ExtendedTouchPadding;
        public ViewConfiguration ViewConfiguration => owner.ViewConfiguration;
        public CancellationToken Token => token;
        public Task AwaitPointerEventScope(Func<IAwaitPointerEventScope, Task> block) => owner.AwaitScope(block, token);
        public Task<T> AwaitPointerEventScope<T>(Func<IAwaitPointerEventScope, Task<T>> block) => owner.AwaitScope(block, token);
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Element.UnregisterCallback<AttachToPanelEvent>(OnAttach);
        Element.UnregisterCallback<DetachFromPanelEvent>(OnDetach);
        Disconnect(); _source.Dispose();
    }
}
