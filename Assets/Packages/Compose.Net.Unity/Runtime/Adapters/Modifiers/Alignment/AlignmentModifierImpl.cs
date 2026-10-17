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
) : UnityModifier
{
    protected override void Apply(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    )
    {
        if (element.parent is Box)
        {
            Box.SetAlign(element, Align);
            return;
        }

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
        if (element.parent is Box)
        {
            Box.SetAlign(element, null);
            return;
        }

        if (element.parent.NotNull().style.flexDirection.value is not (FlexDirection.Row or FlexDirection.RowReverse))
            return;
        element.style.alignSelf = element.style.alignSelf.CompareAndSetNull(Align.ToAlign());
    }
}