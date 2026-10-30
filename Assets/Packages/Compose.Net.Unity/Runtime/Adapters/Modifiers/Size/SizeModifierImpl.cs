// ReSharper disable CheckNamespace
using Compose.Net;
using Compose.Net.Unity.Runtime.Adapters.Extensions.Values;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record SizeModifierImpl(Dp Width, Dp Height)
    : UnityModifierElement<SizeModifierImpl.SizeNode>, IAppearanceModifier
{
    public override SizeNode Create() => new(Width, Height);
    public override void Update(SizeNode node) => node.Update(Width, Height);
    public override object GetTarget(IReusableComposeNode node) => node.VisualElement();

    internal sealed class SizeNode(Dp width, Dp height) : IModifier.Node
    {
        private Dp _width = width;
        private Dp _height = height;
        private StyleLength _originalWidth, _originalHeight;
        private VisualElement? _element;

        public override void OnAttach()
        {
            _element = (VisualElement)Target;
            _originalWidth = _element.style.width;
            _originalHeight = _element.style.height;
            if (!_width.IsUnspecified) _element.style.width = _width.ToLength(UnityDensity.Current);
            if (!_height.IsUnspecified) _element.style.height = _height.ToLength(UnityDensity.Current);
        }

        internal void Update(Dp nextWidth, Dp nextHeight)
        {
            if (_element != null)
            {
                if (!_width.Equals(nextWidth))
                    _element.style.width = !nextWidth.IsUnspecified ? nextWidth.ToLength(UnityDensity.Current) : _originalWidth;
                if (!_height.Equals(nextHeight))
                    _element.style.height = !nextHeight.IsUnspecified ? nextHeight.ToLength(UnityDensity.Current) : _originalHeight;
            }
            _width = nextWidth;
            _height = nextHeight;
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
