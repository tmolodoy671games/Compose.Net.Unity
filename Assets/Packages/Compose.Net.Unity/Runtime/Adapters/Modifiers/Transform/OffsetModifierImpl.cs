// ReSharper disable CheckNamespace
using Compose.Net;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record OffsetModifierImpl(Dp X, Dp Y) : ModifierNodeElement<OffsetModifierImpl.OffsetNode>
{
    public override OffsetNode Create() => new(X.Value, Y.Value);
    public override void Update(OffsetNode node) => node.Update(X.Value, Y.Value);
    public override object GetTarget(IReusableComposeNode node) => node.VisualElement();

    internal sealed class OffsetNode(float x, float y) : IModifier.Node
    {
        private float _x = x;
        private float _y = y;
        private VisualElement? _element;

        public override void OnAttach()
        {
            _element = (VisualElement)Target;
            _element.style.translate = new Translate(_x, _y);
        }

        internal void Update(float nx, float ny)
        {
            if (_x == nx && _y == ny) return;
            _x = nx;
            _y = ny;
            if (_element != null)
                _element.style.translate = new Translate(nx, ny);
        }

        public override void OnDetach()
        {
            if (_element != null)
                _element.style.translate = StyleKeyword.None;
            _element = null;
        }
    }
}
