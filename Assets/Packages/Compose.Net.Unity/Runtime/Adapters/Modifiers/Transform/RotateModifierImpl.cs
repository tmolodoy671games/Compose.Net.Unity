// ReSharper disable CheckNamespace

using System;
using Compose.Net;
using SharpExtensions;
using StableCollections;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record RotateModifierImpl(float Degrees) : UnityModifier
{
    protected override void Apply(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    )
    {
        StyleRotate newRotate = new Rotate(element.style.rotate.value.angle.value + Degrees);
        if (newRotate.value.angle.value == 0f)
            newRotate = StyleKeyword.Null;
        element.style.rotate = newRotate;
    }

    protected override void Revert(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    )
    {
        StyleRotate newRotate = ToNullIfNeeded(new Rotate(element.style.rotate.value.angle.value - Degrees));
        element.style.rotate = newRotate;
    }

    private static StyleRotate ToNullIfNeeded(Rotate rotate)
    {
        return rotate.angle.value.AlmostEquals(0f) ? StyleKeyword.Null : rotate;
    }
}