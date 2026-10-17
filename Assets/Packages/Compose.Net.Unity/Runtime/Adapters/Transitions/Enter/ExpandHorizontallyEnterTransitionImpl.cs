using System;
using Compose.Net;
using StableCollections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Runtime.Adapters.Transitions.Enter;

internal record ExpandHorizontallyEnterTransitionImpl(
    AnimationSpec AnimationSpec,
    bool Clip,
    Func<float, float> InitialWidth
) : IEnterTransition
{
    public void Apply(IReusableComposeNode node, TimeSpan timeElapsed)
    {
        var element = node.VisualElement();
        var progress = AnimationSpec.GetProgress(timeElapsed);
        var width = element.layout.width;
        if (width != 0 && !float.IsNaN(width))
        {
            progress = Mathf.LerpUnclamped(InitialWidth(width), width, progress);
            progress /= width;
        }

        element.style.flexShrink = 0;
        element.parent.style.maxWidth = ShrinkUtils.GetLength(progress);
        if (Clip)
            element.Clip().Increment();
    }

    public void Revert(IReusableComposeNode node, TimeSpan timeElapsed)
    {
        var element = node.VisualElement();
        element.style.flexShrink = StyleKeyword.Null;
        element.style.maxWidth = StyleKeyword.Null;
        if (Clip)
            element.Clip().Decrement();
    }

    public TimeSpan TotalDuration => AnimationSpec.TotalDuration;
}