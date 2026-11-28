using System;
using System.Collections.Generic;
using Compose.Net;
using StableCollections;
using Compose.Net.Unity.Runtime.Adapters.Extensions;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

/// <summary>Native flex layout for one child; one overlapping grid cell for multiple children.</summary>
public partial class Box : VisualElement, ILayout
{
    [Flags] internal enum AnimatedConstraints { Width = 1, Height = 2, MaxWidth = 4, MaxHeight = 8 }
    private const string AnimationKey = "Compose.Box.AnimatedConstraints";
    private const string DataKey = "Compose.Box.ParentData";
    private readonly Dictionary<VisualElement, ChildStyle> _children = new();
    private readonly EventCallback<GeometryChangedEvent> _geometry;
    private IVisualElementScheduledItem? _refresh;
    private Alignment _alignment = Alignment.TopStart;
    private bool _propagate, _refreshing, _grid;
    private Box? _contentLayout;
    private WrapContentModifierImpl? Wrap { get; set; }

    internal void SetWrap(WrapContentModifierImpl? wrap)
    {
        Wrap = wrap;
        style.alignContent = StyleKeyword.Null;
        style.gridTemplateColumns = Track(wrap is { Unbounded: true } && wrap.Wraps(true));
        style.gridTemplateRows = Track(wrap is { Unbounded: true } && wrap.Wraps(false));
        RefreshConstraints();
    }
    private IntrinsicSizeModifierImpl? _intrinsic;

    internal void SetIntrinsic(IntrinsicSizeModifierImpl? intrinsic)
    {
        if (_intrinsic != null)
        {
            SharedVisual.SetAuthoredSize(this, _intrinsic.Axis == Orientation.Horizontal, StyleKeyword.Null);
            _intrinsicCount--;
        }
        _intrinsic = intrinsic;
        if (intrinsic != null) _intrinsicCount++;
        style.gridTemplateColumns = IntrinsicTrack(Orientation.Horizontal);
        style.gridTemplateRows = IntrinsicTrack(Orientation.Vertical);
        RefreshConstraints();
    }

    private List<GridTrackSize> IntrinsicTrack(Orientation axis)
    {
        if (_intrinsic?.Axis != axis) return Track(false);
        var content = _intrinsic.Size == IntrinsicSize.Min ? GridTrackSize.MinContent() : GridTrackSize.MaxContent();
        return new List<GridTrackSize> { GridTrackSize.Minmax(GridTrackSize.Pixels(0), content) };
    }
    // Lazy items are measured without a maximum on their scrolling axis. A native ListView
    // slot's estimated extent is placement data, not an incoming content constraint.
    private bool _unboundedWidth, _unboundedHeight;
    internal bool UnboundedWidth
    {
        get => _unboundedWidth;
        set
        {
            if (_unboundedWidth == value) return;
            _unboundedWidth = value;
            style.gridTemplateColumns = Track(value);
            RefreshConstraints();
        }
    }
    internal bool UnboundedHeight
    {
        get => _unboundedHeight;
        set
        {
            if (_unboundedHeight == value) return;
            _unboundedHeight = value;
            style.gridTemplateRows = Track(value);
            RefreshConstraints();
        }
    }
    private static List<GridTrackSize> Track(bool unbounded) => new()
    {
        unbounded ? GridTrackSize.MaxContent() : GridTrackSize.Minmax(GridTrackSize.Pixels(0), GridTrackSize.Fraction(1))
    };

    public Box()
    {
        style.display = DisplayStyle.Flex;
        style.flexDirection = FlexDirection.Column;
        // A zero track minimum lets incoming max constraints shrink a large child or wrap text.
        style.gridTemplateColumns = Track(false);
        style.gridTemplateRows = Track(false);
        ContentAlignment = Alignment.TopStart;
        // A geometry notification belongs to an active layout pass. Re-entering measurement
        // here makes pixel rounding feed back into Grid's parent/child constraints.
        _geometry = evt =>
        {
            if (evt.oldRect.size != evt.newRect.size) ScheduleRefresh();
        };
        RegisterCallback(_geometry);
        RegisterCallback<AttachToPanelEvent>(_ => ScheduleRefresh());
        RegisterCallback<DetachFromPanelEvent>(_ => _refresh?.Pause());
    }

