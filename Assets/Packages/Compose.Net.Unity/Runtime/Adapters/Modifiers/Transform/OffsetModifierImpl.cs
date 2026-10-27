// ReSharper disable CheckNamespace

using System;
using Compose.Net;
using StableCollections;
using Compose.Net.Unity.Runtime.Adapters.Extensions.Values;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record OffsetModifierImpl(
    Dp X,
    Dp Y
) : UnityModifier
{
    protected override void Apply(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    )
    {
        element.style.translate = element.style.translate.Add(new Translate(X.ToLength(), Y.ToLength()));
    }

    protected override void Revert(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    )
    {
        var style = element.style;
        style.translate = style.translate.Subtract(new Translate(X.ToLength(), Y.ToLength()));
    }
}