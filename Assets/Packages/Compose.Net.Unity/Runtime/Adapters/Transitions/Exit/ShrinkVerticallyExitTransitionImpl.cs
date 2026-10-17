using System;
using Compose.Net;
using Compose.Net.Unity.Runtime.Adapters.Transitions.Enter;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Runtime.Adapters.Transitions.Exit;

internal record ShrinkVerticallyExitTransitionImpl(
    AnimationSpec AnimationSpec,
    bool Clip,
    Func<float, float> TargetHeight
) : IExitTransition
{
    public TimeSpan TotalDuration => AnimationSpec.TotalDuration;

    public void Apply(IReusableComposeNode node, TimeSpan timeElapsed)
    {
        var element = node.VisualElement();
        var progress = AnimationSpec.GetProgress(timeElapsed);
        var height = element.layout.height;
        if (height != 0 && !float.IsNaN(height))
        {
            progress = Mathf.LerpUnclamped(height, TargetHeight(height), progress);
            progress /= height;
        }

        element.style.flexShrink = 0;
        element.parent.style.maxHeight = ShrinkUtils.GetLength(progress);
        if (Clip)
            element.Clip().Increment();
    }

    public void Revert(IReusableComposeNode node, TimeSpan timeElapsed)
    {
        var element = node.VisualElement();
        element.style.flexShrink = StyleKeyword.Null;
        element.style.maxHeight = StyleKeyword.Null;
        if (Clip)
            element.Clip().Decrement();
    }
}