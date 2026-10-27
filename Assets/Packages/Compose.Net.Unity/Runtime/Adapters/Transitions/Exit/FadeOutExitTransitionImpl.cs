using System;
using Compose.Net;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Runtime.Adapters.Transitions.Exit;

internal record FadeOutExitTransitionImpl(
    AnimationSpec AnimationSpec,
    float TargetAlpha
) : IExitTransition, IUnityStyleTransition
{
    public IModifier CreateModifier(TimeSpan timeElapsed) => new UnityTransitionModifier(this, timeElapsed);
    public void UpdateStyle(UnityStyleModifierNode node, TimeSpan timeElapsed)
    {
        var progress = AnimationSpec.GetProgress(timeElapsed);
        node.Set<StyleFloat>(node.Styles.Opacity, Mathf.LerpUnclamped(1f, TargetAlpha, progress));
    }





    public TimeSpan TotalDuration => AnimationSpec.TotalDuration;
}