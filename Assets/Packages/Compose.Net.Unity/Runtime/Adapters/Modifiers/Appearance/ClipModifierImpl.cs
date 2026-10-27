// ReSharper disable CheckNamespace

using Compose.Net;
using Compose.Net.Unity.Runtime.Adapters.Extensions.Values;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record ClipModifierImpl(Shape Shape)
    : ModifierNodeElement<ClipModifierImpl.ClipNode>, IAppearanceModifier
{
    public override ClipNode Create() => new(Shape);
    public override void Update(ClipNode node) => node.Update(Shape);
    public override object GetTarget(IReusableComposeNode node) => node.VisualElement();

    internal sealed class ClipNode(Shape shape) : IModifier.Node
    {
        private Shape _shape = shape;
        private StyleLength _originalTopLeft, _originalTopRight, _originalBottomLeft, _originalBottomRight;
        private VisualElement? _element;

        public override void OnAttach()
        {
            _element = (VisualElement)Target;
            _element.Clip().Increment();
            ApplyShape();
        }

        internal void Update(Shape next)
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
            if (_element == null || _shape.IsUnspecified) return;
            var style = _element.style;
            _originalTopLeft = style.borderTopLeftRadius;
            _originalTopRight = style.borderTopRightRadius;
            _originalBottomLeft = style.borderBottomLeftRadius;
            _originalBottomRight = style.borderBottomRightRadius;
            var shapeValue = _shape;
            style.borderTopLeftRadius = shapeValue.TopLeft.ToLength(UnityDensity.Current);
            style.borderTopRightRadius = shapeValue.TopRight.ToLength(UnityDensity.Current);
            style.borderBottomLeftRadius = shapeValue.BottomLeft.ToLength(UnityDensity.Current);
            style.borderBottomRightRadius = shapeValue.BottomRight.ToLength(UnityDensity.Current);
        }

        private void RevertShape()
        {
            if (_element == null || _shape.IsUnspecified) return;
            var style = _element.style;
            style.borderTopLeftRadius = _originalTopLeft;
            style.borderTopRightRadius = _originalTopRight;
            style.borderBottomLeftRadius = _originalBottomLeft;
            style.borderBottomRightRadius = _originalBottomRight;
        }
    }
}
