// ReSharper disable CheckNamespace

using Compose.Net;
using StableCollections;
using Compose.Net.Unity.Runtime.Adapters.Extensions;
using Compose.Net.Unity.Runtime.Adapters.Extensions.Values;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

public class Column : VisualElement
{
}

public class Row : VisualElement
{
}

public interface ILayout
{
    void Track(VisualElement child);
    void Untrack(VisualElement child);
}

public class Box : VisualElement, ILayout
{
    private bool _propagateMinConstraints;
    private readonly System.Collections.Generic.Dictionary<VisualElement, (StyleLength Width, StyleLength Height)> _minimums = new();
    public bool PropagateMinConstraints
    {
        get => _propagateMinConstraints;
        set { _propagateMinConstraints = value; Arrange(); }
    }

    private void ApplyMinimums()
    {
        foreach (var child in Children())
        {
            if (!IsTracked(child)) continue;
            if (_propagateMinConstraints)
            {
                if (!_minimums.ContainsKey(child)) _minimums.Add(child, (child.style.minWidth, child.style.minHeight));
                var width = Mathf.Max(0, resolvedStyle.minWidth.value - resolvedStyle.paddingLeft - resolvedStyle.paddingRight);
                var height = Mathf.Max(0, resolvedStyle.minHeight.value- resolvedStyle.paddingTop - resolvedStyle.paddingBottom);
                if (!float.IsNaN(width)) child.style.minWidth = Mathf.Max(_minimums[child].Width.value.value, width);
                if (!float.IsNaN(height)) child.style.minHeight = Mathf.Max(_minimums[child].Height.value.value, height);
            }
            else RestoreMinimums(child);
        }
    }

    private void RestoreMinimums(VisualElement child)
    {
        if (!_minimums.TryGetValue(child, out var original)) return;
        child.style.minWidth = original.Width;
        child.style.minHeight = original.Height;
        _minimums.Remove(child);
    }
    private const string AlignKey = "Compose.Net.Unity_BoxAlign";
    private const string OwnerKey = "Compose.Net.Unity_BoxOwner";
    private const string ShrinkKey = "Compose.Net.Unity_BoxPreviousShrink";
    private const string ShiftKey = "Compose.Net.Unity_BoxShift";

    private const string StackMarginKey = "Compose.Net.Unity_BoxStackMargin";
    private const float ShiftTolerance = 0.01f;

    private readonly EventCallback<GeometryChangedEvent> _onGeometryChanged;
    private Alignment _contentAlignment = Alignment.TopLeft;
    private Box? _contentLayout;

    public Box()
    {
        style.flexDirection = FlexDirection.Column;
        style.alignItems = Align.FlexStart;
        style.justifyContent = Justify.FlexStart;
        _onGeometryChanged = evt =>
        {
            if (evt.target == this || !IsAbsolute((VisualElement)evt.target))
                Arrange();
        };
        RegisterCallback(_onGeometryChanged);
    }

    public Alignment ContentAlignment
    {
        set
        {
            _contentAlignment = value;
            if (_contentLayout != null)
                _contentLayout.ContentAlignment = value;
            Arrange();
        }
    }

    // Scroll and animateContentSize move the children into a content container.
    // For a Box that container has to stack them as well, with the same alignment.
    internal Box CreateContentLayout()
    {
        _contentLayout ??= new Box
        {
            pickingMode = UnityEngine.UIElements.PickingMode.Ignore,
        };
        _contentLayout.ContentAlignment = _contentAlignment;
        return _contentLayout;
    }

    public static void SetAlign(VisualElement child, Alignment? alignment)
    {
        child.UserData()[AlignKey] = alignment;
        (child.parent as Box)?.Arrange();
    }

    public virtual void Track(VisualElement child)
    {
        var data = child.UserData();
        var owner = data.GetOrNull(OwnerKey) as ILayout;
        if (owner == this)
            return;
        owner?.Untrack(child);
        data[OwnerKey] = this;
        data[ShrinkKey] = child.style.flexShrink;
        child.style.flexShrink = 0;
        child.RegisterCallback(_onGeometryChanged);
        if (!IsAbsolute(child))
            Arrange();
    }

