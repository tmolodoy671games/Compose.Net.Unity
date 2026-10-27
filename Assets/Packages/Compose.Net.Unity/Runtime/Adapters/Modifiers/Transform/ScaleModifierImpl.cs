// ReSharper disable CheckNamespace

using System;
using Compose.Net;
using SharpExtensions;
using StableCollections;
using Compose.Net.Unity.Runtime.Adapters.Extensions.Values;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record ScaleModifierImpl(
    float ScaleX,
    float ScaleY
) : UnityModifier
{

    protected override void Apply(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    )
    {
        element.style.scale = new Vector2(ScaleX, ScaleY);
    }

    protected override void Revert(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    )
    {
        element.style.scale = element.style.scale.CompareAndSetNull(new Vector2(ScaleX, ScaleY));
    }
    
}