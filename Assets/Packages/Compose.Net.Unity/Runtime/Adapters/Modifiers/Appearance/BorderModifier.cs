// ReSharper disable CheckNamespace

using System;
using Compose.Net;
using SharpExtensions;
using StableCollections;
using UnityEngine;
using UnityEngine.UIElements;
using Rect = Compose.Net.Rect;

namespace Compose.Net.Unity;

internal record BorderModifierImpl(
    Dp BorderWidth,
    IBrush Brush,
    Shape Shape
) : DrawUnityModifier
{
    protected override Action<MeshGenerationContext> GenerateVisualContent { get; } = it =>
    {
        var scope = it.DrawScope();
        var sideOffset = BorderWidth.Value / 2;
        scope.DrawRoundRect(
            brush: Brush,
            topLeft: new Offset(sideOffset, sideOffset),
            size: scope.Size - new Size(sideOffset * 2),
            cornerRadius: Shape.TopLeft.Value,
            style: Stroke(BorderWidth.Value)
        );
    };

    protected override void OnApply(UnityReusableComposeNode node, VisualElement element, IStableList<IModifier> newModifiers)
    {
        element.style.borderTopWidth = BorderWidth.Value;
        element.style.borderBottomWidth = BorderWidth.Value;
        element.style.borderLeftWidth = BorderWidth.Value;
        element.style.borderRightWidth = BorderWidth.Value;
    }

    protected override void OnRevert(UnityReusableComposeNode node, VisualElement element, IStableList<IModifier> newModifiers)
    {
        element.style.borderTopWidth = StyleKeyword.Null;
        element.style.borderBottomWidth = StyleKeyword.Null;
        element.style.borderLeftWidth = StyleKeyword.Null;
        element.style.borderRightWidth = StyleKeyword.Null;
    }
}