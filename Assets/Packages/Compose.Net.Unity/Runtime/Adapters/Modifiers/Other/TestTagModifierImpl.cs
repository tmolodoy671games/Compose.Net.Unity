using System;
using Compose.Net;
using StableCollections;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Runtime.Adapters.Modifiers.Other;

internal record TestTagModifierImpl(
    string Tag
) : UnityModifier
{

    protected override void OnAttach(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    )
    {
        element.name = Tag;
    }

    protected override void OnDetach(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    )
    {
        element.name = element.name == Tag ? null : Tag;
    }
}
