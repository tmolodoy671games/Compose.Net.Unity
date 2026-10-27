// ReSharper disable CheckNamespace
using Compose.Net;
using Compose.Net.Unity.Runtime.Adapters.Extensions.Values;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record OffsetModifierImpl(Dp X, Dp Y) : ModifierNodeElement<OffsetModifierImpl.OffsetNode>
{
    public override OffsetNode Create() => new(X, Y);
    public override void Update(OffsetNode node) => node.Update(X, Y);
    public override object GetTarget(IReusableComposeNode node) => node.VisualElement();

    internal sealed class OffsetNode : IModifier.Node
    {
        private Dp _x;
        private Dp _y;
        private VisualElement? _element;

        internal OffsetNode(Dp x, Dp y)
        {
            _x = x;
            _y = y;
        }

        public override void OnAttach()
        {
            _element = (VisualElement)Target;
            _element.style.translate = _element.style.translate.Add(new Translate(_x.ToLength(), _y.ToLength()));
        }

        internal void Update(Dp x, Dp y)
        {
            if (_x == x && _y == y) return;
            if (_element != null)
                _element.style.translate = _element.style.translate.Add(
                    new Translate((x - _x).ToLength(), (y - _y).ToLength()));
            _x = x;
            _y = y;
        }

        public override void OnDetach()
        {
            if (_element != null)
                _element.style.translate = _element.style.translate.Subtract(
                    new Translate(_x.ToLength(), _y.ToLength()));
            _element = null;
        }
    }
}
