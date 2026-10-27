using System;
using Compose.Net;

namespace Compose.Net.Unity.Runtime.Adapters.Transitions.Exit;

internal record ShrinkVerticallyExitTransitionImpl(
    AnimationSpec AnimationSpec,
    Alignment.Vertical ShrinkTowards,
    bool Clip,
    Func<float, float> TargetHeight
) : IExitTransition
{
    public TimeSpan TotalDuration => AnimationSpec.TotalDuration;

    public void Apply(IReusableComposeNode node, TimeSpan timeElapsed)
    {
        var element = node.VisualElement();
        var size = element.layout.size.ToSize();
        var otherSize = new Size(size.Width, TargetHeight(size.Height));
        var progress = AnimationSpec.GetProgress(timeElapsed);
        SizeTransitionUtils.Apply(element, size, otherSize, progress, false,
            ShrinkTowards, Clip, false, true);
    }

    public void Revert(IReusableComposeNode node, TimeSpan timeElapsed)
        => SizeTransitionUtils.Revert(node.VisualElement(), Clip, false, true);
}
