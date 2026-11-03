using System;
using Compose.Net;

namespace Compose.Net.Unity.Runtime.Adapters.Brushes;

internal record SolidColorBrushImpl(Color Color) : IBrush
{
    public void Apply(IDrawScope drawScope, Rect rect, float alpha, DrawStyle style)
    {
        var painter = drawScope.Painter();
        switch (style)
        {
            case DrawStyle.Fill:
                painter.fillColor = Color.ToUnityColor();
                break;
            case DrawStyle.Stroke strokeStyle:
                painter.strokeColor = Color.ToUnityColor();
                painter.lineWidth = strokeStyle.Width;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(style));
        }
    }
}