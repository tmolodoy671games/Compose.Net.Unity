// ReSharper disable CheckNamespace
using Compose.Net;
using SharpExtensions;
using Compose.Net.Unity.Runtime.Adapters.Extensions.Values;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record SizeModifierImpl(Optional<Dp> Width, Optional<Dp> Height)
    : ModifierNodeElement<SizeModifierImpl.SizeNode>, IAppearanceModifier
{
    public override SizeNode Create() => new(Width, Height);
    public override void Update(SizeNode node) => node.Update(Width, Height);
    public override object GetTarget(IReusableComposeNode node) => node.VisualElement();

    internal sealed class SizeNode(Optional<Dp> width, Optional<Dp> height) : IModifier.Node
    {
        private Optional<Dp> _width = width;
        private Optional<Dp> _height = height;
        private StyleLength _originalWidth, _originalHeight;
        private VisualElement? _element;

        public override void OnAttach()
        {
            _element = (VisualElement)Target;
            _originalWidth = _element.style.width;
            _originalHeight = _element.style.height;
            if (_width.HasValue) _element.style.width = _width.Value.ToLength();
            if (_height.HasValue) _element.style.height = _height.Value.ToLength();
        }

        internal void Update(Optional<Dp> nextWidth, Optional<Dp> nextHeight)
        {
            if (_element != null)
            {
                if (!_width.Equals(nextWidth))
                    _element.style.width = nextWidth.HasValue ? nextWidth.Value.ToLength() : _originalWidth;
                if (!_height.Equals(nextHeight))
                    _element.style.height = nextHeight.HasValue ? nextHeight.Value.ToLength() : _originalHeight;
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
