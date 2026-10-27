// ReSharper disable CheckNamespace
using Compose.Net;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record FillMaxSizeModifierImpl(float WidthFraction, float HeightFraction)
    : ModifierNodeElement<FillMaxSizeModifierImpl.FillNode>, IAppearanceModifier
{
    public override FillNode Create() => new(WidthFraction, HeightFraction);
    public override void Update(FillNode node) => node.Update(WidthFraction, HeightFraction);
    public override object GetTarget(IReusableComposeNode node) => node.VisualElement();

    internal sealed class FillNode : IModifier.Node
    {
        private float _width, _height;
        private StyleLength _originalWidth, _originalHeight;
        private VisualElement? _element;

        internal FillNode(float width, float height)
        {
            _width = width;
            _height = height;
        }

        public override void OnAttach()
        {
            _element = (VisualElement)Target;
            _originalWidth = _element.style.width;
            _originalHeight = _element.style.height;
            if (_width > 0) _element.style.width = new Length(_width * 100, LengthUnit.Percent);
            if (_height > 0) _element.style.height = new Length(_height * 100, LengthUnit.Percent);
        }

        internal void Update(float width, float height)
        {
            if (_element != null)
            {
                if (_width != width)
                    _element.style.width = width > 0
                        ? new Length(width * 100, LengthUnit.Percent)
                        : _originalWidth;
                if (_height != height)
                    _element.style.height = height > 0
                        ? new Length(height * 100, LengthUnit.Percent)
                        : _originalHeight;
            }
            _width = width;
            _height = height;
        }

        public override void OnDetach()
        {
            if (_element != null)
            {
                _element.style.width = _originalWidth;
                _element.style.height = _originalHeight;
            }
            _element = null;
        }
    }
}
