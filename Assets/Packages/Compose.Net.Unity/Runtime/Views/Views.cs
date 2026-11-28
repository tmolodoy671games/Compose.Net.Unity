// ReSharper disable CheckNamespace

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

// A Row or Column measures unweighted children in sequence. A native percentage
// alone would give every child the same fraction of the whole flex container.
public abstract class LinearLayout : VisualElement, ILayout
{
    private readonly Dictionary<VisualElement, Box.ChildStyle> _fillChildren = new();
    private readonly EventCallback<GeometryChangedEvent> _geometry;
    private IVisualElementScheduledItem? _refresh;

    protected LinearLayout()
    {
        _geometry = evt => { if (evt.oldRect.size != evt.newRect.size) ScheduleFill(); };
        RegisterCallback(_geometry);
        RegisterCallback<AttachToPanelEvent>(_ => ScheduleFill());
    }

    internal Box.ChildStyle? FillStyle(VisualElement child) =>
        _fillChildren.TryGetValue(child, out var style) ? style : null;

    void ILayout.Track(VisualElement child)
    {
        child.RegisterCallback(_geometry);
        RefreshFill(child);
    }

    void ILayout.Untrack(VisualElement child)
    {
        child.UnregisterCallback(_geometry);
        if (_fillChildren.Remove(child, out var style)) style.ClearConstraints(child);
        ScheduleFill();
    }

    internal void RefreshFill(VisualElement child)
    {
        var tracked = FillStyle(child);
        var width = tracked?.AuthoredLength(child.style.width, true) ?? child.style.width;
        var height = tracked?.AuthoredLength(child.style.height, false) ?? child.style.height;
        if (Box.IsPercent(width) || Box.IsPercent(height))
            _fillChildren.TryAdd(child, tracked ?? new Box.ChildStyle(child));
        else if (_fillChildren.Remove(child, out tracked)) tracked.ClearConstraints(child);
        ScheduleFill();
    }

    internal void ScheduleFill()
    {
        if (_fillChildren.Count == 0 || panel == null) return;
        if (_refresh == null) _refresh = schedule.Execute(MeasureFill);
        else _refresh.ExecuteLater(0);
    }

    private void MeasureFill()
    {
        var row = style.flexDirection.value is FlexDirection.Row or FlexDirection.RowReverse;
        var available = Box.Maximum(this, row);
        var cross = Box.Maximum(this, !row);
        var gap = row ? resolvedStyle.columnGap : resolvedStyle.rowGap;
        foreach (var child in Children())
        {
            var css = child.resolvedStyle;
            if (css.position == Position.Absolute || css.display == DisplayStyle.None) continue;
            var tracked = FillStyle(child);
            var width = tracked?.AuthoredLength(child.style.width, true) ?? child.style.width;
            var height = tracked?.AuthoredLength(child.style.height, false) ?? child.style.height;
            var mainMargin = row ? css.marginLeft + css.marginRight : css.marginTop + css.marginBottom;
            var crossMargin = row ? css.marginTop + css.marginBottom : css.marginLeft + css.marginRight;
            var mainMax = Mathf.Max(0, available - mainMargin);
            var crossMax = Mathf.Max(0, cross - crossMargin);
            var weighted = child.style.flexGrow.value > 0;
            if (tracked != null)
            {
                tracked.ConstrainAxis(child, row, 0, mainMax, !weighted, onlyFill: true);
                tracked.ConstrainAxis(child, !row, 0, crossMax, true, onlyFill: true);
            }

            // Weighted children receive their allocation after the unweighted pass.
            if (weighted || !float.IsFinite(available)) continue;
            var main = row ? width : height;
            var extent = row ? child.layout.width : child.layout.height;
            if (Box.IsPercent(main)) extent = Box.FillExtent(0, mainMax, main);
            else if (main.keyword == StyleKeyword.Undefined && main.value.unit == LengthUnit.Pixel)
                extent = Box.Limit(child, row, main.value.value, mainMax);
            if (float.IsFinite(extent)) available = Mathf.Max(0, available - extent - mainMargin - gap);
        }
    }
}

public class Column : LinearLayout
{
}

public class Row : LinearLayout
{
}

public interface ILayout
{
    void Track(VisualElement child);
    void Untrack(VisualElement child);
}

public class Spacer : VisualElement
{
}

// Content container of a node (scroll, animateContentSize).
public class Content : LinearLayout
{
}

public class Text : Label
{
    public Text()
    {
        ClearClassList();
        
        style.flexBasis = StyleKeyword.Auto;
        style.flexGrow = 0;
        style.flexShrink = 0;
        style.fontSize = 14;
        
        // Text has no implicit box spacing in Compose. Explicit modifiers and
        // containing components own padding; Label defaults must not shift its center.
        style.marginLeft = style.marginRight = style.marginTop = style.marginBottom = 0;
        style.paddingLeft = style.paddingRight = style.paddingTop = style.paddingBottom = 0;
    }
}