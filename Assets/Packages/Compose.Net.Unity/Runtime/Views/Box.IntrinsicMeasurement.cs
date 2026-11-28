using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

public partial class Box
{
    private static int _intrinsicCount;

    private void MeasureIntrinsicContent(IntrinsicSizeModifierImpl modifier)
    {
        var horizontal = modifier.Axis == Orientation.Horizontal;
        var lower = horizontal ? style.minWidth : style.minHeight;
        var upper = horizontal ? style.maxWidth : style.maxHeight;
        StyleLength size;
        if (lower.keyword == StyleKeyword.Undefined && lower.value.unit == LengthUnit.Pixel && lower == upper)
            size = lower;
        else
        {
            // The query uses the incoming cross-axis maximum, even when required sizing
            // subsequently measures the actual child with its cross axis unconstrained.
            var incoming = parent is Box outer && outer.Wrap?.RequiredAxis == modifier.Axis ? outer : this;
            var measured = WithMargins(this[0], horizontal, modifier.Size, Maximum(incoming, !horizontal));
            size = measured.HasValue ? new StyleLength(measured.Value) : StyleKeyword.Null;
        }
        if (size != SharedVisual.AuthoredSize(this, horizontal))
            SharedVisual.SetAuthoredSize(this, horizontal, size);
    }

    // Content may change while its old constrained rectangle stays the same.
    internal static void InvalidateIntrinsics(VisualElement element)
    {
        if (_intrinsicCount == 0) return;
        for (var ancestor = element.parent; ancestor != null; ancestor = ancestor.parent)
            if (ancestor is Box { _intrinsic: not null } box)
                box.ScheduleRefresh();
    }

    internal static float Limit(VisualElement view, bool horizontal, float value, float available = float.PositiveInfinity)
    {
        var min = AuthoredBound(view, horizontal ? BoundProperty.MinWidth : BoundProperty.MinHeight);
        var max = AuthoredBound(view, horizontal ? BoundProperty.MaxWidth : BoundProperty.MaxHeight);
        var lower = LengthValue(min, available, 0);
        var upper = LengthValue(max, available, float.PositiveInfinity);
        return Mathf.Clamp(value, lower, Mathf.Max(lower, upper));
    }

    private static float? IntrinsicExtent(VisualElement view, bool horizontal, IntrinsicSize mode, float cross)
    {
        var box = view as Box;
        var axis = horizontal ? Orientation.Horizontal : Orientation.Vertical;
        var own = SharedVisual.AuthoredSize(view, horizontal);
        if (box?._intrinsic?.Axis == axis)
            mode = box._intrinsic.Size;
        else if (own.keyword == StyleKeyword.Undefined && own.value.unit == LengthUnit.Pixel)
            return Limit(view, horizontal, own.value.value);

        // FillMax and percentage bounds have no intrinsic size before allocation.
        if (box?.Wrap is { Unbounded: true, RequiredAxis: null } wrap && wrap.Wraps(!horizontal)) cross = float.PositiveInfinity;
        var other = SharedVisual.AuthoredSize(view, !horizontal);
        if (box?._intrinsic == null && other.keyword == StyleKeyword.Undefined && other.value.unit == LengthUnit.Pixel)
            cross = other.value.value;
        else if (IsPercent(other) && float.IsFinite(cross))
            cross = FillExtent(0, cross, other);
        cross = Mathf.Max(0, Limit(view, !horizontal, cross) - Insets(view, !horizontal));
        var extent = ContentExtent(view, horizontal, mode, cross);
        return extent.HasValue ? Limit(view, horizontal, extent.Value + Insets(view, horizontal)) : null;
    }

    private static float? ContentExtent(VisualElement view, bool horizontal, IntrinsicSize mode, float cross)
    {
        if (view is TextElement text)
        {
            if (horizontal) return TextWidth(text, mode);
            var bounded = float.IsFinite(cross) && text.resolvedStyle.whiteSpace != WhiteSpace.NoWrap;
            return text.MeasureTextSize(text.text, bounded ? cross : 0, bounded ? MeasureMode.AtMost : MeasureMode.Undefined,
                0, MeasureMode.Undefined).y;
        }
        if (view is Image image && image.sourceRect.width > 0 && image.sourceRect.height > 0)
        {
            var rect = image.sourceRect;
            var own = horizontal ? rect.width : rect.height;
            var other = horizontal ? rect.height : rect.width;
            return own * Mathf.Min(1, cross / other);
        }
        if (view is Box)
        {
            var largest = 0f;
            for (var i = 0; i < view.hierarchy.childCount; i++)
            {
                var child = view.hierarchy[i];
                if (!Participates(child)) continue;
                var extent = WithMargins(child, horizontal, mode, cross);
                if (!extent.HasValue) return null;
                largest = Mathf.Max(largest, extent.Value);
            }
            return largest;
        }
        if (view is Spacer) return 0;
        // Defer unknown native controls to their own min/max-content grid measurement.
        if (view is not (Row or Column or Content)) return null;
        var row = view.style.flexDirection.value is FlexDirection.Row or FlexDirection.RowReverse;
        var gap = row ? view.resolvedStyle.columnGap : view.resolvedStyle.rowGap;
        return row == horizontal ? LinearMainExtent(view, horizontal, mode, cross, gap)
            : LinearCrossExtent(view, horizontal, mode, cross, gap);
    }

