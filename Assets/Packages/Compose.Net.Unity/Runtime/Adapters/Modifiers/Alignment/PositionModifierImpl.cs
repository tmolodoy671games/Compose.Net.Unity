using Compose.Net;
using Compose.Net.Unity.Runtime.Adapters.Extensions.Values;
using UnityEngine.UIElements;

// ReSharper disable CheckNamespace

namespace Compose.Net.Unity;

internal record PositionModifierImpl(
    Dp Top,
    Dp Bottom,
    Dp Left,
    Dp Right
) : ModifierNodeElement<PositionModifierImpl.PositionNode>
{
    public override PositionNode Create() => new(Top, Bottom, Left, Right);
    public override void Update(PositionNode node) => node.Update(Top, Bottom, Left, Right);
    public override object GetTarget(IReusableComposeNode node) => node.VisualElement();

    internal sealed class PositionNode(
        Dp top,
        Dp bottom,
        Dp left,
        Dp right
    ) : IModifier.Node
    {
        private Dp _top = top;
        private Dp _bottom = bottom;
        private Dp _left = left;
        private Dp _right = right;
        private VisualElement? _element;

        public override void OnAttach()
        {
            _element = (VisualElement)Target;
            _element.style.position = Position.Absolute;
            Apply(1f);
        }

        internal void Update(Dp nextTop, Dp nextBottom, Dp nextLeft, Dp nextRight)
        {
            Apply(-1f);
            _top = nextTop;
            _bottom = nextBottom;
            _left = nextLeft;
            _right = nextRight;
            Apply(1f);
        }

        public override void OnDetach()
        {
            if (_element != null)
                Apply(-1f);
            _element = null;
        }

        private void Apply(float sign)
        {
            if (_element == null) return;
            var style = _element.style;
            var density = UnityDensity.Current;
            if (!_top.IsUnspecified)
                style.top = style.top.Add(sign * _top.ToPx(density));
            if (!_bottom.IsUnspecified)
                style.bottom = style.bottom.Add(sign * _bottom.ToPx(density));
            if (!_left.IsUnspecified)
                style.left = style.left.Add(sign * _left.ToPx(density));
            if (!_right.IsUnspecified)
                style.right = style.right.Add(sign * _right.ToPx(density));
        }
    }
}
