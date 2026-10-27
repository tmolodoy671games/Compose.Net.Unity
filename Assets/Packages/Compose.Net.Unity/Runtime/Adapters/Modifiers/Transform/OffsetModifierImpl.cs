// ReSharper disable CheckNamespace

using System;
using Compose.Net;
using StableCollections;
using Compose.Net.Unity.Runtime.Adapters.Extensions.Values;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record OffsetModifierImpl(
    Dp X,
    Dp Y
) : UnityStyleModifier
{
    public override void UpdateStyle(UnityStyleModifierNode node)
    {
        node.Set<StyleTranslate>(node.Styles.Translate, new Translate(X.ToLength(), Y.ToLength()));
    }

}
