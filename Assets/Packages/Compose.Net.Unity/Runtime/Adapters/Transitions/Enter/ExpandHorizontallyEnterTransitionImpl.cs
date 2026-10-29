using System;
using Compose.Net;

namespace Compose.Net.Unity.Runtime.Adapters.Transitions.Enter;

internal record ExpandHorizontallyEnterTransitionImpl(
    AnimationSpec AnimationSpec,
    Alignment.Horizontal ExpandFrom,
    bool Clip,
    Func<float, float> InitialWidth
) : IEnterTransition
{
    public TimeSpan TotalDuration => AnimationSpec.TotalDuration;

    public void Apply(IReusableComposeNode node, TimeSpan timeElapsed)
    {
        var element = node.VisualElement();
        if (SizeTransitionUtils.DeferUntilMeasured(element, () => Apply(node, timeElapsed), true, false)) return;
        var size = element.layout.size.ToSize();
        var otherSize = new Size(InitialWidth(size.Width), size.Height);
        var progress = AnimationSpec.GetProgress(timeElapsed);
        SizeTransitionUtils.Apply(element, size, otherSize, progress, true,
            ExpandFrom, Clip, true, false);
    }

    public void Revert(IReusableComposeNode node, TimeSpan timeElapsed)
        => SizeTransitionUtils.Revert(node.VisualElement(), Clip, true, false);
}
