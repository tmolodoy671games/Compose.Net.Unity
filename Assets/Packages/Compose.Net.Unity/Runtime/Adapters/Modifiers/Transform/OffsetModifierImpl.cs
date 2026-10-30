// ReSharper disable CheckNamespace
using System;
using Compose.Net;
using UnityEngine.UIElements;
using Compose.Net.Unity.Runtime.Adapters.Extensions.Values;

namespace Compose.Net.Unity;

internal record OffsetModifierImpl(Dp X, Dp Y) : UnityModifierElement<OffsetModifierImpl.OffsetNode>
{
    public virtual bool Equals(OffsetModifierImpl? other) =>
        other != null && X.Equals(other.X) && Y.Equals(other.Y);

    public override int GetHashCode() => HashCode.Combine(X, Y);

    public override OffsetNode Create() => new(X.ToPx(UnityDensity.Current), Y.ToPx(UnityDensity.Current));
    public override void Update(OffsetNode node) => node.Update(X.ToPx(UnityDensity.Current), Y.ToPx(UnityDensity.Current));
    public override object GetTarget(IReusableComposeNode node) => node.VisualElement();

    internal sealed class OffsetNode(float x, float y) : IModifier.Node
    {
        private float _x = x;
        private float _y = y;
        private VisualElement? _element;

        public override void OnAttach()
        {
            _element = (VisualElement)Target;
            _element.usageHints |= UsageHints.DynamicTransform;
            Change(_x, _y);
        }

        internal void Update(float nx, float ny)
        {
            if (_x == nx && _y == ny) return;
            Change(nx - _x, ny - _y);
            _x = nx;
            _y = ny;
        }

        public override void OnDetach()
        {
            Change(-_x, -_y);
            _element = null;
        }

        private void Change(float x, float y)
        {
            if (_element != null)
                _element.style.translate = _element.style.translate.Add(new Translate(x, y));
        }
    }
}
