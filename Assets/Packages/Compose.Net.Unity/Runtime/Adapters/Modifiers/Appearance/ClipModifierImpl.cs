// ReSharper disable CheckNamespace

using Compose.Net;
using SharpExtensions;
using Compose.Net.Unity.Runtime.Adapters.Extensions.Values;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record ClipModifierImpl(Optional<Shape> Shape)
    : ModifierNodeElement<ClipModifierImpl.ClipNode>, IAppearanceModifier
{
    public override ClipNode Create() => new(Shape);
    public override void Update(ClipNode node) => node.Update(Shape);
    public override object GetTarget(IReusableComposeNode node) => node.VisualElement();

    internal sealed class ClipNode(Optional<Shape> shape) : IModifier.Node
    {
        private Optional<Shape> _shape = shape;
        private StyleLength _originalTopLeft, _originalTopRight, _originalBottomLeft, _originalBottomRight;
        private VisualElement? _element;

        public override void OnAttach()
        {
            _element = (VisualElement)Target;
            _element.Clip().Increment();
            ApplyShape();
        }

        internal void Update(Optional<Shape> next)
        {
            if (_shape.Equals(next)) return;
            RevertShape();
            _shape = next;
            ApplyShape();
        }

        public override void OnDetach()
        {
            RevertShape();
            _element?.Clip().Decrement();
            _element = null;
        }

        private void ApplyShape()
        {
            if (_element == null || !_shape.HasValue) return;
            var style = _element.style;
            _originalTopLeft = style.borderTopLeftRadius;
            _originalTopRight = style.borderTopRightRadius;
            _originalBottomLeft = style.borderBottomLeftRadius;
            _originalBottomRight = style.borderBottomRightRadius;
            var shapeValue = _shape.Value;
            style.borderTopLeftRadius = shapeValue.TopLeft.ToLength();
            style.borderTopRightRadius = shapeValue.TopRight.ToLength();
            style.borderBottomLeftRadius = shapeValue.BottomLeft.ToLength();
            style.borderBottomRightRadius = shapeValue.BottomRight.ToLength();
        }

        private void RevertShape()
        {
            if (_element == null || !_shape.HasValue) return;
            var style = _element.style;
            style.borderTopLeftRadius = _originalTopLeft;
            style.borderTopRightRadius = _originalTopRight;
            style.borderBottomLeftRadius = _originalBottomLeft;
            style.borderBottomRightRadius = _originalBottomRight;
        }
    }
}