    private void ScheduleRefresh()
    {
        if (panel == null) return;
        if (_refresh == null) _refresh = schedule.Execute(RefreshConstraints);
        else _refresh.ExecuteLater(0);
    }

    public Alignment ContentAlignment
    {
        get => _alignment;
        set
        {
            _alignment = value;
            RefreshConstraints();
            if (_contentLayout != null) _contentLayout.ContentAlignment = value;
        }
    }

    public bool PropagateMinConstraints
    {
        get => _propagate;
        set
        {
            _propagate = value;
            if (_contentLayout != null) _contentLayout.PropagateMinConstraints = value;
            RefreshConstraints();
        }
    }

    internal Box CreateContentLayout()
    {
        _contentLayout ??= new Box { pickingMode = UnityEngine.UIElements.PickingMode.Ignore };
        _contentLayout.ContentAlignment = _alignment;
        _contentLayout.PropagateMinConstraints = _propagate;
        return _contentLayout;
    }

    public static void SetAlign(VisualElement child, Alignment? alignment) => SetChildData(child, alignment, false);
    internal static void SetChildData(VisualElement child, Alignment? alignment, bool match)
    {
        child.UserData()[DataKey] = new ChildData(alignment, match);
        (child.parent as Box)?.RefreshConstraints();
    }
    private sealed record ChildData(Alignment? Alignment, bool Match);

    public void Track(VisualElement child)
    {
        if (_children.ContainsKey(child)) return;
        _children.Add(child, new ChildStyle(child));
        child.style.gridColumnStart = 1; child.style.gridColumnEnd = 2;
        child.style.gridRowStart = 1; child.style.gridRowEnd = 2;
        child.RegisterCallback(_geometry);
        RefreshConstraints();
    }

    public void Untrack(VisualElement child)
    {
        if (!_children.TryGetValue(child, out var previous)) return;
        _children.Remove(child);
        child.UnregisterCallback(_geometry);
        previous.Restore(child);
        RefreshConstraints();
    }

    // Only the Compose constraint handoff needs a bridge. Native layout performs measurement, overlap,
    // alignment, resize and picking; no per-frame loop, synthetic spacer or transform offset.
    internal enum BoundProperty { MinWidth, MaxWidth, MinHeight, MaxHeight }

    private static ChildStyle? TrackedStyle(VisualElement element) => element.parent switch
    {
        Box box when box._children.TryGetValue(element, out var child) => child,
        LinearLayout linear => linear.FillStyle(element),
        _ => null
    };

    internal static StyleLength AuthoredDimension(VisualElement element, bool horizontal, StyleLength current) =>
        TrackedStyle(element)?.AuthoredLength(current, horizontal) ?? current;

    internal static void AuthorDimension(VisualElement element, bool horizontal, StyleLength value) =>
        TrackedStyle(element)?.AuthorLength(value, horizontal);

    internal static StyleLength AuthoredBound(VisualElement element, BoundProperty property)
    {
        var current = ReadBound(element, property);
        return TrackedStyle(element)?.GetBound(property).Authored(current) ?? current;
    }

    internal static void SetAuthoredBound(VisualElement element, BoundProperty property, StyleLength value)
    {
        TrackedStyle(element)?.GetBound(property).Author(value);
        switch (property)
        {
            case BoundProperty.MinWidth: element.style.minWidth = value; break;
            case BoundProperty.MaxWidth: element.style.maxWidth = value; break;
            case BoundProperty.MinHeight: element.style.minHeight = value; break;
            case BoundProperty.MaxHeight: element.style.maxHeight = value; break;
        }
    }

    private static StyleLength ReadBound(VisualElement element, BoundProperty property) => property switch
    {
        BoundProperty.MinWidth => element.style.minWidth,
        BoundProperty.MaxWidth => element.style.maxWidth,
        BoundProperty.MinHeight => element.style.minHeight,
        _ => element.style.maxHeight
    };

