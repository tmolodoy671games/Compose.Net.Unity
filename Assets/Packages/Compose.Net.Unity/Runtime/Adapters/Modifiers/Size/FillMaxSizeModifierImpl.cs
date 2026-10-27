// ReSharper disable CheckNamespace

using System;
using Compose.Net;
using SharpExtensions;
using StableCollections;
using Compose.Net.Unity.Runtime.Adapters.Extensions.Values;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record FillMaxSizeModifierImpl(
    float WidthFraction,
    float HeightFraction
) : UnityModifier, IAppearanceModifier
{
    protected override void Apply(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    )
    {
        var style = element.style;
        if (WidthFraction > 0)
            style.width = new Length(WidthFraction * 100, LengthUnit.Percent);
        if (HeightFraction > 0)
            style.height = new Length(HeightFraction * 100, LengthUnit.Percent);
    }

    protected override void Revert(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    )
    {
        var style = element.style;
        if (WidthFraction > 0)
            style.width = style.width.CompareAndSetNull(new Length(WidthFraction * 100, LengthUnit.Percent));
        if (HeightFraction > 0)
            style.height = style.height.CompareAndSetNull(new Length(HeightFraction * 100, LengthUnit.Percent));
    }
}