    public void Untrack(VisualElement child)
    {
        RestoreMinimums(child);
        var data = child.UserData();
        if (data.GetOrNull(OwnerKey) != this)
            return;
        data[OwnerKey] = null;
        child.UnregisterCallback(_onGeometryChanged);
        child.style.flexShrink = data.GetOrNull(ShrinkKey) is StyleFloat shrink ? shrink : StyleKeyword.Null;
        data[ShrinkKey] = null;
        child.style.alignSelf = StyleKeyword.Null;
        SetShift(child, 0f);
        SetStackMargin(child, 0f);
        Arrange();
    }

    private void Arrange()
    {
        ApplyMinimums();

        VisualElement? single = null;
        var stackedCount = 0;
        foreach (var child in Children())
        {
            if (!IsTracked(child))
                continue;
            if (IsAbsolute(child))
            {
                // A child that became absolute (Float, animateContentSize) is out of the stack:
                // drop the shift applied while it was stacked.
                SetShift(child, 0f);
                SetStackMargin(child, 0f);
                continue;
            }

            stackedCount++;
            single = child;
        }

        if (stackedCount <= 1)
        {
            ArrangeSingle(single);
            return;
        }

        ArrangeStack();
    }

    // Nothing to overlap: plain flex alignment of the container is enough.
    private void ArrangeSingle(VisualElement? child)
    {
        if (child != null) SetStackMargin(child, 0f);
        var alignment = child != null ? AlignmentOf(child) : _contentAlignment;
        SetContainerAlignment(alignment.ToAlign(), alignment.ToJustify());
        if (child == null)
            return;
        if (child.style.alignSelf.keyword != StyleKeyword.Null)
            child.style.alignSelf = StyleKeyword.Null;
        SetShift(child, 0f);
    }

    // Keep one tallest child in flow. Compensate the other children with their own
    // margins, so the Box measures the maximum height without a synthetic spacer.
    private void ArrangeStack()
    {
        SetContainerAlignment(Align.FlexStart, Justify.FlexStart);

        VisualElement? tallest = null;
        var maxHeight = 0f;
        foreach (var child in Children())
        {
            if (!IsStacked(child))
                continue;

            var align = AlignmentOf(child).ToAlign();
            if (child.style.alignSelf != align)
                child.style.alignSelf = align;
            if (!HasLayout(child))
                continue;
            var outerHeight = OuterHeight(child);
            if (tallest == null || outerHeight > maxHeight)
            {
                tallest = child;
                maxHeight = outerHeight;
            }
        }

        foreach (var child in Children())
            if (IsStacked(child) && HasLayout(child))
                SetStackMargin(child, child == tallest ? 0f : OuterHeight(child));

        if (float.IsNaN(contentRect.height))
            return;
        // The real height may differ from the content height (fixed size, flex grow/shrink).
        var height = contentRect.height;
        foreach (var child in Children())
        {
            if (!IsStacked(child) || !HasLayout(child))
                continue;
            var bias = Bias(AlignmentOf(child).ToJustify());
            var targetY = contentRect.y + (height - OuterHeight(child)) * bias + child.resolvedStyle.marginTop;
            SetShift(child, targetY - child.layout.y);
        }
    }

    private void SetContainerAlignment(Align align, Justify justify)
    {
        if (style.alignItems != align)
            style.alignItems = align;
        if (style.justifyContent != justify)
            style.justifyContent = justify;
    }

    private sealed class StackMargin(StyleLength original, float natural)
    {
        public readonly StyleLength Original = original;
        public readonly float Natural = natural;
        public float Correction;
    }

    private static void SetStackMargin(VisualElement child, float correction)
    {
        var data = child.UserData();
        var previous = data.GetOrNull(StackMarginKey) as StackMargin;
        if (previous == null)
        {
            if (Mathf.Abs(correction) <= ShiftTolerance) return;
            previous = new StackMargin(child.style.marginBottom, child.resolvedStyle.marginBottom);
            data[StackMarginKey] = previous;
        }
        if (Mathf.Abs(previous.Correction - correction) <= ShiftTolerance) return;
        var natural = child.style.marginBottom.keyword == StyleKeyword.Undefined
            ? child.style.marginBottom.value.value + previous.Correction
            : child.resolvedStyle.marginBottom + previous.Correction;
        if (Mathf.Abs(correction) <= ShiftTolerance)
        {
            child.style.marginBottom = Mathf.Abs(natural - previous.Natural) <= ShiftTolerance
                ? previous.Original : new StyleLength(natural);
            data[StackMarginKey] = null;
        }
        else child.style.marginBottom = natural - correction;
        previous.Correction = correction;
    }
    private static void SetShift(VisualElement child, float shift)
    {
        var data = child.UserData();
        var previous = data.GetOrNull(ShiftKey) is float value ? value : 0f;
        if (Mathf.Abs(previous - shift) <= ShiftTolerance)
            return;
        // Offset modifiers also use translate, so only our own part is replaced.
        child.style.translate = child.style.translate.Add(new Translate(0, shift - previous));
        data[ShiftKey] = shift;
    }