    internal static void Refresh(VisualElement element)
    {
        (element.parent as LinearLayout)?.RefreshFill(element);
        (element as LinearLayout)?.ScheduleFill();
        (element.parent as Box)?.RefreshConstraints();
        (element as Box)?.RefreshConstraints();
        InvalidateIntrinsics(element);
    }

    internal void RefreshConstraints()
    {
        if (_refreshing) return;
        _refreshing = true;
        try
        {
            _grid = _children.Count > 1 || _intrinsic != null || Wrap is { Unbounded: true };
            if (_intrinsic != null && panel != null && childCount == 1) MeasureIntrinsicContent(_intrinsic);
            style.display = _grid ? DisplayStyle.Grid : DisplayStyle.Flex;
            style.justifyItems = _alignment.ToAlign();
            style.alignItems = Wrap?.Wraps(true) == true ? WrapHorizontal(Wrap.Align)
                : _grid ? Vertical(_alignment) : _alignment.ToAlign();
            style.justifyContent = Wrap?.Wraps(false) == true ? WrapJustify(Wrap.Align)
                : _grid ? Justify.FlexStart : _alignment.ToJustify();
            if (_intrinsic?.Axis == Orientation.Horizontal) style.justifyItems = Align.Stretch;
            if (_intrinsic?.Axis == Orientation.Vertical) style.alignItems = Align.Stretch;
            if (Wrap is { Unbounded: true } wrap)
            {
                var horizontal = wrap.Wraps(true) ? WrapHorizontal(wrap.Align) : Align.FlexStart;
                var vertical = wrap.Wraps(false) ? WrapJustify(wrap.Align) : Justify.FlexStart;
                style.justifyItems = horizontal;
                style.justifyContent = horizontal == Align.Center ? Justify.Center : horizontal == Align.FlexEnd ? Justify.FlexEnd : Justify.FlexStart;
                style.alignItems = style.alignContent = vertical == Justify.Center ? Align.Center : vertical == Justify.FlexEnd ? Align.FlexEnd : Align.FlexStart;
            }
            var maxWidth = ChildMaximum(true);
            var maxHeight = ChildMaximum(false);
            var minWidth = _propagate && Wrap?.Wraps(true) != true ? Minimum(this, true, maxWidth) : 0;
            var minHeight = _propagate && Wrap?.Wraps(false) != true ? Minimum(this, false, maxHeight) : 0;
            foreach (var pair in _children)
            {
                var child = pair.Key; var previous = pair.Value;
                if (child.parent != this) continue;
                var value = child.UserData().GetOrNull(DataKey) as ChildData;
                previous.SetAlignment(child, value?.Alignment, _grid);
                if (!_grid && value?.Alignment is { } alignment) style.justifyContent = alignment.ToJustify();
                previous.SetMatch(child, value?.Match == true, resolvedStyle);
                if (value?.Match == true)
                {
                    // Absolute children do not participate in grid track sizing.
                    // Insets are the padding edge, while their constraints use the content box.
                    var width = Mathf.Max(0, Finite(contentRect.width));
                    var height = Mathf.Max(0, Finite(contentRect.height));
                    previous.Constrain(child, width, width, height, height, false, _grid);
                }
                // animateContentSize makes the internal content layout absolute, but it still
                // receives the Box constraints when measuring the next natural size.
                else if (child.style.position != Position.Absolute || child == _contentLayout)
                {
                    // Leading padding on the inner chain is represented as native margins.
                    var marginX = 0f;
                    var marginY = 0f;
                    if (Wrap != null || _intrinsic != null || IsPercent(child.style.width) || IsPercent(child.style.height))
                    {
                        marginX = child.resolvedStyle.marginLeft + child.resolvedStyle.marginRight;
                        marginY = child.resolvedStyle.marginTop + child.resolvedStyle.marginBottom;
                    }
                    previous.Constrain(child, Mathf.Max(0, minWidth - marginX), Mathf.Max(0, maxWidth - marginX),
                        Mathf.Max(0, minHeight - marginY), Mathf.Max(0, maxHeight - marginY), true, _grid && _intrinsic == null);
                }
                else previous.ClearConstraints(child);
                previous.PlaceOverflow(child, this);
                (child as Box)?.RefreshConstraints();
                (child as LinearLayout)?.ScheduleFill();
            }
        }
        finally { _refreshing = false; }
    }

