using System;
using Compose.Net;
using SharpExtensions;
using StableCollections;
using Compose.Net.Unity.Runtime.Adapters.Extensions.Values;
using UnityEngine.UIElements;

// ReSharper disable CheckNamespace

namespace Compose.Net.Unity;

internal record PositionModifierImpl(
    Optional<Dp> Top,
    Optional<Dp> Bottom,
    Optional<Dp> Left,
    Optional<Dp> Right
) : UnityStyleModifier
{
    public override void UpdateStyle(UnityStyleModifierNode node)
    {
        node.Set<StyleEnum<Position>>(node.Styles.Position, Position.Absolute);
        if (Top.HasValue) node.Set<StyleLength>(node.Styles.Top, Top.Value.ToLength());
        if (Bottom.HasValue) node.Set<StyleLength>(node.Styles.Bottom, Bottom.Value.ToLength());
        if (Left.HasValue) node.Set<StyleLength>(node.Styles.Left, Left.Value.ToLength());
        if (Right.HasValue) node.Set<StyleLength>(node.Styles.Right, Right.Value.ToLength());
    }

}
