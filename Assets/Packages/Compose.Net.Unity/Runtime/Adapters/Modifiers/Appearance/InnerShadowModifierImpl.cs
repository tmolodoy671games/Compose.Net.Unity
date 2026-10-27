// ReSharper disable CheckNamespace

using Compose.Net;
using Compose.Net.Unity.Runtime.Adapters.Extensions;
using Compose.Net.Unity.Runtime.Adapters.Extensions.Values;
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
            Init(shadowElement, _shape, _shadow);
        }

        protected override void RevertFromShadow(
            UnityReusableComposeNode node,
            VisualElement element,
            VisualElement shadowElement
        )
        {
            element.style.overflow = StyleKeyword.Null;
            Revert(shadowElement, _shape, _shadow);
        }

        private static void Init(
            VisualElement shadowElement,
            Shape shape,
            Compose.Net.Shadow shadow
        )
        {
            var style = shadowElement.style;
            style.borderTopLeftRadius = shape.TopLeft.ToLength(UnityDensity.Current);
            style.borderTopRightRadius = shape.TopRight.ToLength(UnityDensity.Current);
            style.borderBottomLeftRadius = shape.BottomLeft.ToLength(UnityDensity.Current);
            style.borderBottomRightRadius = shape.BottomRight.ToLength(UnityDensity.Current);

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
            Shape shape,
            Compose.Net.Shadow shadow
        )
        {
            var style = shadowElement.style;
            style.borderTopLeftRadius = style.borderTopLeftRadius.CompareAndSetNull(shape.TopLeft.ToLength(UnityDensity.Current));
            style.borderTopRightRadius = style.borderTopRightRadius.CompareAndSetNull(shape.TopRight.ToLength(UnityDensity.Current));
            style.borderBottomLeftRadius = style.borderBottomLeftRadius.CompareAndSetNull(shape.BottomLeft.ToLength(UnityDensity.Current));
            style.borderBottomRightRadius = style.borderBottomRightRadius.CompareAndSetNull(shape.BottomRight.ToLength(UnityDensity.Current));

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
