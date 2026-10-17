using Compose.Net;

namespace Compose.Net.Unity.Runtime.Adapters.Nodes;

internal class BoxNodeFactoryImpl : IBoxNodeFactory
{
    public IReusableComposeNode CreateNode(string? type = null)
    {
        return new UnityReusableComposeNode(CreateBox(type));
    }

    public void Apply(IReusableComposeNode node, Alignment alignment)
    {
        node.VisualElement<Box>().ContentAlignment = alignment;
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

internal class AnimatedContent : Box
{
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
