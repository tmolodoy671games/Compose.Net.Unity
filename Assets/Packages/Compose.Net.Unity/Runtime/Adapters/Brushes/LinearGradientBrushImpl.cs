using System;
using Compose.Net;
using StableCollections;
using UnityEngine;
using UnityEngine.UIElements;
using Rect = Compose.Net.Rect;

namespace Compose.Net.Unity.Runtime.Adapters.Brushes;

internal record LinearGradientBrushImpl(
    IStableList<Color> Colors,
    IStableList<float>? Stops,
    Offset Start,
    Offset End,
    TileMode TileMode
) : IBrush
{
    public void Apply(IDrawScope drawScope, Rect rect, float alpha, DrawStyle style)
    {
        var painter = drawScope.Painter();
        switch (style)
        {
            case DrawStyle.Fill:
                painter.fillGradient = Create(rect, alpha);
                break;
            case DrawStyle.Stroke strokeStyle:
                painter.lineWidth = strokeStyle.Width;
                painter.strokeFillGradient = Create(rect, alpha);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(style));
        }
    }

    private GradientDefinition Create(Rect contentRect, float alpha)
    {
        var start = Start + contentRect.Offset;
        var end = new Offset(
            X: float.IsPositiveInfinity(End.X) ? contentRect.Size.Width : End.X,
            Y: float.IsPositiveInfinity(End.Y) ? contentRect.Size.Height : End.Y
        ) + contentRect.Offset;
        return new GradientDefinition(
            Colors, Stops, alpha, false,
            start.ToVector2(), end.ToVector2(), 0,
            TileMode switch
            {
                TileMode.Clamp => AddressMode.Clamp,
                TileMode.Repeated => AddressMode.Wrap,
                TileMode.Mirror => AddressMode.Mirror,
                _ => throw new ArgumentOutOfRangeException()
            }
        );
    }
}
