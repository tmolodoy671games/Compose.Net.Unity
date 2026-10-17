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
        var progress = AnimationSpec.GetProgress(timeElapsed);
        var element = node.VisualElement();
        var initialOffset = InitialOffsetY(element.LayoutCoordinates().Size.Height);
        var offset = Mathf.LerpUnclamped(initialOffset, 0, progress);
        element.style.translate = new Vector2(0f, offset);
    }

    public void Revert(IReusableComposeNode node, TimeSpan timeElapsed)
    {
        node.VisualElement().style.translate = StyleKeyword.None;
    }

    public TimeSpan TotalDuration => AnimationSpec.TotalDuration;
}