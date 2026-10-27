// ReSharper disable CheckNamespace

using System;
using Compose.Net;
using SharpExtensions;
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
        var density = UnityDensity.Current;
        var sideOffset = BorderWidth.ToPx(density) / 2;
        scope.DrawRoundRect(
            brush: Brush,
            topLeft: new Offset(sideOffset, sideOffset),
            size: scope.Size - new Size(sideOffset * 2),
            cornerRadius: Shape.TopLeft.ToPx(density),
            style: Stroke(BorderWidth.ToPx(density))
        );
    };

    protected override void OnAttached(VisualElement element)
    {
        var width = BorderWidth.ToPx(UnityDensity.Current);
        element.style.borderTopWidth = width;
        element.style.borderBottomWidth = width;
        element.style.borderLeftWidth = width;
        element.style.borderRightWidth = width;
    }

    protected override void OnDetached(VisualElement element)
    {
        element.style.borderTopWidth = StyleKeyword.Null;
        element.style.borderBottomWidth = StyleKeyword.Null;
        element.style.borderLeftWidth = StyleKeyword.Null;
        element.style.borderRightWidth = StyleKeyword.Null;
    }
}
