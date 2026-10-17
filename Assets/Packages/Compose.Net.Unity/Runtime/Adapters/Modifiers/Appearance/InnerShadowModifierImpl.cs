// ReSharper disable CheckNamespace

using System;
using System.Collections.Generic;
using System.Linq;
using Compose.Net;
using StableCollections;
using Compose.Net.Unity.Runtime.Adapters.Extensions;
using Compose.Net.Unity.Runtime.Adapters.Extensions.Values;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record InnerShadowModifierImpl(
    Shape Shape,
    Compose.Net.Shadow Shadow
) : ForegroundShadowUnityModifier, IAppearanceModifier
{
    protected override void Apply(
        UnityReusableComposeNode node,
        VisualElement element,
        VisualElement drawBehind,
        IStableList<IModifier> newModifiers
    )
    {
        element.style.overflow = Overflow.Hidden;
        Init(drawBehind, Shape, Shadow);
    }

    protected override void Revert(
        UnityReusableComposeNode node,
        VisualElement element,
        VisualElement drawBehind,
        IStableList<IModifier> newModifiers
    )
    {
        element.style.overflow = StyleKeyword.Null;
        Revert(drawBehind, Shape, Shadow);
    }

    private static void Init(
        VisualElement shadowElement,
        Shape shape,
        Compose.Net.Shadow shadow
    )
    {
        var style = shadowElement.style;
        style.borderTopLeftRadius = shape.TopLeft.ToLength();
        style.borderTopRightRadius = shape.TopRight.ToLength();
        style.borderBottomLeftRadius = shape.BottomLeft.ToLength();
        style.borderBottomRightRadius = shape.BottomRight.ToLength();

        style.borderTopColor = shadow.Color.ToUnityColor();
        style.borderBottomColor = shadow.Color.ToUnityColor();
        style.borderLeftColor = shadow.Color.ToUnityColor();
        style.borderRightColor = shadow.Color.ToUnityColor();

        var borderWidth = shadow.Spread * 2;
        style.borderTopWidth = borderWidth.Value;
        style.borderBottomWidth = borderWidth.Value;
        style.borderLeftWidth = borderWidth.Value;
        style.borderRightWidth = borderWidth.Value;

        var offsetWidth = (-borderWidth / 2).ToLength();
        style.top = offsetWidth;
        style.bottom = offsetWidth;
        style.left = offsetWidth;
        style.right = offsetWidth;

        style.translate = shadow.Offset.ToVector2();

        var blur = new FilterFunction(FilterFunctionType.Blur);
        blur.AddParameter(new FilterParameter(shadow.Radius.Value));
        style.AddFilter(blur);
    }

    private static void Revert(
        VisualElement shadowElement,
        Shape shape,
        Compose.Net.Shadow shadow
    )
    {
        var style = shadowElement.style;
        style.borderTopLeftRadius = style.borderTopLeftRadius.CompareAndSetNull(shape.TopLeft.ToLength());
        style.borderTopRightRadius = style.borderTopRightRadius.CompareAndSetNull(shape.TopRight.ToLength());
        style.borderBottomLeftRadius = style.borderBottomLeftRadius.CompareAndSetNull(shape.BottomLeft.ToLength());
        style.borderBottomRightRadius = style.borderBottomRightRadius.CompareAndSetNull(shape.BottomRight.ToLength());

        style.borderTopColor = style.borderTopColor.CompareAndSetNull(shadow.Color.ToUnityColor());
        style.borderBottomColor = style.borderBottomColor.CompareAndSetNull(shadow.Color.ToUnityColor());
        style.borderLeftColor = style.borderLeftColor.CompareAndSetNull(shadow.Color.ToUnityColor());
        style.borderRightColor = style.borderRightColor.CompareAndSetNull(shadow.Color.ToUnityColor());

        var borderWidth = shadow.Spread * 2;
        style.borderTopWidth = style.borderTopWidth.CompareAndSetNull(borderWidth.Value);
        style.borderBottomWidth = style.borderBottomWidth.CompareAndSetNull(borderWidth.Value);
        style.borderLeftWidth = style.borderLeftWidth.CompareAndSetNull(borderWidth.Value);
        style.borderRightWidth = style.borderRightWidth.CompareAndSetNull(borderWidth.Value);

        var offsetWidth = (-borderWidth / 2).ToLength();
        style.top = style.top.CompareAndSetNull(offsetWidth);
        style.bottom = style.bottom.CompareAndSetNull(offsetWidth);
        style.left = style.left.CompareAndSetNull(offsetWidth);
        style.right = style.right.CompareAndSetNull(offsetWidth);

        style.translate = style.translate.CompareAndSetNull(shadow.Offset.ToVector2());

        var blur = new FilterFunction(FilterFunctionType.Blur);
        blur.AddParameter(new FilterParameter(shadow.Radius.Value));
        style.RemoveFilter(blur);
    }
}