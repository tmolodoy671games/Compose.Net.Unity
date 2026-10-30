using System;
using Compose.Net;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Runtime.Adapters.Transitions.Enter;

internal record SlideInVerticallyTransitionImpl(
    AnimationSpec AnimationSpec,
    Func<float, float> InitialOffsetY
) : IEnterTransition
{
    public void Apply(IReusableComposeNode node, TimeSpan timeElapsed)
    {
        SlideTransitionUtils.Apply(node.VisualElement(), "Y", AnimationSpec, timeElapsed,
            size => new Offset(0, InitialOffsetY(size.Height)), entering: true);
    }

    public void Revert(IReusableComposeNode node, TimeSpan timeElapsed)
    {
        SlideTransitionUtils.Revert(node.VisualElement(), "Y");
    }

    public TimeSpan TotalDuration => AnimationSpec.TotalDuration;
}