    private static Align Vertical(Alignment value) => value.ToJustify() switch
    {
        Justify.Center => Align.Center,
        Justify.FlexEnd => Align.FlexEnd,
        _ => Align.FlexStart
    };
    private static Justify WrapJustify(Alignment align) => align is Alignment.Vertical
        ? align == Alignment.Bottom ? Justify.FlexEnd : align == Alignment.CenterVertically ? Justify.Center : Justify.FlexStart
        : align.ToJustify();

    private Align WrapHorizontal(Alignment alignment)
    {
        var align = alignment.ToAlign();
        VisualElement direction = this;
        while (direction != null && direction.languageDirection == LanguageDirection.Inherit)
            direction = direction.parent;
        if (direction?.languageDirection != LanguageDirection.RTL || align == Align.Center) return align;
        return align == Align.FlexStart ? Align.FlexEnd : Align.FlexStart;
    }

    private float ChildMaximum(bool horizontal) =>
        Wrap is { Unbounded: true } wrap && wrap.Wraps(horizontal) ? float.PositiveInfinity : Maximum(this, horizontal);
    private static float Finite(float value) => float.IsFinite(value) ? value : 0;
    private static float Insets(VisualElement e, bool horizontal) => horizontal
        ? e.resolvedStyle.paddingLeft + e.resolvedStyle.paddingRight + e.resolvedStyle.borderLeftWidth + e.resolvedStyle.borderRightWidth
        : e.resolvedStyle.paddingTop + e.resolvedStyle.paddingBottom + e.resolvedStyle.borderTopWidth + e.resolvedStyle.borderBottomWidth;
    internal static bool IsPercent(StyleLength size) =>
        size.keyword == StyleKeyword.Undefined && size.value.unit == LengthUnit.Percent;

    internal static float FillExtent(float min, float max, StyleLength percent) =>
        Mathf.Clamp(Mathf.Floor(max * (percent.value.value / 100f) + .5f), min, max);

    private static float LengthValue(StyleLength value, float available, float fallback)
    {
        if (value.keyword != StyleKeyword.Undefined) return fallback;
        return value.value.unit == LengthUnit.Pixel ? value.value.value
            : float.IsFinite(available) ? available * value.value.value / 100 : fallback;
    }
    internal static void SetAnimatedConstraints(VisualElement element, AnimatedConstraints mask, bool enabled)
    {
        var previous = element.UserData().GetOrNull(AnimationKey) is AnimatedConstraints flags ? flags : 0;
        var next = enabled ? previous | mask : previous & ~mask;
        if (previous == next) return;
        element.UserData()[AnimationKey] = next;
        Refresh(element);
    }

