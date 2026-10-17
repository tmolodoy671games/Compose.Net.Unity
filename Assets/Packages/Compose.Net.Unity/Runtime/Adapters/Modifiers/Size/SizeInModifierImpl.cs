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
) : UnityModifier, IAppearanceModifier
{
    protected override void Apply(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    )
    {
        var style = element.style;
        if (MinWidth.HasValue)
            style.minWidth = MinWidth.Value.ToLength();
        if (MaxWidth.HasValue)
            style.maxWidth = MaxWidth.Value.ToLength();
        if (MinHeight.HasValue)
            style.minHeight = MinHeight.Value.ToLength();
        if (MaxHeight.HasValue)
            style.maxHeight = MaxHeight.Value.ToLength();
    }

    protected override void Revert(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    )
    {
        var style = element.style;
        if (MinWidth.HasValue)
            style.minWidth = style.minWidth.CompareAndSetNull(MinWidth.Value.ToLength());
        if (MaxWidth.HasValue)
            style.maxWidth = style.maxWidth.CompareAndSetNull(MaxWidth.Value.ToLength());
        if (MinHeight.HasValue)
            style.minHeight = style.minHeight.CompareAndSetNull(MinHeight.Value.ToLength());
        if (MaxHeight.HasValue)
            style.maxHeight = style.maxHeight.CompareAndSetNull(MaxHeight.Value.ToLength());
    }
}