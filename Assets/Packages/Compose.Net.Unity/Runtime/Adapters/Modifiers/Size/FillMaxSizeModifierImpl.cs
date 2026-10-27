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
) : UnityStyleModifier, IAppearanceModifier
{
    public override void UpdateStyle(UnityStyleModifierNode node)
    {
        if (WidthFraction > 0) node.Set<StyleLength>(node.Styles.Width, new Length(WidthFraction * 100, LengthUnit.Percent));
        if (HeightFraction > 0) node.Set<StyleLength>(node.Styles.Height, new Length(HeightFraction * 100, LengthUnit.Percent));
    }

}
