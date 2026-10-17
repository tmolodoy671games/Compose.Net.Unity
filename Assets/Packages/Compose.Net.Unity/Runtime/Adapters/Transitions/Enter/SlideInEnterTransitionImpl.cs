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
        var progress = AnimationSpec.GetProgress(timeElapsed);
        var initialOffset = InitialOffset(node.VisualElement().LayoutCoordinates().Size);
        var targetOffset = new Offset(0, 0);
        var offset = Offset.LerpUnclamped(initialOffset, targetOffset, progress);
        node.VisualElement().style.translate = offset.ToVector2();
    }

    public void Revert(IReusableComposeNode node, TimeSpan timeElapsed)
    {
        node.VisualElement().style.translate = StyleKeyword.None;
    }

    public TimeSpan TotalDuration => AnimationSpec.TotalDuration;
}