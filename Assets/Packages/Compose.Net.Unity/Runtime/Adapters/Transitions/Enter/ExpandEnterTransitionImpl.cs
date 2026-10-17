using System;
using Compose.Net;
using StableCollections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Runtime.Adapters.Transitions.Enter;

internal record ExpandEnterTransitionImpl(
    AnimationSpec AnimationSpec,
    bool Clip,
    Func<Size, Size> InitialSize
) : IEnterTransition
{
    public void Apply(IReusableComposeNode node, TimeSpan timeElapsed)
    {
        var element = node.VisualElement();
        element.style.flexShrink = 0;
        var size = element.layout.size.ToSize();
        if (Clip)
            element.Clip().Increment();
        if (size.Width == 0 || float.IsNaN(size.Width) || size.Height == 0 || float.IsNaN(size.Height)) return;

        var initialSize = InitialSize(size);
        var progress = AnimationSpec.GetProgress(timeElapsed);

        var widthProgress = progress;
        widthProgress = Mathf.LerpUnclamped(initialSize.Width, size.Width, widthProgress);
        widthProgress /= size.Width;
        element.parent.style.maxHeight = ShrinkUtils.GetLength(widthProgress);

        var heightProgress = progress;
        heightProgress = Mathf.LerpUnclamped(initialSize.Height, size.Height, heightProgress);
        heightProgress /= size.Height;
        element.parent.style.maxHeight = ShrinkUtils.GetLength(heightProgress);
    }

    public void Revert(IReusableComposeNode node, TimeSpan timeElapsed)
    {
        var element = node.VisualElement();
        if (Clip)
            element.Clip().Decrement();
        element.style.maxWidth = StyleKeyword.Null;
        element.style.maxHeight = StyleKeyword.Null;
    }

    public TimeSpan TotalDuration => AnimationSpec.TotalDuration;
}