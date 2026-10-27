// ReSharper disable CheckNamespace

using System;
using Compose.Net;
using StableCollections;
using Compose.Net.Unity.Runtime.Adapters.Extensions.Values;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record TransformOriginModifierImpl(
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
        element.style.transformOrigin = new UnityEngine.UIElements.TransformOrigin(X.ToLength(), Y.ToLength());
    }

    protected override void Revert(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    )
    {
        var style = element.style;
        style.transformOrigin =
            style.transformOrigin.CompareAndSetNull(new UnityEngine.UIElements.TransformOrigin(X.ToLength(), Y.ToLength()));
    }
}