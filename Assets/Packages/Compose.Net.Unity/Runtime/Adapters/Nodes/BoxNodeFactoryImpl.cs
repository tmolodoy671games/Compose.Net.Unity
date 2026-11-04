using Compose.Net;

namespace Compose.Net.Unity.Runtime.Adapters.Nodes;

internal class BoxNodeFactoryImpl : IBoxNodeFactory
{
    public IReusableComposeNode CreateNode() => new UnityReusableComposeNode(new Box());

    public IReusableComposeNode CreateLazyListNode(LazyListOrientation orientation) =>
        new LazyListNode(orientation == LazyListOrientation.Horizontal);
    public IReusableComposeNode CreateLazyListItemNode() => new LazyListItemNode();
    public IReusableComposeNode CreateLazyGridNode(LazyGridOrientation orientation) =>
        new LazyGridNode(orientation == LazyGridOrientation.Horizontal);
    public IReusableComposeNode CreateLazyGridItemNode() => new LazyGridItemNode();
    public IReusableComposeNode CreatePagerNode(PagerOrientation orientation) =>
        new PagerLayoutNode(orientation == PagerOrientation.Horizontal);
    public IReusableComposeNode CreatePagerItemNode() => new PagerItemNode();
    public IReusableComposeNode CreateAnimatedContentNode() => new AnimatedContentLayoutNode(new AnimatedContent());
    public IReusableComposeNode CreateAnimatedContentItemNode() => new AnimatedContentItemNode(new AnimatedContentItem());
    public IReusableComposeNode CreateAnimatedVisibilityNode() => new UnityReusableComposeNode(new AnimatedVisibility());
    public IReusableComposeNode CreateAnimatedVisibilityContentNode() => new UnityReusableComposeNode(new AnimatedVisibilityContent());

    public void Apply(IReusableComposeNode node, Alignment alignment, bool propagateMinConstraints = false)
    {
        var box = node.VisualElement<Box>();
        box.ContentAlignment = alignment;
        box.PropagateMinConstraints = propagateMinConstraints;
        box.RefreshConstraints();
    }

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
