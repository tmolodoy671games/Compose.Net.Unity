// ReSharper disable CheckNamespace
using Compose.Net;
using Compose.Net.Unity.Runtime.Adapters.Extensions.Values;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record SizeModifierImpl(Dp Width, Dp Height)
    : ModifierNodeElement<SizeModifierImpl.SizeNode>, IAppearanceModifier
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
            _originalWidth = SharedVisual.AuthoredSize(_element, true);
            _originalHeight = SharedVisual.AuthoredSize(_element, false);
            if (!_width.IsUnspecified) SharedVisual.SetAuthoredSize(_element, true, _width.ToLength(UnityDensity.Current));
            if (!_height.IsUnspecified) SharedVisual.SetAuthoredSize(_element, false, _height.ToLength(UnityDensity.Current));
            Box.Refresh(_element);
        }

        internal void Update(Dp nextWidth, Dp nextHeight)
        {
            if (_element != null)
            {
                if (!_width.Equals(nextWidth))
                    SharedVisual.SetAuthoredSize(_element, true, !nextWidth.IsUnspecified ? nextWidth.ToLength(UnityDensity.Current) : _originalWidth);
                if (!_height.Equals(nextHeight))
                    SharedVisual.SetAuthoredSize(_element, false, !nextHeight.IsUnspecified ? nextHeight.ToLength(UnityDensity.Current) : _originalHeight);
            }
            _width = nextWidth;
            _height = nextHeight;
            if (_element != null) Box.Refresh(_element);
        }

        public override void OnDetach()
        {
            if (_element != null)
            {
                SharedVisual.SetAuthoredSize(_element, true, _originalWidth);
                SharedVisual.SetAuthoredSize(_element, false, _originalHeight);
            }
            if (_element != null) Box.Refresh(_element);
            _element = null;
        }
    }
}
