using System;
using Compose.Net;

namespace Compose.Net.Unity.Runtime.Adapters.Transitions.Exit;

internal record ShrinkHorizontallyExitTransitionImpl(
    AnimationSpec AnimationSpec,
    Alignment.Horizontal ShrinkTowards,
    bool Clip,
    Func<float, float> TargetWidth
) : IExitTransition
{
    public TimeSpan TotalDuration => AnimationSpec.TotalDuration;

    public void Apply(IReusableComposeNode node, TimeSpan timeElapsed)
    {
        var element = node.VisualElement();
        var size = element.layout.size.ToSize();
        var otherSize = new Size(TargetWidth(size.Width), size.Height);
        var progress = AnimationSpec.GetProgress(timeElapsed);
        SizeTransitionUtils.Apply(element, size, otherSize, progress, false,
            ShrinkTowards, Clip, true, false);
    }

    public void Revert(IReusableComposeNode node, TimeSpan timeElapsed)
        => SizeTransitionUtils.Revert(node.VisualElement(), Clip, true, false);
}
