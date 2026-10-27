// ReSharper disable CheckNamespace

using System;
using Compose.Net;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record PaintModifierImpl(
    IPainter Painter,
    float Alpha
) : DrawUnityModifier
{
    protected override Action<MeshGenerationContext> GenerateVisualContent { get; } = it =>
    {
        var scope = it.DrawScope();
        Painter.ApplyAlpha(Alpha);
        Painter.Draw(scope, new Rect(Offset.Zero, scope.Size));
    };
}