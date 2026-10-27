// ReSharper disable CheckNamespace

using System;
using Compose.Net;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record DrawOnTopModifierImpl(
    Action<IDrawScope> OnDraw
) : DrawOnTopUnityModifier
{
    protected override Action<MeshGenerationContext> GenerateVisualContent { get; } = it =>
    {
        var scope = it.DrawScope();
        OnDraw(scope);
    };
}

public static partial class DrawScopeExtensions
{
    public static void DrawImageScreen(
        this IDrawScope scope,
        ILayoutCoordinates coordinates,
        IImageBitmap image,
        float alpha = 1f
    )
    {
        var topLeft = coordinates.RootToLocal(Offset.Zero);

        var bottomRight = coordinates.RootToLocal(
            new Offset(Screen.width, Screen.height)
        );

        var size = new Size(
            bottomRight.X - topLeft.X,
            bottomRight.Y - topLeft.Y
        );

        scope.DrawImage(
            image,
            topLeft: topLeft,
            alpha: alpha,
            size: size
        );
    }
}