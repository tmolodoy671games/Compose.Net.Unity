// ReSharper disable CheckNamespace

using Compose.Net;
using StableCollections;
using Compose.Net.Unity.Runtime.Adapters.Extensions.Values;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record FloatModifierImpl(bool Enabled) : UnityModifier
{
    protected override void Apply(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    )
    {
        if (!Enabled)
            return;
        element.style.position = Position.Absolute;
    }

    protected override void Revert(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    )
    {
        if (!Enabled)
            return;
        element.style.position = element.style.position.CompareAndSetNull(Position.Absolute);
    }
}