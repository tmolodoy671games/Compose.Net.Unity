using System;
using Compose.Net;
using SharpExtensions;
using StableCollections;
using Compose.Net.Unity.Runtime.Adapters.Extensions;
using UnityEngine.UIElements;

// ReSharper disable CheckNamespace

namespace Compose.Net.Unity;

internal record VerticalAlignModifierImpl(
    Alignment.Vertical Align
) : UnityStyleModifier
{
    public override bool DependsOnChain => true;
    public override void UpdateStyle(UnityStyleModifierNode node)
    {
        if (node.Element.parent?.style.flexDirection.value is FlexDirection.Row or FlexDirection.RowReverse)
            node.Set<StyleEnum<Align>>(node.Styles.AlignSelf, Align.ToAlign());
    }

}
