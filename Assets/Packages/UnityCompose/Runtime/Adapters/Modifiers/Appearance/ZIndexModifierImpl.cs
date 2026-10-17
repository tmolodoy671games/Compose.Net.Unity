// ReSharper disable CheckNamespace

using Compose.Net;
using StableCollections;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityCompose;

internal record ZIndexModifierImpl(
    float ZIndex
) : UnityModifier, IAppearanceModifier
{
    protected override void Apply(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    )
    {
        element.style.zIndex = Mathf.RoundToInt(ZIndex);
    }

    protected override void Revert(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    )
    {
        if (element.style.zIndex == Mathf.RoundToInt(ZIndex))
            element.style.zIndex = StyleKeyword.Null;
    }
}
