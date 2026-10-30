using System;
using Compose.Net;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Runtime.Adapters.Transitions.Enter;

internal record SlideInEnterTransitionImpl(
    AnimationSpec AnimationSpec,
    Func<Size, Offset> InitialOffset
) : IEnterTransition
{
    public void Apply(IReusableComposeNode node, TimeSpan timeElapsed)
    {
        SlideTransitionUtils.Apply(node.VisualElement(), "XY", AnimationSpec, timeElapsed, InitialOffset, entering: true);
    }

    public void Revert(IReusableComposeNode node, TimeSpan timeElapsed)
    {
        SlideTransitionUtils.Revert(node.VisualElement(), "XY");
    }

    public TimeSpan TotalDuration => AnimationSpec.TotalDuration;
}
