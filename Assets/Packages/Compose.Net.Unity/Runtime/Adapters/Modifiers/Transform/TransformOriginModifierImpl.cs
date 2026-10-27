// ReSharper disable CheckNamespace

using Compose.Net;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record TransformOriginModifierImpl(Dp X, Dp Y)
    : ModifierNodeElement<TransformOriginModifierImpl.TransformOriginNode>
{
    public override TransformOriginNode Create() => new(X, Y);
    public override void Update(TransformOriginNode node) => node.Update(X, Y);
    public override object GetTarget(IReusableComposeNode node) => node.VisualElement();

    internal sealed class TransformOriginNode(Dp x, Dp y) : IModifier.Node
    {
        private Dp _x = x;
        private Dp _y = y;
        private StyleTransformOrigin _original;
        private VisualElement? _element;

        public override void OnAttach()
        {
            _element = (VisualElement)Target;
            _original = _element.style.transformOrigin;
            Apply(_x, _y);
        }

        internal void Update(Dp nextX, Dp nextY)
        {
            if (_x.Equals(nextX) && _y.Equals(nextY)) return;
            _x = nextX;
            _y = nextY;
            Apply(nextX, nextY);
        }

        public override void OnDetach()
        {
            if (_element != null)
                _element.style.transformOrigin = _original;
            _element = null;
        }

        private void Apply(Dp x, Dp y)
        {
            if (_element == null) return;
            _element.style.transformOrigin = new UnityEngine.UIElements.TransformOrigin(x.Value, y.Value);
        }
    }
}