using System;
using Compose.Net;

namespace Compose.Net.Unity.Runtime.Adapters.Transitions.Enter;

internal record ExpandVerticallyEnterTransitionImpl(
    AnimationSpec AnimationSpec,
    Alignment.Vertical ExpandFrom,
    bool Clip,
    Func<float, float> InitialHeight
) : IEnterTransition
{
    public TimeSpan TotalDuration => AnimationSpec.TotalDuration;

    public void Apply(IReusableComposeNode node, TimeSpan timeElapsed)
    {
        var element = node.VisualElement();
        var size = element.layout.size.ToSize();
        var otherSize = new Size(size.Width, InitialHeight(size.Height));
        var progress = AnimationSpec.GetProgress(timeElapsed);
        SizeTransitionUtils.Apply(element, size, otherSize, progress, true,
            ExpandFrom, Clip, false, true);
    }

    public void Revert(IReusableComposeNode node, TimeSpan timeElapsed)
        => SizeTransitionUtils.Revert(node.VisualElement(), Clip, false, true);
}
