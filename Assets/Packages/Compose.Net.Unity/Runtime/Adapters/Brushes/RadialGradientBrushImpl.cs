using System;
using Compose.Net;
using SharpExtensions;
using StableCollections;
using UnityEngine;
using UnityEngine.UIElements;
using Rect = Compose.Net.Rect;

namespace Compose.Net.Unity.Runtime.Adapters.Brushes;

internal record RadialGradientBrushImpl(
    IStableList<Color> Colors,
    IStableList<float>? Stops,
    Offset Center,
    float Radius,
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
        var containerSize = contentRect.Size.ToVector2();
        var center = ((Center.IsSpecified ? Center : containerSize.ToOffset() / 2) + contentRect.Offset).ToVector2();

        var defaultRadius = Mathf.Min(
            Mathf.Min(center.x.Absolute(), (center.x - containerSize.x).Absolute()),
            Mathf.Min(center.y.Absolute(), (center.y - containerSize.y).Absolute())
        );
        return new GradientDefinition(
            Colors, Stops, alpha, true,
            center, center, (float.IsPositiveInfinity(Radius) ? defaultRadius : Radius) * 2,
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
