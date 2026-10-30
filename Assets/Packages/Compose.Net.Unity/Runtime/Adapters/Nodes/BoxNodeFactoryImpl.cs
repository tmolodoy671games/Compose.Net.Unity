using Compose.Net;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Runtime.Adapters.Nodes;

internal class BoxNodeFactoryImpl : IBoxNodeFactory
{
    public IReusableComposeNode CreateNode(string? type = null)
    {
        return new UnityReusableComposeNode(CreateBox(type));
    }

    public void Apply(IReusableComposeNode node, Alignment alignment, bool propagateMinConstraints = false)
    {
        var box = node.VisualElement<Box>();
        box.ContentAlignment = alignment;
        box.PropagateMinConstraints = propagateMinConstraints;
    }

    // Typed nodes also give the UI Toolkit debugger readable names.
    private static Box CreateBox(string? type) => type switch
    {
        null => new Box(),
        NodeTypes.AnimatedContent => new AnimatedContent(),
        NodeTypes.AnimatedContentItem => new AnimatedContentItem(),
        NodeTypes.AnimatedVisibility => new AnimatedVisibility(),
        NodeTypes.AnimatedVisibilityContent => new AnimatedVisibilityContent(),
        NodeTypes.HorizontalPager => new HorizontalPager(),
        NodeTypes.VerticalPager => new VerticalPager(),
        NodeTypes.PagerItem => new PagerItem(),
        _ => new Box { name = type },
    };
}

internal class AnimatedContent : Box
{
    public AnimatedContent()
    {
        RegisterCallback<GeometryChangedEvent>(_ => FillItems());
    }

    public override void Track(VisualElement child)
    {
        FillItem(child);
        base.Track(child);
    }

    private void FillItems()
    {
        foreach (var child in Children())
            FillItem(child);
    }

    private void FillItem(VisualElement child)
    {
        if (child is not AnimatedContentItem)
            return;
        // Pass a bounded host's content size through the transition wrapper.
        // Auto-sized hosts stay intrinsic to avoid a percentage sizing cycle.
        FillDimension(child, style.width, true);
        FillDimension(child, style.height, false);
    }

    private static void FillDimension(VisualElement child, StyleLength parentSize, bool horizontal)
    {
        var current = horizontal ? child.style.width : child.style.height;
        // Hidden retained entries have an explicit zero size; preserve it.
        if (current.keyword == StyleKeyword.Undefined && current.value.unit == LengthUnit.Pixel)
            return;
        StyleLength next = parentSize.keyword == StyleKeyword.Undefined
            ? new Length(100, LengthUnit.Percent)
            : StyleKeyword.Null;
        if (current == next)
            return;
        if (horizontal) child.style.width = next;
        else child.style.height = next;
    }
}

internal class AnimatedContentItem : Box
{
}

internal class AnimatedVisibility : Box
{
}

internal class AnimatedVisibilityContent : Box
{
}

internal class HorizontalPager : Box
{
}

internal class VerticalPager : Box
{
}

internal class PagerItem : Box
{
}