    internal static float Maximum(VisualElement e, bool horizontal)
    {
        var parent = e.parent;
        // Native scroll content is intentionally unbounded on its scrolling axis.
        var scroll = parent as ScrollView ?? e.GetFirstAncestorOfType<ScrollView>();
        var unbounded = e is Box box && (horizontal ? box.UnboundedWidth : box.UnboundedHeight) ||
            scroll != null && (parent == scroll || e == scroll.contentContainer) &&
            (horizontal ? scroll.mode != ScrollViewMode.Vertical : scroll.mode != ScrollViewMode.Horizontal);
        // A fixed composition root defines its own logical canvas. Its native host may
        // be smaller (a scaled preview or a clipped viewport), without constraining
        // the composition again. Auto/percentage roots still inherit host constraints.
        var authoredSize = horizontal ? e.style.width : e.style.height;
        var fixedRoot = e.GetReusableComposeNode()?.IsRoot == true &&
            authoredSize.keyword == StyleKeyword.Undefined && authoredSize.value.unit == LengthUnit.Pixel;
        var available = unbounded || fixedRoot || parent == null ? float.PositiveInfinity
            : parent is Box parentBox ? parentBox.ChildMaximum(horizontal) : Maximum(parent, horizontal);
        // Animated shells change rendered bounds, not the constraints for natural target measurement.
        var animated = e.UserData().GetOrNull(AnimationKey) is AnimatedConstraints flags ? flags : 0;
        // A weighted child receives only its share of the main axis. Passing the whole row's
        // maximum through an auto-sized Column makes nested percentage-sized Boxes overflow it.
        var rowAxis = parent?.resolvedStyle.flexDirection is FlexDirection.Row or FlexDirection.RowReverse;
        if (parent != null && horizontal == rowAxis && e.resolvedStyle.flexGrow > 0 && float.IsFinite(available) &&
            (animated & (horizontal ? AnimatedConstraints.Width : AnimatedConstraints.Height)) == 0)
        {
            var allocated = horizontal ? e.layout.width : e.layout.height;
            if (float.IsFinite(allocated) && allocated > 0) available = Mathf.Min(available, allocated);
        }
        var size = (animated & (horizontal ? AnimatedConstraints.Width : AnimatedConstraints.Height)) != 0
            ? float.PositiveInfinity : LengthValue(horizontal ? e.style.width : e.style.height, available, float.PositiveInfinity);
        var limit = (animated & (horizontal ? AnimatedConstraints.MaxWidth : AnimatedConstraints.MaxHeight)) != 0
            ? float.PositiveInfinity : LengthValue(horizontal ? e.style.maxWidth : e.style.maxHeight, available, float.PositiveInfinity);
        var minimum = LengthValue(horizontal ? e.style.minWidth : e.style.minHeight, available, 0);
        size = Mathf.Max(size, minimum);
        return Mathf.Max(0, Mathf.Min(available, Mathf.Min(size, limit)) - Insets(e, horizontal));
    }
    private static float Minimum(VisualElement e, bool horizontal, float maximum)
    {
        var available = e.parent == null ? float.PositiveInfinity : Maximum(e.parent, horizontal);
        var animated = e.UserData().GetOrNull(AnimationKey) is AnimatedConstraints flags ? flags : 0;
        var size = (animated & (horizontal ? AnimatedConstraints.Width : AnimatedConstraints.Height)) != 0
            ? 0 : LengthValue(horizontal ? e.style.width : e.style.height, available, 0);
        var min = LengthValue(horizontal ? e.style.minWidth : e.style.minHeight, available, 0);
        return Mathf.Min(maximum, Mathf.Max(0, Mathf.Max(min, size) - Insets(e, horizontal)));
    }

    internal sealed class ChildStyle
    {
        private readonly StyleGridLine _columnStart, _columnEnd, _rowStart, _rowEnd;
        private readonly StyleEnum<Align> _align, _justify;
        private StyleEnum<Position> _position;
        private StyleLength _left, _right, _top, _bottom;
        private bool _match;
        private StyleLength _originalLeft, _originalTop;
        private bool _offsetX, _offsetY;
        private Bound _minWidth, _maxWidth, _minHeight, _maxHeight, _textWidth, _textHeight;
        internal ChildStyle(VisualElement child)
        {
            var s = child.style;
            _columnStart = s.gridColumnStart; _columnEnd = s.gridColumnEnd;
            _rowStart = s.gridRowStart; _rowEnd = s.gridRowEnd;
            _align = s.alignSelf; _justify = s.justifySelf;
        }
        internal ref Bound GetBound(BoundProperty property)
        {
            if (property == BoundProperty.MinWidth) return ref _minWidth;
            if (property == BoundProperty.MaxWidth) return ref _maxWidth;
            if (property == BoundProperty.MinHeight) return ref _minHeight;
            return ref _maxHeight;
        }
        internal StyleLength AuthoredLength(StyleLength current, bool horizontal) =>
            horizontal ? _textWidth.Authored(current) : _textHeight.Authored(current);
        internal void AuthorLength(StyleLength value, bool horizontal)
        {
            if (horizontal) _textWidth.Author(value);
            else _textHeight.Author(value);
        }
        internal void PlaceOverflow(VisualElement child, Box box)
        {
            var wrap = box.Wrap;
            if (wrap is not { Unbounded: true } && !_offsetX && !_offsetY) return;
            // Grid aligns an overflowing track to its start. Compose applies alignment
            // to negative free space too. Relative insets leave draw/offset transforms intact.
            var dx = 0f;
            var dy = 0f;
            if (wrap is { Unbounded: true })
            {
                var freeX = box.contentRect.width - child.layout.width;
                var freeY = box.contentRect.height - child.layout.height;
                if (wrap.Wraps(true) && freeX < 0)
                {
                    var align = box.WrapHorizontal(wrap.Align);
                    dx = align == Align.Center ? freeX / 2 : align == Align.FlexEnd ? freeX : 0;
                }
                if (wrap.Wraps(false) && freeY < 0)
                {
                    var align = WrapJustify(wrap.Align);
                    dy = align == Justify.Center ? freeY / 2 : align == Justify.FlexEnd ? freeY : 0;
                }
            }
            child.style.left = RelativeInset(child.style.left, dx, ref _originalLeft, ref _offsetX);
            child.style.top = RelativeInset(child.style.top, dy, ref _originalTop, ref _offsetY);
        }

