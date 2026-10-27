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
    protected override Action<MeshGenerationContext> GenerateVisualContent { get; } = it =>
    {
        var drawScope = it.DrawScope();
        drawScope.DrawShape(
            color: BackgroundColor,
            shape: Shape
        );
    };

    protected override void OnApply(UnityReusableComposeNode node, VisualElement element,
        IStableList<IModifier> newModifiers)
    {
        element.Clip().Increment();
        var shapeValue = Shape;
        element.style.borderTopLeftRadius = shapeValue.TopLeft.ToLength();
        element.style.borderTopRightRadius = shapeValue.TopRight.ToLength();
        element.style.borderBottomLeftRadius = shapeValue.BottomLeft.ToLength();
        element.style.borderBottomRightRadius = shapeValue.BottomRight.ToLength();
    }

    protected override void OnRevert(UnityReusableComposeNode node, VisualElement element,
        IStableList<IModifier> newModifiers)
    { 
        element.Clip().Decrement();
        var shapeValue = Shape;
        var style = element.style;
        style.borderTopLeftRadius = style.borderTopLeftRadius.CompareAndSetNull(shapeValue.TopLeft.ToLength());
        style.borderTopRightRadius = style.borderTopRightRadius.CompareAndSetNull(shapeValue.TopRight.ToLength());
        style.borderBottomLeftRadius = style.borderBottomLeftRadius.CompareAndSetNull(shapeValue.BottomLeft.ToLength());
        style.borderBottomRightRadius =
            style.borderBottomRightRadius.CompareAndSetNull(shapeValue.BottomRight.ToLength());
    }
}

internal record BackgroundColorModifierImpl(
    Color BackgroundColor
) : UnityModifier, IAppearanceModifier
{
    protected override void Apply(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    )
    {
        element.style.backgroundColor = BackgroundColor.ToUnityColor();
    }

    protected override void Revert(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    )
    {
        var style = element.style;
        style.backgroundColor = style.backgroundColor.CompareAndSetNull(BackgroundColor.ToUnityColor());
    }
}