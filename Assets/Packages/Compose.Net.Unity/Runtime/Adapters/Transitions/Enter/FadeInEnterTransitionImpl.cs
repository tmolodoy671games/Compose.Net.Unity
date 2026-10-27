using System;
using Compose.Net;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Runtime.Adapters.Transitions.Enter;

internal record FadeInEnterTransitionImpl(
    AnimationSpec AnimationSpec,
    float InitialAlpha
) : IEnterTransition, IUnityStyleTransition
{
    public IModifier CreateModifier(TimeSpan timeElapsed) => new UnityTransitionModifier(this, timeElapsed);
    public void UpdateStyle(UnityStyleModifierNode node, TimeSpan timeElapsed)
    {
        var progress = AnimationSpec.GetProgress(timeElapsed);
        node.Set<StyleFloat>(node.Styles.Opacity, Mathf.LerpUnclamped(InitialAlpha, 1f, progress));
    }





    public TimeSpan TotalDuration => AnimationSpec.TotalDuration;
}