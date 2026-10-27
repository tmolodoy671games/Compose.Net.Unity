// ReSharper disable CheckNamespace

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Compose.Net;
using SharpExtensions;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record PointerInputModifierImpl<T>(
    T Key,
    Func<IPointerInputScope, Task> Body
) : ModifierNodeElement<PointerInputModifierImpl<T>.PointerNode>
{
    public override PointerNode Create() => new(Body);
    public override void Update(PointerNode node) => node.Update(Body);
    public override object GetTarget(IReusableComposeNode node) => node.VisualElement();

    public virtual bool Equals(PointerInputModifierImpl<T>? other) =>
        other != null && EqualityUtils.Equals(Key, other.Key);

    public override int GetHashCode() => EqualityComparer<T>.Default.GetHashCode(Key!);

    internal sealed class PointerNode(Func<IPointerInputScope, Task> body) : IModifier.Node
    {
        private Func<IPointerInputScope, Task> _body = body;
        private CancellationTokenSource? _source;
        private VisualElement? _element;

        public override void OnAttach()
        {
            _element = (VisualElement)Target;
            _element.PickingMode().Increment();
            StartBody();
        }

        internal void Update(Func<IPointerInputScope, Task> next)
        {
            if (ReferenceEquals(_body, next))
                return;
            _body = next;
            if (_element != null)
                StartBody();
        }

        public override void OnDetach()
        {
            if (_element != null)
                _element.PickingMode().Decrement();
            _source?.Cancel();
            _source?.Dispose();
            _source = null;
            _element = null;
        }

        private void StartBody()
        {
            _source?.Cancel();
            _source?.Dispose();
            _source = new CancellationTokenSource();
            _body(new PointerInputScopeImpl(_element!, _source.Token));
        }
    }
}