    private Alignment AlignmentOf(VisualElement child) =>
        child.UserData().GetOrNull(AlignKey) as Alignment ?? _contentAlignment;

    private bool IsTracked(VisualElement child) =>
        child.UserData().GetOrNull(OwnerKey) == this;

    private bool IsStacked(VisualElement child) =>
        IsTracked(child) && !IsAbsolute(child);

    private static bool IsAbsolute(VisualElement child) =>
        child.style.position == Position.Absolute || child.resolvedStyle.position == Position.Absolute;

    private static bool HasLayout(VisualElement child) =>
        !float.IsNaN(child.layout.height) && !float.IsNaN(child.layout.y);

    private static float OuterHeight(VisualElement child)
    {
        var margin = child.resolvedStyle.marginBottom;
        if (child.UserData().GetOrNull(StackMarginKey) is StackMargin stack)
            margin = child.style.marginBottom.value.value + stack.Correction;
        return child.layout.height + child.resolvedStyle.marginTop + margin;
    }

    private static float Bias(Justify justify) => justify switch
    {
        Justify.Center => 0.5f,
        Justify.FlexEnd => 1f,
        _ => 0f,
    };
}

public class Spacer : VisualElement
{
}

// Content container of a node (scroll, animateContentSize).
public class Content : VisualElement
{
}

public class Text : Label
{
    private Compose.Net.TextOverflow _overflow;
    private int _maxLines = int.MaxValue, _minLines = 1;
    private System.Action<TextLayoutResult>? _onTextLayout;

    internal void ConfigureLayout(Compose.Net.TextOverflow overflow, int maxLines, int minLines,
        System.Action<TextLayoutResult>? onTextLayout)
    {
        _overflow = overflow;
        _maxLines = maxLines;
        _minLines = minLines;
        _onTextLayout = onTextLayout;
        style.textOverflow = overflow == Compose.Net.TextOverflow.Ellipsis
            ? UnityEngine.UIElements.TextOverflow.Ellipsis : UnityEngine.UIElements.TextOverflow.Clip;
        style.overflow = overflow == Compose.Net.TextOverflow.Visible ? Overflow.Visible : Overflow.Hidden;
        UpdateTextLayout();
    }

    private void UpdateTextLayout()
    {
        var lineHeight = MeasureTextSize("Mg", 0, MeasureMode.Undefined, 0, MeasureMode.Undefined).y;
        if (!(lineHeight > 0) || float.IsNaN(lineHeight)) return;
        style.minHeight = _minLines * lineHeight + resolvedStyle.paddingTop + resolvedStyle.paddingBottom;
        style.maxHeight = _maxLines == int.MaxValue ? new StyleLength(StyleKeyword.None)
            : new StyleLength(_maxLines * lineHeight + resolvedStyle.paddingTop + resolvedStyle.paddingBottom);
        var width = contentRect.width;
        if (!(width > 0)) return;
        var measured = MeasureTextSize(text, width, MeasureMode.AtMost, 0, MeasureMode.Undefined);
        var lines = Mathf.Max(1, Mathf.CeilToInt(measured.y / lineHeight));
        _onTextLayout?.Invoke(new TextLayoutResult(new Size(width, contentRect.height),
            Mathf.Min(lines, _maxLines), measured.x > width + .5f, lines > _maxLines || measured.y > contentRect.height + .5f));
    }

    public Text()
    {
        RegisterCallback<GeometryChangedEvent>(_ => UpdateTextLayout());
        ClearClassList();
        
        style.flexBasis = StyleKeyword.Auto;
        style.flexGrow = 0;
        style.flexShrink = 0;
        style.fontSize = 14;
        
        style.marginBottom = 2;
        style.marginLeft = 2;
        style.marginRight = 4;
        style.marginTop = 4;

        style.paddingBottom = 4;
        style.paddingLeft = 1;
        style.paddingRight = 2;
        style.paddingTop = 4;
    }
}
