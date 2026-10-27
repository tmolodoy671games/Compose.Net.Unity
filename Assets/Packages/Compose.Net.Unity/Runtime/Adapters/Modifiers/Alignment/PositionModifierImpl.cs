using Compose.Net;
using SharpExtensions;
using Compose.Net.Unity.Runtime.Adapters.Extensions.Values;
using UnityEngine.UIElements;

// ReSharper disable CheckNamespace

namespace Compose.Net.Unity;

internal record PositionModifierImpl(
    Optional<Dp> Top,
    Optional<Dp> Bottom,
    Optional<Dp> Left,
    Optional<Dp> Right
) : ModifierNodeElement<PositionModifierImpl.PositionNode>
{
    public override PositionNode Create() => new(Top, Bottom, Left, Right);
    public override void Update(PositionNode node) => node.Update(Top, Bottom, Left, Right);
    public override object GetTarget(IReusableComposeNode node) => node.VisualElement();

    internal sealed class PositionNode(
        Optional<Dp> top,
        Optional<Dp> bottom,
        Optional<Dp> left,
        Optional<Dp> right
    ) : IModifier.Node
    {
        private Optional<Dp> _top = top;
        private Optional<Dp> _bottom = bottom;
        private Optional<Dp> _left = left;
        private Optional<Dp> _right = right;
        private VisualElement? _element;

        public override void OnAttach()
        {
            _element = (VisualElement)Target;
            _element.style.position = Position.Absolute;
            Apply(1f);
        }

        internal void Update(Optional<Dp> nextTop, Optional<Dp> nextBottom, Optional<Dp> nextLeft, Optional<Dp> nextRight)
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
            if (_top.HasValue)
                style.top = style.top.Add(sign * _top.Value.Value);
            if (_bottom.HasValue)
                style.bottom = style.bottom.Add(sign * _bottom.Value.Value);
            if (_left.HasValue)
                style.left = style.left.Add(sign * _left.Value.Value);
            if (_right.HasValue)
                style.right = style.right.Add(sign * _right.Value.Value);
        }
    }
}
