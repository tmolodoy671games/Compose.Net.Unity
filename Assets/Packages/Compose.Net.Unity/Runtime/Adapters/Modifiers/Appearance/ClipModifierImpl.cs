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
) : UnityStyleModifier, IAppearanceModifier
{
    public override void UpdateStyle(UnityStyleModifierNode node)
    {
        node.Set<StyleEnum<Overflow>>(node.Styles.Overflow, Overflow.Hidden);
        if (!Shape.HasValue) return;
        node.Set<StyleLength>(node.Styles.BorderTopLeftRadius, Shape.Value.TopLeft.ToLength());
        node.Set<StyleLength>(node.Styles.BorderTopRightRadius, Shape.Value.TopRight.ToLength());
        node.Set<StyleLength>(node.Styles.BorderBottomLeftRadius, Shape.Value.BottomLeft.ToLength());
        node.Set<StyleLength>(node.Styles.BorderBottomRightRadius, Shape.Value.BottomRight.ToLength());
    }

}
