// ReSharper disable CheckNamespace

using Compose.Net;
using Compose.Net.Unity.Runtime.Adapters.Extensions;
using Compose.Net.Unity.Runtime.Adapters.Extensions.Values;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record InnerShadowModifierImpl(
    Shape Shape,
    Compose.Net.Shadow Shadow
) : ForegroundShadowUnityModifier<InnerShadowModifierImpl.InnerShadowNode>, IAppearanceModifier
{
    public override InnerShadowNode Create() => new(Shape, Shadow);

    public override void Update(InnerShadowNode node) => node.Update(Shape, Shadow);

    internal sealed class InnerShadowNode(Shape shape, Compose.Net.Shadow shadow) : ForegroundShadowNodeBase
    {
        private Shape _shape = shape;
        private Compose.Net.Shadow _shadow = shadow;
        private Vector4 _radii;

        internal void Update(Shape nextShape, Compose.Net.Shadow nextShadow)
        {
            if (Element != null && Shadow != null)
                RevertFromShadow(UnityNode!, Element, Shadow);
            _shape = nextShape;
            _shadow = nextShadow;
            if (Element != null && Shadow != null)
                ApplyToShadow(UnityNode!, Element, Shadow);
        }

        protected override void ApplyToShadow(
            UnityReusableComposeNode node,
            VisualElement element,
            VisualElement shadowElement
        )
        {
            element.style.overflow = Overflow.Hidden;
            Init(shadowElement, _shadow);
            element.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            ApplyCorners();
        }

        protected override void RevertFromShadow(
            UnityReusableComposeNode node,
            VisualElement element,
            VisualElement shadowElement
        )
        {
            element.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            element.style.overflow = StyleKeyword.Null;
            Revert(shadowElement, _radii, _shadow);
        }

        private void OnGeometryChanged(GeometryChangedEvent evt) => ApplyCorners();

        private void ApplyCorners()
        {
            if (Element == null || Shadow == null) return;
            var size = Element.layout.size;
            if (!float.IsFinite(size.x) || !float.IsFinite(size.y)) return;
            var radii = RoundedCorners.Resolve(_shape, size);
            var spread = _shadow.Spread.ToPx(UnityDensity.Current);
            // The shadow border extends by Spread on each side. Resolve the original
            // shape first so radii above the cap produce identical shadow geometry.
            var limit = Mathf.Max(0, Mathf.Min(size.x, size.y) / 2 + spread);
            _radii = new Vector4(Mathf.Clamp(radii.x + spread, 0, limit),
                Mathf.Clamp(radii.y + spread, 0, limit),
                Mathf.Clamp(radii.z + spread, 0, limit),
                Mathf.Clamp(radii.w + spread, 0, limit));
            RoundedCorners.Apply(Shadow, _radii);
        }

        private static void Init(
            VisualElement shadowElement,
            Compose.Net.Shadow shadow
        )
        {
            var style = shadowElement.style;

            style.borderTopColor = shadow.Color.ToUnityColor();
            style.borderBottomColor = shadow.Color.ToUnityColor();
            style.borderLeftColor = shadow.Color.ToUnityColor();
            style.borderRightColor = shadow.Color.ToUnityColor();

            var borderWidth = shadow.Spread * 2;
            style.borderTopWidth = borderWidth.ToPx(UnityDensity.Current);
            style.borderBottomWidth = borderWidth.ToPx(UnityDensity.Current);
            style.borderLeftWidth = borderWidth.ToPx(UnityDensity.Current);
            style.borderRightWidth = borderWidth.ToPx(UnityDensity.Current);

            var offsetWidth = (-borderWidth / 2).ToLength(UnityDensity.Current);
            style.top = offsetWidth;
            style.bottom = offsetWidth;
            style.left = offsetWidth;
            style.right = offsetWidth;

            style.translate = shadow.Offset.ToVector2();

            var blur = new FilterFunction(FilterFunctionType.Blur);
            blur.AddParameter(new FilterParameter(shadow.Radius.ToPx(UnityDensity.Current)));
            style.AddFilter(blur);
        }

        private static void Revert(
            VisualElement shadowElement,
            Vector4 radii,
            Compose.Net.Shadow shadow
        )
        {
            var style = shadowElement.style;
            style.borderTopLeftRadius = style.borderTopLeftRadius.CompareAndSetNull(new Length(radii.x));
            style.borderTopRightRadius = style.borderTopRightRadius.CompareAndSetNull(new Length(radii.y));
            style.borderBottomLeftRadius = style.borderBottomLeftRadius.CompareAndSetNull(new Length(radii.z));
            style.borderBottomRightRadius = style.borderBottomRightRadius.CompareAndSetNull(new Length(radii.w));

            style.borderTopColor = style.borderTopColor.CompareAndSetNull(shadow.Color.ToUnityColor());
            style.borderBottomColor = style.borderBottomColor.CompareAndSetNull(shadow.Color.ToUnityColor());
            style.borderLeftColor = style.borderLeftColor.CompareAndSetNull(shadow.Color.ToUnityColor());
            style.borderRightColor = style.borderRightColor.CompareAndSetNull(shadow.Color.ToUnityColor());

            var borderWidth = shadow.Spread * 2;
            style.borderTopWidth = style.borderTopWidth.CompareAndSetNull(borderWidth.ToPx(UnityDensity.Current));
            style.borderBottomWidth = style.borderBottomWidth.CompareAndSetNull(borderWidth.ToPx(UnityDensity.Current));
            style.borderLeftWidth = style.borderLeftWidth.CompareAndSetNull(borderWidth.ToPx(UnityDensity.Current));
            style.borderRightWidth = style.borderRightWidth.CompareAndSetNull(borderWidth.ToPx(UnityDensity.Current));

            var offsetWidth = (-borderWidth / 2).ToLength(UnityDensity.Current);
            style.top = style.top.CompareAndSetNull(offsetWidth);
            style.bottom = style.bottom.CompareAndSetNull(offsetWidth);
            style.left = style.left.CompareAndSetNull(offsetWidth);
            style.right = style.right.CompareAndSetNull(offsetWidth);

            style.translate = style.translate.CompareAndSetNull(shadow.Offset.ToVector2());

            var blur = new FilterFunction(FilterFunctionType.Blur);
            blur.AddParameter(new FilterParameter(shadow.Radius.ToPx(UnityDensity.Current)));
            style.RemoveFilter(blur);
        }
    }
}