    private static bool Participates(VisualElement view) =>
        view.style.position != Position.Absolute && view.resolvedStyle.display != DisplayStyle.None;

    private static float? WithMargins(VisualElement view, bool horizontal, IntrinsicSize mode, float cross)
    {
        var style = view.resolvedStyle;
        var mainMargin = horizontal ? style.marginLeft + style.marginRight : style.marginTop + style.marginBottom;
        var crossMargin = horizontal ? style.marginTop + style.marginBottom : style.marginLeft + style.marginRight;
        return IntrinsicExtent(view, horizontal, mode, Mathf.Max(0, cross - crossMargin)) + mainMargin;
    }

    private static float? LinearMainExtent(VisualElement view, bool horizontal, IntrinsicSize mode, float cross, float gap)
    {
        var fixedSpace = 0f;
        var weights = 0f;
        var unit = 0f;
        var count = 0;
        foreach (var child in view.Children())
        {
            if (!Participates(child)) continue;
            var extent = WithMargins(child, horizontal, mode, cross);
            if (!extent.HasValue) return null;
            var weight = child.style.flexGrow.value;
            if (weight <= 0) fixedSpace += extent.Value;
            else
            {
                weights += weight;
                unit = Mathf.Max(unit, extent.Value / weight);
            }
            count++;
        }
        return fixedSpace + unit * weights + Mathf.Max(0, count - 1) * gap;
    }

    private static float? LinearCrossExtent(VisualElement view, bool horizontal, IntrinsicSize mode, float available, float gap)
    {
        var count = 0;
        var weights = 0f;
        foreach (var child in view.Children())
        {
            if (!Participates(child)) continue;
            count++;
            weights += Mathf.Max(0, child.style.flexGrow.value);
        }
        var remaining = Mathf.Max(0, available - Mathf.Max(0, count - 1) * gap);
        var largest = 0f;
        foreach (var child in view.Children())
        {
            if (!Participates(child) || child.style.flexGrow.value > 0) continue;
            var preferred = WithMargins(child, !horizontal, IntrinsicSize.Max, float.PositiveInfinity);
            if (!preferred.HasValue) return null;
            var allocated = Mathf.Min(remaining, preferred.Value);
            remaining -= allocated;
            var extent = WithMargins(child, horizontal, mode, allocated);
            if (!extent.HasValue) return null;
            largest = Mathf.Max(largest, extent.Value);
        }
        if (weights <= 0) return largest;
        foreach (var child in view.Children())
        {
            var weight = child.style.flexGrow.value;
            if (weight <= 0 || !Participates(child)) continue;
            var extent = WithMargins(child, horizontal, mode, remaining * (weight / weights));
            if (!extent.HasValue) return null;
            largest = Mathf.Max(largest, extent.Value);
        }
        return largest;
    }

    private static float TextWidth(TextElement text, IntrinsicSize mode)
    {
        var value = text.text;
        if (string.IsNullOrEmpty(value)) return 0;
        float Measure(string part) => text.MeasureTextSize(part, 0, MeasureMode.Undefined, 0, MeasureMode.Undefined).x;
        if (mode == IntrinsicSize.Max || text.resolvedStyle.whiteSpace == WhiteSpace.NoWrap)
            return Measure(value);

        // Native min-content can emergency-wrap a word down to one glyph. Compose asks for
        // the width of the widest run between normal line-break opportunities.
        var widest = 0f;
        var start = 0;
        var elements = StringInfo.GetTextElementEnumerator(value);
        while (elements.MoveNext())
        {
            var at = elements.ElementIndex;
            var c = value[at];
            var space = (char.IsWhiteSpace(c) && c != '\u00a0' && c != '\u202f') || c == '\u200b';
            var ideograph = c is >= '\u2e80' and <= '\u9fff' or >= '\uf900' and <= '\ufaff';
            if (!space && !ideograph && c != '-' && c != '\u2010') continue;
            var end = space || ideograph ? at : at + 1;
            if (end > start) widest = Mathf.Max(widest, Measure(value.Substring(start, end - start)));
            if (ideograph) widest = Mathf.Max(widest, Measure(elements.GetTextElement()));
            start = at + elements.GetTextElement().Length;
        }
        if (start < value.Length) widest = Mathf.Max(widest, Measure(value.Substring(start)));
        return widest;
    }
}

