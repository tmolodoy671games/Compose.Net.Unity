// ReSharper disable CheckNamespace

using Compose.Net;
using SharpExtensions;
using Compose.Net.Unity.Runtime.Adapters.Extensions.Values;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record SizeInModifierImpl(
    Optional<Dp> MinWidth,
    Optional<Dp> MaxWidth,
    Optional<Dp> MinHeight,
    Optional<Dp> MaxHeight
) : ModifierNodeElement<SizeInModifierImpl.SizeInNode>, IAppearanceModifier
{
    public override SizeInNode Create() => new(MinWidth, MaxWidth, MinHeight, MaxHeight);
    public override void Update(SizeInNode node) => node.Update(MinWidth, MaxWidth, MinHeight, MaxHeight);
    public override object GetTarget(IReusableComposeNode node) => node.VisualElement();

    internal sealed class SizeInNode(
        Optional<Dp> minWidth,
        Optional<Dp> maxWidth,
        Optional<Dp> minHeight,
        Optional<Dp> maxHeight
    ) : IModifier.Node
    {
        private Optional<Dp> _minWidth = minWidth;
        private Optional<Dp> _maxWidth = maxWidth;
        private Optional<Dp> _minHeight = minHeight;
        private Optional<Dp> _maxHeight = maxHeight;
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
            Optional<Dp> nextMinWidth,
            Optional<Dp> nextMaxWidth,
            Optional<Dp> nextMinHeight,
            Optional<Dp> nextMaxHeight
        )
        {
            if (_element == null) return;
            var style = _element.style;
            if (!_minWidth.Equals(nextMinWidth))
                style.minWidth = nextMinWidth.HasValue ? nextMinWidth.Value.ToLength() : _originalMinWidth;
            if (!_maxWidth.Equals(nextMaxWidth))
                style.maxWidth = nextMaxWidth.HasValue ? nextMaxWidth.Value.ToLength() : _originalMaxWidth;
            if (!_minHeight.Equals(nextMinHeight))
                style.minHeight = nextMinHeight.HasValue ? nextMinHeight.Value.ToLength() : _originalMinHeight;
            if (!_maxHeight.Equals(nextMaxHeight))
                style.maxHeight = nextMaxHeight.HasValue ? nextMaxHeight.Value.ToLength() : _originalMaxHeight;
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
            if (_minWidth.HasValue) style.minWidth = _minWidth.Value.ToLength();
            if (_maxWidth.HasValue) style.maxWidth = _maxWidth.Value.ToLength();
            if (_minHeight.HasValue) style.minHeight = _minHeight.Value.ToLength();
            if (_maxHeight.HasValue) style.maxHeight = _maxHeight.Value.ToLength();
        }
    }
}
