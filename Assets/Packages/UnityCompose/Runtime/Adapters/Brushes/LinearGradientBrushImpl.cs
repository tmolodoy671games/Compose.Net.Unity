using System;
using System.Linq;
using Compose.Net;
using StableCollections;
using UnityEngine;
using UnityEngine.UIElements;
using Rect = Compose.Net.Rect;

namespace UnityCompose.Packages.UnityCompose.Runtime.Adapters.Brushes;

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
        var context = drawScope.Context();
        switch (style)
        {
            case DrawStyle.Fill:
                context.painter2D.fillGradient = Create(rect, alpha);
                break;
            case DrawStyle.Stroke strokeStyle:
                context.painter2D.lineWidth = strokeStyle.Width;
                context.painter2D.strokeFillGradient = Create(rect, alpha);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(style));
        }
    }

    private FillGradient Create(Rect contentRect, float alpha)
    {
        var start = Start + contentRect.Offset;
        var end = new Offset(
            X: float.IsPositiveInfinity(End.X) ? contentRect.Size.Width : End.X,
            Y: float.IsPositiveInfinity(End.Y) ? contentRect.Size.Height : End.Y
        ) + contentRect.Offset;
        return FillGradient.MakeLinearGradient(
            gradient: new Gradient
            {
                colorKeys = Colors
                    .Select((color, index) => new GradientColorKey(
                        color.ToUnityColor(),
                        Stops != null
                            ? Stops[index]
                            : (float)index / (Colors.Size - 1)
                    ))
                    .ToArray(),

                alphaKeys = Colors
                    .Select((color, index) => new GradientAlphaKey(
                        color.Alpha * alpha,
                        Stops != null
                            ? Stops[index]
                            : (float)index / (Colors.Size - 1)
                    ))
                    .ToArray()
            },
            start: start.ToVector2(),
            end: end.ToVector2(),
            addressMode: TileMode switch
            {
                TileMode.Clamp => AddressMode.Clamp,
                TileMode.Repeated => AddressMode.Wrap,
                TileMode.Mirror => AddressMode.Mirror,
                _ => throw new ArgumentOutOfRangeException()
            }
        );
    }
}