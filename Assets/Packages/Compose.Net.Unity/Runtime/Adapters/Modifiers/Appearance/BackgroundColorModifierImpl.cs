// ReSharper disable CheckNamespace

using System;
using Compose.Net;
using StableCollections;
using Compose.Net.Unity.Runtime.Adapters.Extensions.Values;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record BackgroundColorShapeModifierImpl(
    Color BackgroundColor,
    Shape Shape
) : DrawUnityModifier
{
    public override void UpdateStyle(UnityStyleModifierNode node)
    {
        node.Set<StyleEnum<Overflow>>(node.Styles.Overflow, Overflow.Hidden);
        node.Set<StyleLength>(node.Styles.BorderTopLeftRadius, Shape.TopLeft.ToLength());
        node.Set<StyleLength>(node.Styles.BorderTopRightRadius, Shape.TopRight.ToLength());
        node.Set<StyleLength>(node.Styles.BorderBottomLeftRadius, Shape.BottomLeft.ToLength());
        node.Set<StyleLength>(node.Styles.BorderBottomRightRadius, Shape.BottomRight.ToLength());
    }

    protected override Action<MeshGenerationContext> GenerateVisualContent { get; } = it =>
    {
        var drawScope = it.DrawScope();
        drawScope.DrawShape(
            color: BackgroundColor,
            shape: Shape
        );
    };




}

internal record BackgroundColorModifierImpl(
    Color BackgroundColor
) : UnityStyleModifier, IAppearanceModifier
{
    public override void UpdateStyle(UnityStyleModifierNode node)
        => node.Set<StyleColor>(node.Styles.BackgroundColor, BackgroundColor.ToUnityColor());




}
