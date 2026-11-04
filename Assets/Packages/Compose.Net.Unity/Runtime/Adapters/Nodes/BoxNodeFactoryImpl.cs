using Compose.Net;

namespace Compose.Net.Unity.Runtime.Adapters.Nodes;

internal class BoxNodeFactoryImpl : IBoxNodeFactory
{
    public IReusableComposeNode CreateNode() => new UnityReusableComposeNode(new Box());

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
