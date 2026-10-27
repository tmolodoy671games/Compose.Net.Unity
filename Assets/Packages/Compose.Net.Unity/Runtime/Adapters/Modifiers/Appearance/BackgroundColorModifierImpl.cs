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

internal record BackgroundColorModifierImpl(Color BackgroundColor)
    : ModifierNodeElement<BackgroundColorModifierImpl.ColorNode>, IAppearanceModifier
{
    public override ColorNode Create() => new(BackgroundColor);
    public override void Update(ColorNode node) => node.Update(BackgroundColor);
    public override object GetTarget(IReusableComposeNode node) => node.VisualElement();

    internal sealed class ColorNode(Color color) : IModifier.Node
    {
        private Color _color = color;
        private StyleColor _original;
        private VisualElement? _element;

        public override void OnAttach()
        {
            _element = (VisualElement)Target;
            _original = _element.style.backgroundColor;
            _element.style.backgroundColor = _color.ToUnityColor();
        }

        internal void Update(Color next)
        {
            if (_color == next) return;
            _color = next;
            if (_element != null) _element.style.backgroundColor = next.ToUnityColor();
        }

        public override void OnDetach()
        {
            if (_element != null)
                _element.style.backgroundColor = _original;
            _element = null;
        }
    }
}
