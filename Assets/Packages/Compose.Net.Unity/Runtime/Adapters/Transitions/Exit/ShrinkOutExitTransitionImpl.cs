using System;
using Compose.Net;

namespace Compose.Net.Unity.Runtime.Adapters.Transitions.Exit;

internal record ShrinkOutExitTransitionImpl(
    AnimationSpec AnimationSpec,
    Alignment ShrinkTowards,
    bool Clip,
    Func<Size, Size> TargetSize
) : IExitTransition
{
    public TimeSpan TotalDuration => AnimationSpec.TotalDuration;

    public void Apply(IReusableComposeNode node, TimeSpan timeElapsed)
    {
        var element = node.VisualElement();
        var size = element.layout.size.ToSize();
        var otherSize = TargetSize(size);
        var progress = AnimationSpec.GetProgress(timeElapsed);
        SizeTransitionUtils.Apply(element, size, otherSize, progress, false,
            ShrinkTowards, Clip, true, true);
    }

    public void Revert(IReusableComposeNode node, TimeSpan timeElapsed)
        => SizeTransitionUtils.Revert(node.VisualElement(), Clip, true, true);
}
