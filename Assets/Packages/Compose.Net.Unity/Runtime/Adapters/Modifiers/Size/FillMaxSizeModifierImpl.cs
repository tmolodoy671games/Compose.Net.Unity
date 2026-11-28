// ReSharper disable CheckNamespace
using Compose.Net;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record FillMaxSizeModifierImpl(float WidthFraction, float HeightFraction)
    : UnityModifierNodeElement<FillMaxSizeModifierImpl.FillNode>, IAppearanceModifier
{
    // Compose.Net uses -1 for the omitted axis in FillMaxWidth and FillMaxHeight.
    internal const float Omitted = -1f;

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
            if (_width != Omitted) SharedVisual.SetAuthoredSize(_element, true, Percent(_width));
            if (_height != Omitted) SharedVisual.SetAuthoredSize(_element, false, Percent(_height));
            Box.Refresh(_element);
        }

        private static Length Percent(float fraction) => Length.Percent(Mathf.Clamp01(fraction) * 100);

        internal void Update(float nextWidth, float nextHeight)
        {
            if (_element != null)
            {
                if (_width != nextWidth)
                    SharedVisual.SetAuthoredSize(_element, true, nextWidth != Omitted
                        ? Percent(nextWidth)
                        : _originalWidth);
                if (_height != nextHeight)
                    SharedVisual.SetAuthoredSize(_element, false, nextHeight != Omitted
                        ? Percent(nextHeight)
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
                if (_width != Omitted) SharedVisual.SetAuthoredSize(_element, true, _originalWidth);
                if (_height != Omitted) SharedVisual.SetAuthoredSize(_element, false, _originalHeight);
            }
            if (_element != null) Box.Refresh(_element);
            _element = null;
        }
    }
}
