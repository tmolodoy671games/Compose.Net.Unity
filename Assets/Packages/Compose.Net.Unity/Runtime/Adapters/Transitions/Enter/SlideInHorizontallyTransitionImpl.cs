using System;
using Compose.Net;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Runtime.Adapters.Transitions.Enter;

internal record SlideInHorizontallyTransitionImpl(
    AnimationSpec AnimationSpec,
    Func<float, float> InitialOffsetX
) : IEnterTransition
{
    public void Apply(IReusableComposeNode node, TimeSpan timeElapsed)
    {
        SlideTransitionUtils.Apply(node.VisualElement(), "X", AnimationSpec, timeElapsed,
            size => new Offset(InitialOffsetX(size.Width), 0), entering: true);
    }

    public void Revert(IReusableComposeNode node, TimeSpan timeElapsed)
    {
        SlideTransitionUtils.Revert(node.VisualElement(), "X");
    }

    public TimeSpan TotalDuration => AnimationSpec.TotalDuration;
}
