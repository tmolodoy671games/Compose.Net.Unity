// ReSharper disable CheckNamespace

using System;
using Compose.Net;
using SharpExtensions;
using StableCollections;
using UnityEngine;
using UnityEngine.UIElements;
using Rect = Compose.Net.Rect;

namespace Compose.Net.Unity;

internal record BackgroundBrushModifierImpl(
    IBrush Brush,
    Optional<Shape> Shape
) : DrawUnityModifier, IAppearanceModifier
{
    protected override Action<MeshGenerationContext> GenerateVisualContent { get; } = evt =>
    {
        var scope = evt.DrawScope();
        scope.DrawShape(
            brush: Brush,
            shape: Shape
        );
    };
}
