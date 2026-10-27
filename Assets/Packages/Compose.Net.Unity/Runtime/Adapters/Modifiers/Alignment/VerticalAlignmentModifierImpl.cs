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
) : UnityModifier
{
    protected override void Apply(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    )
    {
        if (element.parent.NotNull().style.flexDirection.value is not (FlexDirection.Row or FlexDirection.RowReverse))
            return;
        element.style.alignSelf = Align.ToAlign();
    }

    protected override void Revert(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    )
    {
        if (element.parent.NotNull().style.flexDirection.value is not (FlexDirection.Row or FlexDirection.RowReverse))
            return;
        element.style.alignSelf = StyleKeyword.Null;
    }
}