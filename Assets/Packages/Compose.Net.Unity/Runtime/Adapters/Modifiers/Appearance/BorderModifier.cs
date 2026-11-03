using System;
using Compose.Net;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record BorderModifierImpl(Dp BorderWidth, IBrush Brush, Shape Shape) : DrawUnityModifier
{
    protected override Action<MeshGenerationContext> GenerateVisualContent { get; } = context =>
    {
        var scope = context.DrawScope();
        var density = UnityDensity.Current;
        var width = Math.Min(BorderWidth.ToPx(density), Math.Min(scope.Size.Width, scope.Size.Height));
        if (width <= 0) return;
        var half = width / 2;
        var radii = RoundedCorners.Resolve(Shape, new UnityEngine.Vector2(scope.Size.Width, scope.Size.Height));
        var pxPerDp = 1.Dp().ToPx(density);
        var inset = new Shape((Math.Max(0, radii.x - half) / pxPerDp).Dp(),
            (Math.Max(0, radii.y - half) / pxPerDp).Dp(),
            (Math.Max(0, radii.z - half) / pxPerDp).Dp(),
            (Math.Max(0, radii.w - half) / pxPerDp).Dp());
        scope.DrawShape(Brush, new Offset(half, half), scope.Size - new Size(width), inset, style: Stroke(width));
    };
}
