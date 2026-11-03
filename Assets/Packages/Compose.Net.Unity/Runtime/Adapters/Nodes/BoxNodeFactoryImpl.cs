using Compose.Net;

namespace Compose.Net.Unity.Runtime.Adapters.Nodes;

internal class BoxNodeFactoryImpl : IBoxNodeFactory
{
    public IReusableComposeNode CreateNode(string? type = null)
    {
        return type switch
        {
            NodeTypes.LazyColumn => new LazyListNode(false),
            NodeTypes.LazyRow => new LazyListNode(true),
            NodeTypes.LazyListItem => new LazyListItemNode(),
            NodeTypes.HorizontalPager => new PagerLayoutNode(true),
            NodeTypes.VerticalPager => new PagerLayoutNode(false),
            NodeTypes.PagerItem => new PagerItemNode(),
            NodeTypes.LazyVerticalGrid => new LazyGridNode(false),
            NodeTypes.LazyHorizontalGrid => new LazyGridNode(true),
            NodeTypes.LazyGridItem => new LazyGridItemNode(),
            NodeTypes.AnimatedContent => new AnimatedContentLayoutNode(new AnimatedContent()),
            NodeTypes.AnimatedContentItem => new AnimatedContentItemNode(new AnimatedContentItem()),
            _ => new UnityReusableComposeNode(CreateBox(type))
        };
    }

    public void Apply(IReusableComposeNode node, Alignment alignment, bool propagateMinConstraints = false)
    {
        var box = node.VisualElement<Box>();
        box.ContentAlignment = alignment;
        box.PropagateMinConstraints = propagateMinConstraints;
        box.RefreshConstraints();
    }

    // Typed nodes only differ by class, so the UI Toolkit debugger shows a readable name.
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
