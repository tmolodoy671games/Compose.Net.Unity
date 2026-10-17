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
    private const string AlignKey = "Compose.Net.Unity_BoxAlign";
    private const string OwnerKey = "Compose.Net.Unity_BoxOwner";
    private const string ShrinkKey = "Compose.Net.Unity_BoxPreviousShrink";
    private const string ShiftKey = "Compose.Net.Unity_BoxShift";

    private const float PullTolerance = 0.5f;
    private const float ShiftTolerance = 0.01f;

    private readonly EventCallback<GeometryChangedEvent> _onGeometryChanged;
    private readonly VisualElement _pull = new BoxPull();
    private Alignment _contentAlignment = Alignment.TopLeft;
    private Box? _contentLayout;
    private float _pullValue;

    public Box()
    {
        style.flexDirection = FlexDirection.Column;
        style.alignItems = Align.FlexStart;
        style.justifyContent = Justify.FlexStart;
        Add(_pull);
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

    public void Track(VisualElement child)
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
        var data = child.UserData();
        if (data.GetOrNull(OwnerKey) != this)
            return;
        data[OwnerKey] = null;
        child.UnregisterCallback(_onGeometryChanged);
        child.style.flexShrink = data.GetOrNull(ShrinkKey) is StyleFloat shrink ? shrink : StyleKeyword.Null;
        data[ShrinkKey] = null;
        child.style.alignSelf = StyleKeyword.Null;
        SetShift(child, 0f);
        Arrange();
    }

    private void Arrange()
    {
        KeepPullLast();

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
        SetPull(0f);
        var alignment = child != null ? AlignmentOf(child) : _contentAlignment;
        SetContainerAlignment(alignment.ToAlign(), alignment.ToJustify());
        if (child == null)
            return;
        if (child.style.alignSelf.keyword != StyleKeyword.Null)
            child.style.alignSelf = StyleKeyword.Null;
        SetShift(child, 0f);
    }

    // Two or more children are stacked on top of each other: the pull collapses the column
    // to the tallest child and every child is shifted to its own vertical position.
    private void ArrangeStack()
    {
        SetContainerAlignment(Align.FlexStart, Justify.FlexStart);

        var sumHeight = 0f;
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
            sumHeight += outerHeight;
            maxHeight = Mathf.Max(maxHeight, outerHeight);
        }

        SetPull(maxHeight - sumHeight);

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

    // Children are inserted by index, so the spacer must stay after every in-flow child.
    // Elements appended after it (draw-on-top layers) are absolute and do not matter.
    private void KeepPullLast()
    {
        if (_pull.parent != this)
        {
            Add(_pull);
            return;
        }

        for (var i = IndexOf(_pull) + 1; i < childCount; i++)
        {
            if (IsStacked(this[i]))
            {
                _pull.BringToFront();
                return;
            }
        }
    }

    private void SetContainerAlignment(Align align, Justify justify)
    {
        if (style.alignItems != align)
            style.alignItems = align;
        if (style.justifyContent != justify)
            style.justifyContent = justify;
    }

    private void SetPull(float pull)
    {
        if (Mathf.Abs(_pullValue - pull) <= PullTolerance)
            return;
        _pullValue = pull;
        _pull.style.marginTop = pull;
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

    private static float OuterHeight(VisualElement child) =>
        child.layout.height + child.resolvedStyle.marginTop + child.resolvedStyle.marginBottom;

    private static float Bias(Justify justify) => justify switch
    {
        Justify.Center => 0.5f,
        Justify.FlexEnd => 1f,
        _ => 0f,
    };
}

internal class BoxPull : VisualElement
{
    public BoxPull()
    {
        pickingMode = UnityEngine.UIElements.PickingMode.Ignore;
        style.flexShrink = 0;
        style.width = 0;
        style.height = 0;
    }
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
    public Text()
    {
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