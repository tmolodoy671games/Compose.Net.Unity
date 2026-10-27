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
    public override void UpdateStyle(UnityStyleModifierNode node)
    {
        node.Set<StyleFloat>(node.Styles.BorderTopWidth, BorderWidth.Value);
        node.Set<StyleFloat>(node.Styles.BorderBottomWidth, BorderWidth.Value);
        node.Set<StyleFloat>(node.Styles.BorderLeftWidth, BorderWidth.Value);
        node.Set<StyleFloat>(node.Styles.BorderRightWidth, BorderWidth.Value);
    }

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




}
