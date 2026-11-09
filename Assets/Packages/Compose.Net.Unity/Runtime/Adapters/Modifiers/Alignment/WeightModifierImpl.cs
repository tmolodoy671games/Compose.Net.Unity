// ReSharper disable CheckNamespace

using Compose.Net;
using StableCollections;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record WeightModifierImpl(float Weight) : ModifierNodeElement<WeightModifierImpl.WeightNode>
{
    public override WeightNode Create() => new(Weight);
    public override void Update(WeightNode node) => node.Update(Weight);
    public override object GetTarget(IReusableComposeNode node) => node.VisualElement();

    internal sealed class WeightNode(float weight) : IModifier.Node
    {
        private float _weight = weight;
        private StyleFloat _original;
        private StyleLength _originalBasis;
        private VisualElement? _element;
        private Row? _row;
        private WeightedRowWidth? _rowWidth;

        public override void OnAttach()
        {
            _element = (VisualElement)Target;
            _original = _element.style.flexGrow;
            _originalBasis = _element.style.flexBasis;
            _element.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            _element.RegisterCallback<GeometryChangedEvent>(OnGeometry);
            ApplyBasis();
            _element.style.flexGrow = _weight;
        }

        internal void Update(float next)
        {
            _weight = next;
            ApplyBasis();
            if (_element != null)
                _element.style.flexGrow = next;
        }

        private void OnAttachToPanel(AttachToPanelEvent evt) => ApplyBasis();
        private void OnGeometry(GeometryChangedEvent evt) => ApplyBasis();
        private void ApplyBasis()
        {
            if (_element == null) return;
            var parent = _element.parent;
            var row = _weight > 0 ? parent as Row : null;
            if (_row != row)
            {
                _rowWidth?.Release();
                _row = row;
                _rowWidth = row == null ? null : WeightedRowWidth.Acquire(row);
            }
            _rowWidth?.Refresh();
            var horizontal = parent is Row || parent?.style.flexDirection.value is FlexDirection.Row or FlexDirection.RowReverse;
            var size = horizontal ? _element.style.width : _element.style.height;
            // Preserve an authored main-axis size as the wrapping basis. An auto-sized
            // weighted child instead divides remaining space, independent of its content.
            var fixedSize = size.keyword == StyleKeyword.Undefined && size.value.unit == LengthUnit.Pixel;
            _element.style.flexBasis = fixedSize ? _originalBasis : new StyleLength(0f);
        }

        public override void OnDetach()
        {
            if (_element != null)
            {
                _element.UnregisterCallback<AttachToPanelEvent>(OnAttachToPanel);
                _element.UnregisterCallback<GeometryChangedEvent>(OnGeometry);
                _element.style.flexGrow = _original;
                _element.style.flexBasis = _originalBasis;
            }
            _rowWidth?.Release();
            _rowWidth = null;
            _row = null;
            _element = null;
        }
    }

    // Weighted rows fill their bounded width even without an explicit FillMaxWidth.
    // Otherwise Yoga shrink-wraps the row around zero-basis text and collapses it.
    private sealed class WeightedRowWidth(Row row)
    {
        private const string Key = "Compose.Net.Unity.WeightedRowWidth";
        private static StyleLength FillWidth => new Length(100, LengthUnit.Percent);
        private int _users;
        private bool _ownsWidth;
        private StyleLength _originalWidth;

        internal static WeightedRowWidth Acquire(Row row)
        {
            var data = row.UserData();
            if (data.GetOrNull(Key) is not WeightedRowWidth result)
                data[Key] = result = new WeightedRowWidth(row);
            result._users++;
            result.Refresh();
            return result;
        }

        internal void Refresh()
        {
            if (_users == 0) return;
            // Preserve authored sizes and temporary transition constraints.
            var width = row.style.width;
            if (width.keyword != StyleKeyword.Null && width.keyword != StyleKeyword.Auto) return;
            for (var ancestor = row.parent; ancestor != null; ancestor = ancestor.parent)
            {
                if (ancestor is ScrollView scroll && scroll.mode != ScrollViewMode.Vertical) return;
                // A bounded subtree restores finite width even inside a horizontal
                // scroller (for example the preview window's fixed-size viewport).
                if (IsPixelBound(ancestor.style.width) || IsPixelBound(ancestor.style.maxWidth)) break;
            }
            _originalWidth = width;
            _ownsWidth = true;
            row.style.width = FillWidth;
        }

        private static bool IsPixelBound(StyleLength value) =>
            value.keyword == StyleKeyword.Undefined && value.value.unit == LengthUnit.Pixel &&
            float.IsFinite(value.value.value) && value.value.value >= 0;

        internal void Release()
        {
            if (--_users != 0) return;
            if (_ownsWidth && row.style.width == FillWidth) row.style.width = _originalWidth;
            _ownsWidth = false;
        }
    }
}
