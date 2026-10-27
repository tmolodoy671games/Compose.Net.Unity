// ReSharper disable CheckNamespace

using System;
using Compose.Net;
using SharpExtensions;
using StableCollections;
using Compose.Net.Unity.Runtime.Adapters.Extensions.Values;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record WeightModifierImpl(
    float Weight
) : UnityStyleModifier
{
    public override void UpdateStyle(UnityStyleModifierNode node)
    {
        node.Set<StyleFloat>(node.Styles.FlexGrow, Weight);
    }

}
