// ReSharper disable CheckNamespace

using System;
using Compose.Net;
using SharpExtensions;
using StableCollections;
using Compose.Net.Unity.Runtime.Adapters.Extensions.Values;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record SizeModifierImpl(
    Optional<Dp> Width,
    Optional<Dp> Height
) : UnityStyleModifier, IAppearanceModifier
{
    public override void UpdateStyle(UnityStyleModifierNode node)
    {
        if (Width.HasValue) node.Set<StyleLength>(node.Styles.Width, Width.Value.ToLength());
        if (Height.HasValue) node.Set<StyleLength>(node.Styles.Height, Height.Value.ToLength());
    }

}
