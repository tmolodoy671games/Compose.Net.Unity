// ReSharper disable CheckNamespace

using System;
using Compose.Net;
using SharpExtensions;
using StableCollections;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record RotateModifierImpl(float Degrees) : UnityStyleModifier
{
    public override void UpdateStyle(UnityStyleModifierNode node)
    {
        node.Set<StyleRotate>(node.Styles.Rotate, new Rotate(Degrees));
    }

    private static StyleRotate ToNullIfNeeded(Rotate rotate)
    {
        return rotate.angle.value.AlmostEquals(0f) ? StyleKeyword.Null : rotate;
    }
}
