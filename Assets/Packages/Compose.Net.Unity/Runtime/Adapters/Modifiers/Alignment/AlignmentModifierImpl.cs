using Compose.Net;
using SharpExtensions;
using StableCollections;
using Compose.Net.Unity.Runtime.Adapters.Extensions;
using Compose.Net.Unity.Runtime.Adapters.Extensions.Values;
using UnityEngine.UIElements;

// ReSharper disable CheckNamespace

namespace Compose.Net.Unity;

internal record AlignModifierImpl(
    Alignment Align
) : UnityStyleModifier
{
    public override bool DependsOnChain => true;
    public override void UpdateStyle(UnityStyleModifierNode node)
    {
        if (node.Element.parent?.style.flexDirection.value is FlexDirection.Row or FlexDirection.RowReverse)
            node.Set<StyleEnum<Align>>(node.Styles.AlignSelf, Align.ToAlign());
    }

}
