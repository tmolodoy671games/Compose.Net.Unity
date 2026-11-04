using System;
using Compose.Net;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record DropShadowModifierImpl
(Shape Shape, Compose.Net.Shadow Shadow) : DrawUnityModifier, IAppearanceModifier
{
    protected override Action<MeshGenerationContext> GenerateVisualContent { get; } =
        new ShapeShadowPainter(Shape, Shadow, false).Draw;
}
