// ReSharper disable CheckNamespace

using Compose.Net;
using Compose.Net.Unity.Runtime.Adapters.Extensions.Values;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record SizeInModifierImpl(
    Dp MinWidth,
    Dp MaxWidth,
    Dp MinHeight,
    Dp MaxHeight
) : ModifierNodeElement<SizeInModifierImpl.SizeInNode>, IAppearanceModifier
{
    public override SizeInNode Create() => new(MinWidth, MaxWidth, MinHeight, MaxHeight);
    public override void Update(SizeInNode node) => node.Update(MinWidth, MaxWidth, MinHeight, MaxHeight);
    public override object GetTarget(IReusableComposeNode node) => node.VisualElement();

    internal sealed class SizeInNode(
        Dp minWidth,
        Dp maxWidth,
        Dp minHeight,
        Dp maxHeight
    ) : IModifier.Node
    {
        private Dp _minWidth = minWidth;
        private Dp _maxWidth = maxWidth;
        private Dp _minHeight = minHeight;
        private Dp _maxHeight = maxHeight;
        private StyleLength _originalMinWidth, _originalMaxWidth, _originalMinHeight, _originalMaxHeight;
        private VisualElement? _element;

        public override void OnAttach()
        {
            _element = (VisualElement)Target;
            _originalMinWidth = Box.AuthoredBound(_element, Box.BoundProperty.MinWidth);
            _originalMaxWidth = Box.AuthoredBound(_element, Box.BoundProperty.MaxWidth);
            _originalMinHeight = Box.AuthoredBound(_element, Box.BoundProperty.MinHeight);
            _originalMaxHeight = Box.AuthoredBound(_element, Box.BoundProperty.MaxHeight);
            ApplyAll();
            Box.Refresh(_element);
        }

        internal void Update(
            Dp nextMinWidth,
            Dp nextMaxWidth,
            Dp nextMinHeight,
            Dp nextMaxHeight
        )
        {
            if (_element == null) return;
            if (!_minWidth.Equals(nextMinWidth))
                Box.SetAuthoredBound(_element, Box.BoundProperty.MinWidth, !nextMinWidth.IsUnspecified ? nextMinWidth.ToLength(UnityDensity.Current) : _originalMinWidth);
            if (!_maxWidth.Equals(nextMaxWidth))
                Box.SetAuthoredBound(_element, Box.BoundProperty.MaxWidth, !nextMaxWidth.IsUnspecified ? nextMaxWidth.ToLength(UnityDensity.Current) : _originalMaxWidth);
            if (!_minHeight.Equals(nextMinHeight))
                Box.SetAuthoredBound(_element, Box.BoundProperty.MinHeight, !nextMinHeight.IsUnspecified ? nextMinHeight.ToLength(UnityDensity.Current) : _originalMinHeight);
            if (!_maxHeight.Equals(nextMaxHeight))
                Box.SetAuthoredBound(_element, Box.BoundProperty.MaxHeight, !nextMaxHeight.IsUnspecified ? nextMaxHeight.ToLength(UnityDensity.Current) : _originalMaxHeight);
            _minWidth = nextMinWidth;
            _maxWidth = nextMaxWidth;
            _minHeight = nextMinHeight;
            _maxHeight = nextMaxHeight;
            Box.Refresh(_element);
        }

        public override void OnDetach()
        {
            if (_element != null)
            {
                Box.SetAuthoredBound(_element, Box.BoundProperty.MinWidth, _originalMinWidth);
                Box.SetAuthoredBound(_element, Box.BoundProperty.MaxWidth, _originalMaxWidth);
                Box.SetAuthoredBound(_element, Box.BoundProperty.MinHeight, _originalMinHeight);
                Box.SetAuthoredBound(_element, Box.BoundProperty.MaxHeight, _originalMaxHeight);
            }
            if (_element != null) Box.Refresh(_element);
            _element = null;
        }

        private void ApplyAll()
        {
            if (_element == null) return;
            if (!_minWidth.IsUnspecified) Box.SetAuthoredBound(_element, Box.BoundProperty.MinWidth, _minWidth.ToLength(UnityDensity.Current));
            if (!_maxWidth.IsUnspecified) Box.SetAuthoredBound(_element, Box.BoundProperty.MaxWidth, _maxWidth.ToLength(UnityDensity.Current));
            if (!_minHeight.IsUnspecified) Box.SetAuthoredBound(_element, Box.BoundProperty.MinHeight, _minHeight.ToLength(UnityDensity.Current));
            if (!_maxHeight.IsUnspecified) Box.SetAuthoredBound(_element, Box.BoundProperty.MaxHeight, _maxHeight.ToLength(UnityDensity.Current));
        }
    }
}
