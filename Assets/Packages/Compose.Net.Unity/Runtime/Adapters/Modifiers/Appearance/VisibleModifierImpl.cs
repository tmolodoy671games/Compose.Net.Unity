// ReSharper disable CheckNamespace

using Compose.Net;
using StableCollections;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record VisibleModifierImpl(
    bool Visible
) : UnityStyleModifier, IAppearanceModifier
{
    public override void UpdateStyle(UnityStyleModifierNode node)
    {
        node.Set<StyleEnum<Visibility>>(node.Styles.Visibility, Visible ? Visibility.Visible : Visibility.Hidden);
    }

}
