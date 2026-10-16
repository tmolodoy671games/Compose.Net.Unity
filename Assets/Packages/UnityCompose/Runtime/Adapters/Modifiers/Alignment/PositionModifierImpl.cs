using System;
using Compose.Net;
using SharpExtensions;
using StableCollections;
using UnityCompose.Packages.UnityCompose.Runtime.Adapters.Extensions.Values;
using UnityEngine.UIElements;

// ReSharper disable CheckNamespace

namespace UnityCompose;

internal record PositionModifierImpl(
    Optional<Dp> Top,
    Optional<Dp> Bottom,
    Optional<Dp> Left,
    Optional<Dp> Right
) : UnityModifier
{
    protected override void Apply(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    )
    {
        var style = element.style;
        style.position = Position.Absolute;
        if (Top.HasValue)
            style.top = style.top.Add(Top.Value.Value);
        if (Bottom.HasValue)
            style.bottom = style.bottom.Add(Bottom.Value.Value);
        if (Left.HasValue)
            style.left = style.left.Add(Left.Value.Value);
        if (Right.HasValue)
            style.right = style.right.Add(Right.Value.Value);
    }

    protected override void Revert(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    )
    {
        var style = element.style;
        if (Top.HasValue)
            style.top = style.top.Add(-Top.Value.Value);
        if (Bottom.HasValue)
            style.bottom = style.bottom.Add(-Bottom.Value.Value);
        if (Left.HasValue)
            style.left = style.left.Add(-Left.Value.Value);
        if (Right.HasValue)
            style.right = style.right.Add(-Right.Value.Value);
    }
}