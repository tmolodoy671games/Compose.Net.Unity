// ReSharper disable CheckNamespace

using System;
using Compose.Net;
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

    private readonly EventCallback<GeometryChangedEvent> _geometry = e => RoundedCorners.Apply((VisualElement)e.currentTarget, Shape);
    protected override void OnAttached(VisualElement element)
    {
        element.RegisterCallback(_geometry);
        RoundedCorners.Apply(element, Shape);
    }

    protected override void OnDetached(VisualElement element)
    {
        element.UnregisterCallback(_geometry);
        var radii = RoundedCorners.Resolve(Shape, element.layout.size);
        var style = element.style;
        style.borderTopLeftRadius = style.borderTopLeftRadius.CompareAndSetNull(new Length(radii.x));
        style.borderTopRightRadius = style.borderTopRightRadius.CompareAndSetNull(new Length(radii.y));
        style.borderBottomLeftRadius = style.borderBottomLeftRadius.CompareAndSetNull(new Length(radii.z));
        style.borderBottomRightRadius = style.borderBottomRightRadius.CompareAndSetNull(new Length(radii.w));
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
