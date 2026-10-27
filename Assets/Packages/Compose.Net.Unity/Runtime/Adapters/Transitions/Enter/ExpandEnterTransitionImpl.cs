using System;
using Compose.Net;

namespace Compose.Net.Unity.Runtime.Adapters.Transitions.Enter;

internal record ExpandEnterTransitionImpl(
    AnimationSpec AnimationSpec,
    Alignment ExpandFrom,
    bool Clip,
    Func<Size, Size> InitialSize
) : IEnterTransition
{
    public TimeSpan TotalDuration => AnimationSpec.TotalDuration;

    public void Apply(IReusableComposeNode node, TimeSpan timeElapsed)
    {
        var element = node.VisualElement();
        var size = element.layout.size.ToSize();
        var otherSize = InitialSize(size);
        var progress = AnimationSpec.GetProgress(timeElapsed);
        SizeTransitionUtils.Apply(element, size, otherSize, progress, true,
            ExpandFrom, Clip, true, true);
    }

    public void Revert(IReusableComposeNode node, TimeSpan timeElapsed)
        => SizeTransitionUtils.Revert(node.VisualElement(), Clip, true, true);
}
