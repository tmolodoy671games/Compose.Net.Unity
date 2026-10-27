using System;
using Compose.Net;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Runtime.Adapters.Transitions.Exit;

internal record ScaleOutExitTransitionImpl(
    AnimationSpec AnimationSpec,
    float TargetScale,
    Net.TransformOrigin TransformOrigin
) : IExitTransition
{
    public void Apply(IReusableComposeNode node, TimeSpan timeElapsed)
    {
        var element = node.VisualElement();
        var scale = Mathf.LerpUnclamped(1f, TargetScale, AnimationSpec.GetProgress(timeElapsed));
        element.style.transformOrigin = new UnityEngine.UIElements.TransformOrigin(
            new Length(TransformOrigin.PivotFractionX * 100f, LengthUnit.Percent),
            new Length(TransformOrigin.PivotFractionY * 100f, LengthUnit.Percent));
        element.style.scale = new Vector2(scale, scale);
    }

    public void Revert(IReusableComposeNode node, TimeSpan timeElapsed)
    {
        var element = node.VisualElement();
        element.style.scale = StyleKeyword.Null;
        element.style.transformOrigin = StyleKeyword.Null;
    }

    public TimeSpan TotalDuration => AnimationSpec.TotalDuration;
}
