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
            var style = _element.style;
            _originalMinWidth = style.minWidth;
            _originalMaxWidth = style.maxWidth;
            _originalMinHeight = style.minHeight;
            _originalMaxHeight = style.maxHeight;
            ApplyAll();
        }

        internal void Update(
            Dp nextMinWidth,
            Dp nextMaxWidth,
            Dp nextMinHeight,
            Dp nextMaxHeight
        )
        {
            if (_element == null) return;
            var style = _element.style;
            if (!_minWidth.Equals(nextMinWidth))
                style.minWidth = !nextMinWidth.IsUnspecified ? nextMinWidth.ToLength(UnityDensity.Current) : _originalMinWidth;
            if (!_maxWidth.Equals(nextMaxWidth))
                style.maxWidth = !nextMaxWidth.IsUnspecified ? nextMaxWidth.ToLength(UnityDensity.Current) : _originalMaxWidth;
            if (!_minHeight.Equals(nextMinHeight))
                style.minHeight = !nextMinHeight.IsUnspecified ? nextMinHeight.ToLength(UnityDensity.Current) : _originalMinHeight;
            if (!_maxHeight.Equals(nextMaxHeight))
                style.maxHeight = !nextMaxHeight.IsUnspecified ? nextMaxHeight.ToLength(UnityDensity.Current) : _originalMaxHeight;
            _minWidth = nextMinWidth;
            _maxWidth = nextMaxWidth;
            _minHeight = nextMinHeight;
            _maxHeight = nextMaxHeight;
        }

        public override void OnDetach()
        {
            if (_element != null)
            {
                var style = _element.style;
                style.minWidth = _originalMinWidth;
                style.maxWidth = _originalMaxWidth;
                style.minHeight = _originalMinHeight;
                style.maxHeight = _originalMaxHeight;
            }
            _element = null;
        }

        private void ApplyAll()
        {
            if (_element == null) return;
            var style = _element.style;
            if (!_minWidth.IsUnspecified) style.minWidth = _minWidth.ToLength(UnityDensity.Current);
            if (!_maxWidth.IsUnspecified) style.maxWidth = _maxWidth.ToLength(UnityDensity.Current);
            if (!_minHeight.IsUnspecified) style.minHeight = _minHeight.ToLength(UnityDensity.Current);
            if (!_maxHeight.IsUnspecified) style.maxHeight = _maxHeight.ToLength(UnityDensity.Current);
        }
    }
}
