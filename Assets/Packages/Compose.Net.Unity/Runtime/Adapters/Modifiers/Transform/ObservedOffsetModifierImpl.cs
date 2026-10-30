// ReSharper disable CheckNamespace

using System;
using Compose.Net;
using Compose.Net.Unity.Runtime.Adapters.Extensions.Values;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

/// <summary>
/// Offset pushed by a subscription, so it changes without recomposition (e.g. pager scroll).
/// </summary>
internal record ObservedOffsetModifierImpl(Func<Action<Offset>, IDisposable> Subscribe)
    : UnityModifierElement<ObservedOffsetModifierImpl.ObservedOffsetNode>
{
    public override ObservedOffsetNode Create() => new(Subscribe);
    public override void Update(ObservedOffsetNode node) => node.Update(Subscribe);
    public override object GetTarget(IReusableComposeNode node) => node.VisualElement();

    internal sealed class ObservedOffsetNode(Func<Action<Offset>, IDisposable> subscribe) : IModifier.Node
    {
        private Func<Action<Offset>, IDisposable> _subscribe = subscribe;
        private IDisposable? _subscription;
        private Offset _offset;
        private VisualElement? _element;

        public override void OnAttach()
        {
            _element = (VisualElement)Target;
            _subscription = _subscribe(SetOffset);
        }

        internal void Update(Func<Action<Offset>, IDisposable> next)
        {
            if (Equals(_subscribe, next)) return;
            _subscription?.Dispose();
            _subscription = null;
            _subscribe = next;
            if (_element != null)
                _subscription = _subscribe(SetOffset);
        }

        private void SetOffset(Offset offset)
        {
            if (_element == null) return;
            var style = _element.style;
            style.translate = style.translate
                .Subtract(new Translate(_offset.X, _offset.Y))
                .Add(new Translate(offset.X, offset.Y));
            _offset = offset;
        }

        public override void OnDetach()
        {
            _subscription?.Dispose();
            _subscription = null;
            if (_element != null && (_offset.X != 0f || _offset.Y != 0f))
                SetOffset(Offset.Zero);
            _element = null;
            _offset = default;
        }
    }
}
