// ReSharper disable CheckNamespace

using System;
using Compose.Net;
using StableCollections;
using Compose.Net.Unity.Runtime.Adapters.Extensions.Values;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record TransformOriginModifierImpl(
    Dp X,
    Dp Y
) : UnityStyleModifier
{
    public override void UpdateStyle(UnityStyleModifierNode node)
    {
        node.Set<StyleTransformOrigin>(node.Styles.TransformOrigin, new UnityEngine.UIElements.TransformOrigin(X.ToLength(), Y.ToLength()));
    }

}
