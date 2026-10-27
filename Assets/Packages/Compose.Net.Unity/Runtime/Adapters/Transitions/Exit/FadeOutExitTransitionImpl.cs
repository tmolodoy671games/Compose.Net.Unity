using System;
using Compose.Net;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Runtime.Adapters.Transitions.Exit;

internal record FadeOutExitTransitionImpl(
    AnimationSpec AnimationSpec,
    float TargetAlpha
) : IExitTransition
{
    public void Apply(IReusableComposeNode node, TimeSpan timeElapsed)
    {
        var progress = AnimationSpec.GetProgress(timeElapsed);
        node.VisualElement().style.opacity = Mathf.LerpUnclamped(1, TargetAlpha, progress);
    }

    public void Revert(IReusableComposeNode node, TimeSpan timeElapsed)
    {
        node.VisualElement().style.opacity = StyleKeyword.None;
    }

    public TimeSpan TotalDuration => AnimationSpec.TotalDuration;
}