// ReSharper disable CheckNamespace

using System;
using System.Threading;
using System.Threading.Tasks;
using Compose.Net;
using Compose.Net.Unity.Runtime.Adapters.Modifiers.Input.Pointer;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal class PointerInputScopeImpl : IPointerInputScope
{
    private readonly VisualElement _element;

    public PointerInputScopeImpl(VisualElement element, CancellationToken token)
    {
        _element = element;
        Token = token;
    }

    public Size Size => _element.contentRect.size.ToSize();
    public Size ExtendedTouchPadding => new(
        Math.Max(0f, ViewConfiguration.Default.MinimumTouchTargetSize.Width - Size.Width) / 2f,
        Math.Max(0f, ViewConfiguration.Default.MinimumTouchTargetSize.Height - Size.Height) / 2f);
    public CancellationToken Token { get; }

    public async Task AwaitPointerEventScope(Func<IAwaitPointerEventScope, Task> block)
    {
        using var scope = new AwaitPointerEventScopeImpl(_element, Token);
        await block(scope);
    }
}
