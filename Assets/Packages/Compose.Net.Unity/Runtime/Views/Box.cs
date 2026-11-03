using System;
using System.Collections.Generic;
using Compose.Net;
using StableCollections;
using Compose.Net.Unity.Runtime.Adapters.Extensions;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

/// <summary>A single native grid cell: all children overlap and contribute their intrinsic size.</summary>
public class Box : VisualElement, ILayout
{
    [Flags] internal enum AnimatedConstraints { Width = 1, Height = 2, MaxWidth = 4, MaxHeight = 8 }
    private const string AnimationKey = "Compose.Box.AnimatedConstraints";
    private const string DataKey = "Compose.Box.ParentData";
    private readonly Dictionary<VisualElement, ChildStyle> _children = new();
    private readonly EventCallback<GeometryChangedEvent> _geometry;
    private IVisualElementScheduledItem? _refresh;
    private Alignment _alignment = Alignment.TopStart;
    private bool _propagate, _refreshing;
    private Box? _contentLayout;
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
        style.display = DisplayStyle.Grid;
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
            style.justifyItems = value.ToAlign();
            style.alignItems = Vertical(value);
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
    }

    // Only the Compose constraint handoff needs a bridge. Grid performs measurement, overlap,
    // alignment, resize and picking; no per-frame loop, synthetic spacer or transform offset.
    internal enum BoundProperty { MinWidth, MaxWidth, MinHeight, MaxHeight }

    internal static StyleLength AuthoredBound(VisualElement element, BoundProperty property)
    {
        var current = ReadBound(element, property);
        return element.parent is Box box && box._children.TryGetValue(element, out var child)
            ? child.GetBound(property).Authored(current) : current;
    }

    internal static void SetAuthoredBound(VisualElement element, BoundProperty property, StyleLength value)
    {
        if (element.parent is Box box && box._children.TryGetValue(element, out var child))
            child.GetBound(property).Author(value);
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
        (element.parent as Box)?.RefreshConstraints();
        (element as Box)?.RefreshConstraints();
    }

    internal void RefreshConstraints()
    {
        if (_refreshing) return;
        _refreshing = true;
        try
        {
            var maxWidth = Maximum(this, true);
            var maxHeight = Maximum(this, false);
            var minWidth = _propagate ? Minimum(this, true, maxWidth) : 0;
            var minHeight = _propagate ? Minimum(this, false, maxHeight) : 0;
            foreach (var pair in _children)
            {
                var child = pair.Key; var previous = pair.Value;
                if (child.parent != this) continue;
                var value = child.UserData().GetOrNull(DataKey) as ChildData;
                previous.SetAlignment(child, value?.Alignment);
                previous.SetMatch(child, value?.Match == true, resolvedStyle);
                if (value?.Match == true)
                {
                    // Absolute children do not participate in grid track sizing.
                    // Insets are the padding edge, while their constraints use the content box.
                    var width = Mathf.Max(0, Finite(contentRect.width));
                    var height = Mathf.Max(0, Finite(contentRect.height));
                    previous.Constrain(child, width, width, height, height, false);
                }
                // animateContentSize makes the internal content layout absolute, but it still
                // receives the Box constraints when measuring the next natural size.
                else if (child.style.position != Position.Absolute || child == _contentLayout)
                    previous.Constrain(child, minWidth, maxWidth, minHeight, maxHeight, true);
                else previous.ClearConstraints(child);
                (child as Box)?.RefreshConstraints();
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
    private static float Finite(float value) => float.IsFinite(value) ? value : 0;
    private static float Insets(VisualElement e, bool horizontal) => horizontal
        ? e.resolvedStyle.paddingLeft + e.resolvedStyle.paddingRight + e.resolvedStyle.borderLeftWidth + e.resolvedStyle.borderRightWidth
        : e.resolvedStyle.paddingTop + e.resolvedStyle.paddingBottom + e.resolvedStyle.borderTopWidth + e.resolvedStyle.borderBottomWidth;
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

    private static float Maximum(VisualElement e, bool horizontal)
    {
        var parent = e.parent;
        // Native scroll content is intentionally unbounded on its scrolling axis.
        var scroll = parent as ScrollView ?? e.GetFirstAncestorOfType<ScrollView>();
        var unbounded = e is Box box && (horizontal ? box.UnboundedWidth : box.UnboundedHeight) ||
            scroll != null && (parent == scroll || e == scroll.contentContainer) &&
            (horizontal ? scroll.mode != ScrollViewMode.Vertical : scroll.mode != ScrollViewMode.Horizontal);
        var available = unbounded || parent == null ? float.PositiveInfinity : Maximum(parent, horizontal);
        // Animated shells change rendered bounds, not the constraints for natural target measurement.
        var animated = e.UserData().GetOrNull(AnimationKey) is AnimatedConstraints flags ? flags : 0;
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

    private sealed class ChildStyle
    {
        private readonly StyleGridLine _columnStart, _columnEnd, _rowStart, _rowEnd;
        private readonly StyleEnum<Align> _align, _justify;
        private StyleEnum<Position> _position;
        private StyleLength _left, _right, _top, _bottom;
        private bool _match;
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
        internal void SetAlignment(VisualElement child, Alignment? alignment)
        {
            child.style.justifySelf = alignment == null ? _justify : alignment.ToAlign();
            child.style.alignSelf = alignment == null ? _align : Vertical(alignment);
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
        internal void Constrain(VisualElement child, float minW, float maxW, float minH, float maxH, bool fill)
        {
            var s = child.style;
            if (child is TextElement text && text.panel != null)
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
            if (fill)
            {
                if (s.width.keyword == StyleKeyword.Undefined && s.width.value.unit == LengthUnit.Percent && float.IsFinite(maxW))
                    minW = maxW = Mathf.Max(minW, maxW * s.width.value.value / 100);
                if (s.height.keyword == StyleKeyword.Undefined && s.height.value.unit == LengthUnit.Percent && float.IsFinite(maxH))
                    minH = maxH = Mathf.Max(minH, maxH * s.height.value.value / 100);
            }
            s.minWidth = _minWidth.Apply(s.minWidth, minW, maxW, true);
            s.maxWidth = _maxWidth.Apply(s.maxWidth, minW, maxW, false);
            s.minHeight = _minHeight.Apply(s.minHeight, minH, maxH, true);
            s.maxHeight = _maxHeight.Apply(s.maxHeight, minH, maxH, false);
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
            SetMatch(child, false, child.resolvedStyle); ClearConstraints(child);
            var s = child.style;
            s.gridColumnStart = _columnStart; s.gridColumnEnd = _columnEnd;
            s.gridRowStart = _rowStart; s.gridRowEnd = _rowEnd;
            s.alignSelf = _align; s.justifySelf = _justify;
        }
    }
    // Keep authored bounds separate from temporary parent constraints, and restore them on removal.
    private struct Bound
    {
        private StyleLength _original, _applied;
        private bool _active;
        internal StyleLength Authored(StyleLength current) => _active && current == _applied ? _original : current;
        internal void Author(StyleLength value) { _original = value; _active = false; }
        internal StyleLength Apply(StyleLength current, float min, float max, bool minimum)
        {
            if (!_active || current != _applied) _original = current;
            _active = true;
            var own = LengthValue(_original, max, minimum ? 0 : float.PositiveInfinity);
            var constrained = Mathf.Clamp(own, min, max);
            _applied = float.IsFinite(constrained) ? new StyleLength(constrained) : _original;
            return _applied;
        }
        internal StyleLength Restore(StyleLength current)
        {
            var restored = _active && current == _applied ? _original : current;
            _active = false; return restored;
        }
    }
}
