using System;
using Compose.Net;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record InnerShadowModifierImpl
(Shape Shape, Compose.Net.Shadow Shadow) : DrawUnityModifier, IAppearanceModifier
{
    protected override Action<MeshGenerationContext> GenerateVisualContent { get; } =
        new ShapeShadowPainter(Shape, Shadow, true).Draw;
}