        private static StyleLength RelativeInset(StyleLength current, float offset, ref StyleLength original, ref bool active)
        {
            if (!active) original = current;
            active = offset != 0;
            return active ? new StyleLength(original.value.value + offset) : original;
        }
        internal void SetAlignment(VisualElement child, Alignment? alignment, bool grid)
        {
            child.style.justifySelf = alignment == null ? _justify : alignment.ToAlign();
            child.style.alignSelf = alignment == null ? _align : grid ? Vertical(alignment) : alignment.ToAlign();
        }
        internal void SetMatch(VisualElement child, bool match, IResolvedStyle box)
        {
            var s = child.style;
            if (match && !_match)
            {
                _position = s.position; _left = s.left; _right = s.right; _top = s.top; _bottom = s.bottom;
                s.position = Position.Absolute;
            }
            else if (!match && _match)
            {
                s.position = _position; s.left = _left; s.right = _right; s.top = _top; s.bottom = _bottom;
            }
            _match = match;
            if (!match) return;
            s.left = box.paddingLeft; s.right = box.paddingRight;
            s.top = box.paddingTop; s.bottom = box.paddingBottom;
        }
        internal void Constrain(VisualElement child, float minW, float maxW, float minH, float maxH, bool fill, bool measureText)
        {
            var s = child.style;
            if (!measureText && child is TextElement)
            {
                // Flex measures text itself. Drop any dimensions installed while this
                // Box had multiple children and needed the grid intrinsic-size bridge.
                s.width = _textWidth.Restore(s.width);
                s.height = _textHeight.Restore(s.height);
            }
            if (measureText && child is TextElement text && text.panel != null)
            {
                var authored = _textWidth.Authored(s.width);
                if (authored.keyword is StyleKeyword.Null or StyleKeyword.Auto)
                {
                    // Ask UI Toolkit for both intrinsic dimensions under the incoming width.
                    // Measuring again at its rounded preferred width can wrap a word onto an
                    // extra line at fractional DPI, feeding a different height back into Grid.
                    var insets = Insets(child, true);
                    var available = Mathf.Max(0, maxW - child.resolvedStyle.marginLeft - child.resolvedStyle.marginRight);
                    var bounded = float.IsFinite(available);
                    var measured = text.MeasureTextSize(text.text, bounded ? Mathf.Max(0, available - insets) : 0,
                        bounded ? VisualElement.MeasureMode.AtMost : VisualElement.MeasureMode.Undefined,
                        0, VisualElement.MeasureMode.Undefined);
                    if (float.IsFinite(measured.x) && float.IsFinite(measured.y))
                    {
                        // A measured glyph must fit in the assigned pixel-aligned content width.
                        var pixelsPerPoint = text.panel.scaledPixelsPerPoint;
                        var width = Mathf.Ceil(measured.x * pixelsPerPoint) / pixelsPerPoint + insets;
                        s.width = _textWidth.Apply(s.width, Mathf.Min(width, available), available, true);
                        var authoredHeight = _textHeight.Authored(s.height);
                        if (authoredHeight.keyword is StyleKeyword.Null or StyleKeyword.Auto)
                            s.height = _textHeight.Apply(s.height, Mathf.Min(measured.y + Insets(child, false), maxH), maxH, true);
                    }
                }
            }
            ConstrainAxis(child, true, minW, maxW, fill);
            ConstrainAxis(child, false, minH, maxH, fill);
        }

