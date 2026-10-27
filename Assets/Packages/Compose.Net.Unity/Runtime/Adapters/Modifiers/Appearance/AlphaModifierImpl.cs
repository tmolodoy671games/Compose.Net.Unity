// ReSharper disable CheckNamespace

using Compose.Net;
using SharpExtensions;
using StableCollections;
using Compose.Net.Unity.Runtime.Adapters.Extensions.Values;
using UnityEngine.UIElements;
using HashCode = System.HashCode;

namespace Compose.Net.Unity;

internal record AlphaModifierImpl(
    float Alpha
) : UnityStyleModifier, IAppearanceModifier
{
    public override void UpdateStyle(UnityStyleModifierNode node)
    {
        node.Set<StyleFloat>(node.Styles.Opacity, Alpha);
    }

}
