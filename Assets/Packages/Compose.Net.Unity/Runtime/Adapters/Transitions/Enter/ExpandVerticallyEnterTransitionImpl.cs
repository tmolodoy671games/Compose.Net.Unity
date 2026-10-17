using System;
using Compose.Net;
using SharpExtensions;
using StableCollections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Runtime.Adapters.Transitions.Enter;

internal record ExpandVerticallyEnterTransitionImpl(
    AnimationSpec AnimationSpec,
    bool Clip,
    Func<float, float> InitialHeight
) : IEnterTransition
{
    public TimeSpan TotalDuration => AnimationSpec.TotalDuration;

    public void Apply(IReusableComposeNode node, TimeSpan timeElapsed)
    {
        var element = node.VisualElement();
        var progress = AnimationSpec.GetProgress(timeElapsed);
        var height = element.layout.height;
        if (height != 0 && !float.IsNaN(height))
        {
            progress = Mathf.LerpUnclamped(InitialHeight(height), height, progress);
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

internal static class ShrinkUtils
{
    public static Length GetLength(float progress)
    {
        var result = new Length(progress * 8, LengthUnit.Percent);
        if (progress.AlmostEquals(1))
            result = new Length(100, LengthUnit.Percent);
        return result;
    }
}