        internal void ConstrainAxis(VisualElement child, bool horizontal, float min, float max, bool fill, bool onlyFill = false)
        {
            var s = child.style;
            ref var dimension = ref (horizontal ? ref _textWidth : ref _textHeight);
            ref var lower = ref (horizontal ? ref _minWidth : ref _minHeight);
            ref var upper = ref (horizontal ? ref _maxWidth : ref _maxHeight);
            var current = horizontal ? s.width : s.height;
            var authored = dimension.Authored(current);
            var percentage = fill && IsPercent(authored);
            if (percentage)
            {
                // A native percentage can still resolve against a scroll viewport.
                // On an unbounded Compose axis the content must measure naturally.
                var bounded = float.IsFinite(max);
                var size = bounded ? dimension.Restore(current) : dimension.Override(current, StyleKeyword.Auto);
                if (horizontal) s.width = size;
                else s.height = size;
                if (bounded) min = max = FillExtent(min, max, authored);
            }
            var constrain = percentage || !onlyFill;
            var low = horizontal ? s.minWidth : s.minHeight;
            var high = horizontal ? s.maxWidth : s.maxHeight;
            low = constrain ? lower.Apply(low, min, max, true) : lower.Restore(low);
            high = constrain ? upper.Apply(high, min, max, false) : upper.Restore(high);
            if (horizontal) { s.minWidth = low; s.maxWidth = high; }
            else { s.minHeight = low; s.maxHeight = high; }
        }

        internal void ClearConstraints(VisualElement child)
        {
            var s = child.style;
            s.width = _textWidth.Restore(s.width);
            s.height = _textHeight.Restore(s.height);
            s.minWidth = _minWidth.Restore(s.minWidth); s.maxWidth = _maxWidth.Restore(s.maxWidth);
            s.minHeight = _minHeight.Restore(s.minHeight); s.maxHeight = _maxHeight.Restore(s.maxHeight);
        }
        internal void Restore(VisualElement child)
        {
            if (_offsetX) child.style.left = _originalLeft;
            if (_offsetY) child.style.top = _originalTop;
            SetMatch(child, false, child.resolvedStyle); ClearConstraints(child);
            var s = child.style;
            s.gridColumnStart = _columnStart; s.gridColumnEnd = _columnEnd;
            s.gridRowStart = _rowStart; s.gridRowEnd = _rowEnd;
            s.alignSelf = _align; s.justifySelf = _justify;
        }
    }
    // Keep authored bounds separate from temporary parent constraints, and restore them on removal.
    internal struct Bound
    {
        private StyleLength _original, _applied;
        private bool _active;
        // UI Toolkit can return Auto with a different underlying Length after a style write.
        // Keywords carry the whole value; compare the number and unit only for numeric lengths.
        private bool IsApplied(StyleLength current) => _active && (_applied.keyword == StyleKeyword.Undefined
            ? current == _applied : current.keyword == _applied.keyword);
        internal StyleLength Authored(StyleLength current) => IsApplied(current) ? _original : current;
        internal void Author(StyleLength value) { _original = value; _active = false; }
        internal StyleLength Apply(StyleLength current, float min, float max, bool minimum)
        {
            if (!IsApplied(current)) _original = current;
            _active = true;
            var own = LengthValue(_original, max, minimum ? 0 : float.PositiveInfinity);
            var constrained = Mathf.Clamp(own, min, max);
            _applied = float.IsFinite(constrained) ? new StyleLength(constrained) : _original;
            return _applied;
        }
        internal StyleLength Override(StyleLength current, StyleLength value)
        {
            _original = Authored(current);
            _active = true;
            return _applied = value;
        }
        internal StyleLength Restore(StyleLength current)
        {
            var restored = Authored(current);
            _active = false; return restored;
        }
    }
}
