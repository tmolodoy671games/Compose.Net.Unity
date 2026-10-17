// ReSharper disable CheckNamespace

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Compose.Net;
using SharpExtensions;
using StableCollections;

namespace Compose.Net.Unity;

internal class PointerInputModifierImpl<T> : IModifier
{
    private readonly T _key;
    private readonly Func<IPointerInputScope, Task> _body;

    public PointerInputModifierImpl(T key, Func<IPointerInputScope, Task> body)
    {
        _key = key;
        _body = body;
    }

    public void Apply(IReusableComposeNode node, IStableList<IModifier> newModifiers)
    {
        var source = new CancellationTokenSource();
        var token = source.Token;
        node.VisualElement().PickingMode().Increment();
        node.VisualElement().UserData()[new ReferenceKey(this)] = new CustomDisposable(() =>
        {
            source.Cancel();
            source.Dispose();
        });
        _body(new PointerInputScopeImpl(node.VisualElement(), token));
    }

    public void Revert(IReusableComposeNode node, IStableList<IModifier> newModifiers)
    {
        var key = new ReferenceKey(this);
        node.VisualElement().PickingMode().Decrement();
        var source = node.VisualElement().UserData().GetOrNull(key)
            ?.CastToOrNull<IDisposable>();
        source?.Dispose();
        node.VisualElement().UserData().Remove(key);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((PointerInputModifierImpl<T>)obj);
    }

    public override int GetHashCode()
    {
        return EqualityComparer<T>.Default.GetHashCode(_key);
    }

    private bool Equals(PointerInputModifierImpl<T> other)
    {
        return EqualityUtils.Equals(_key, other._key);
    }
}