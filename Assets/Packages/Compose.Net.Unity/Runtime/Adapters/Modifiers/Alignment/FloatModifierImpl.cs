// ReSharper disable CheckNamespace

using Compose.Net;
using StableCollections;
using Compose.Net.Unity.Runtime.Adapters.Extensions.Values;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record FloatModifierImpl(bool Enabled) : UnityStyleModifier
{
    public override void UpdateStyle(UnityStyleModifierNode node)
    {
        node.Set<StyleEnum<Position>>(node.Styles.Position, Position.Absolute);
    }

}
