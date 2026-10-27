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

    public ModifierNode CreateNode() => new InputNode(_body);

    private sealed class InputNode : ModifierNode
    {
        private readonly Func<IPointerInputScope, Task> _body;
        internal InputNode(Func<IPointerInputScope, Task> body) => _body = body;
        private CancellationTokenSource? _source;
        public override bool CanUpdate(IModifier modifier) => false;
        public override void Update(IModifier modifier) => throw new InvalidOperationException("Pointer input key changed");
        protected override void OnAttach()
        {
            _source = new CancellationTokenSource();
            Owner.VisualElement().PickingMode().Increment();
            _ = _body(new PointerInputScopeImpl(Owner.VisualElement(), _source.Token));
        }
        protected override void OnDetach()
        {
            Owner.VisualElement().PickingMode().Decrement();
            var source = _source;
            _source = null;
            if (source == null) return;
            try { source.Cancel(); }
            finally { source.Dispose(); }
        }
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
