// ReSharper disable CheckNamespace

using System.Collections.Generic;
using System.Linq;
using Compose.Net;
using StableCollections;
using Compose.Net.Unity.Runtime.Adapters.Extensions;
using Compose.Net.Unity.Runtime.Adapters.Extensions.Values;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record DropShadowModifierImpl(
    Shape Shape,
    Compose.Net.Shadow Shadow
) : UnityModifier
{
    protected override void Apply(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    )
    {
        if (Shadow.Color.Alpha <= 0f)
            return;
        Init(element, Shadow);
    }

    protected override void Revert(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    )
    {
        if (Shadow.Color.Alpha <= 0f)
            return;
        Revert(element, Shadow);
    }

    private static void Init(
        VisualElement shadowElement,
        Compose.Net.Shadow shadow
    )
    {
        var shadowFilter = new FilterFunction(FilterFunctionType.DropShadow);
        shadowFilter.AddParameter(new FilterParameter(shadow.Offset.X));
        shadowFilter.AddParameter(new FilterParameter(shadow.Offset.Y));
        shadowFilter.AddParameter(new FilterParameter(shadow.Radius.Value));
        shadowFilter.AddParameter(new FilterParameter(shadow.Color.ToUnityColor()));
        shadowElement.style.AddFilters(shadowFilter, shadowFilter);
    }

    private static void Revert(
        VisualElement shadowElement,
        Compose.Net.Shadow shadow
    )
    {
        var shadowFilter = new FilterFunction(FilterFunctionType.DropShadow);
        shadowFilter.AddParameter(new FilterParameter(shadow.Offset.X));
        shadowFilter.AddParameter(new FilterParameter(shadow.Offset.Y));
        shadowFilter.AddParameter(new FilterParameter(shadow.Radius.Value));
        shadowFilter.AddParameter(new FilterParameter(shadow.Color.ToUnityColor()));
        shadowElement.style.RemoveFilters(shadowFilter, shadowFilter);
    }
}