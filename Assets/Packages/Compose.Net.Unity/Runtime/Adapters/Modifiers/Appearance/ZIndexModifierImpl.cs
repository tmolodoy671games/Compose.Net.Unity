// ReSharper disable CheckNamespace

using Compose.Net;
using StableCollections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record ZIndexModifierImpl(
    float ZIndex
) : UnityStyleModifier, IAppearanceModifier
{
    public override void UpdateStyle(UnityStyleModifierNode node)
    {
        node.Set<StyleInt>(node.Styles.ZIndex, Mathf.RoundToInt(ZIndex));
    }

}
