// ReSharper disable CheckNamespace

using System;
using Compose.Net;
using SharpExtensions;
using StableCollections;
using Compose.Net.Unity.Runtime.Adapters.Extensions.Values;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record SizeInModifierImpl(
    Optional<Dp> MinWidth,
    Optional<Dp> MaxWidth,
    Optional<Dp> MinHeight,
    Optional<Dp> MaxHeight
) : UnityStyleModifier, IAppearanceModifier
{
    public override void UpdateStyle(UnityStyleModifierNode node)
    {
        if (MinWidth.HasValue) node.Set<StyleLength>(node.Styles.MinWidth, MinWidth.Value.ToLength());
        if (MaxWidth.HasValue) node.Set<StyleLength>(node.Styles.MaxWidth, MaxWidth.Value.ToLength());
        if (MinHeight.HasValue) node.Set<StyleLength>(node.Styles.MinHeight, MinHeight.Value.ToLength());
        if (MaxHeight.HasValue) node.Set<StyleLength>(node.Styles.MaxHeight, MaxHeight.Value.ToLength());
    }

}
