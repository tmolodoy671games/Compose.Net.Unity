// ReSharper disable CheckNamespace

using System;
using Compose.Net;
using SharpExtensions;
using StableCollections;
using Compose.Net.Unity.Runtime.Adapters.Extensions.Values;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record ClipModifierImpl(
    Optional<Shape> Shape
) : UnityModifier, IAppearanceModifier
{
    protected override void Apply(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    )
    {
        element.Clip().Increment();
        if (!Shape.HasValue)
            return;
        var shapeValue = Shape.Value;
        element.style.borderTopLeftRadius = shapeValue.TopLeft.ToLength();
        element.style.borderTopRightRadius = shapeValue.TopRight.ToLength();
        element.style.borderBottomLeftRadius = shapeValue.BottomLeft.ToLength();
        element.style.borderBottomRightRadius = shapeValue.BottomRight.ToLength();
    }

    protected override void Revert(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    )
    {
        element.Clip().Decrement();
        if (!Shape.HasValue)
            return;
        var shapeValue = Shape.Value;
        var style = element.style;
        style.borderTopLeftRadius = style.borderTopLeftRadius.CompareAndSetNull(shapeValue.TopLeft.ToLength());
        style.borderTopRightRadius = style.borderTopRightRadius.CompareAndSetNull(shapeValue.TopRight.ToLength());
        style.borderBottomLeftRadius = style.borderBottomLeftRadius.CompareAndSetNull(shapeValue.BottomLeft.ToLength());
        style.borderBottomRightRadius =
            style.borderBottomRightRadius.CompareAndSetNull(shapeValue.BottomRight.ToLength());
    }
}