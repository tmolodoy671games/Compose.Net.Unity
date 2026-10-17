using System;
using Compose.Net;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Runtime.Adapters.Transitions.Enter;

internal record FadeInEnterTransitionImpl(
    AnimationSpec AnimationSpec,
    float InitialAlpha
) : IEnterTransition
{
    public void Apply(IReusableComposeNode node, TimeSpan timeElapsed)
    {
        var progress = AnimationSpec.GetProgress(timeElapsed);
        node.VisualElement().style.opacity = Mathf.LerpUnclamped(InitialAlpha, 1f, progress);
    }

    public void Revert(IReusableComposeNode node, TimeSpan timeElapsed)
    {
        node.VisualElement().style.opacity = StyleKeyword.None;
    }

    public TimeSpan TotalDuration => AnimationSpec.TotalDuration;
}