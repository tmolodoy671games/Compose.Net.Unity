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
) : UnityModifier, IAppearanceModifier
{
    protected override void Apply(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    )
    {
        var style = element.style;
        if (Width.HasValue)
            style.width = Width.Value.ToLength();
        if (Height.HasValue)
            style.height = Height.Value.ToLength();
    }

    protected override void Revert(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    )
    {
        var style = element.style;
        if (Width.HasValue)
            style.width = style.width.CompareAndSetNull(Width.Value.ToLength());
        if (Height.HasValue)
            style.height = style.height.CompareAndSetNull(Height.Value.ToLength());
    }
}