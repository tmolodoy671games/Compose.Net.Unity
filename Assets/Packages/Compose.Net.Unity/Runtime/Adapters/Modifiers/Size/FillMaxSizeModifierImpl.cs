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

    internal sealed class FillNode(float width, float height) : IModifier.Node
    {
        private float _width = width;
        private float _height = height;
        private StyleLength _originalWidth, _originalHeight;
        private VisualElement? _element;

        public override void OnAttach()
        {
            _element = (VisualElement)Target;
            _originalWidth = SharedVisual.AuthoredSize(_element, true);
            _originalHeight = SharedVisual.AuthoredSize(_element, false);
            if (_width > 0) SharedVisual.SetAuthoredSize(_element, true, new Length(_width * 100, LengthUnit.Percent));
            if (_height > 0) SharedVisual.SetAuthoredSize(_element, false, new Length(_height * 100, LengthUnit.Percent));
            Box.Refresh(_element);
        }

        internal void Update(float nextWidth, float nextHeight)
        {
            if (_element != null)
            {
                if (_width != nextWidth)
                    SharedVisual.SetAuthoredSize(_element, true, nextWidth > 0
                        ? new Length(nextWidth * 100, LengthUnit.Percent)
                        : _originalWidth);
                if (_height != nextHeight)
                    SharedVisual.SetAuthoredSize(_element, false, nextHeight > 0
                        ? new Length(nextHeight * 100, LengthUnit.Percent)
                        : _originalHeight);
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
