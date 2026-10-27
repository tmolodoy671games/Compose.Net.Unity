using System;
using Compose.Net;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Runtime.Adapters.Transitions.Enter;

internal record ScaleInEnterTransitionImpl(
    AnimationSpec AnimationSpec,
    float InitialScale,
    Net.TransformOrigin TransformOrigin
) : IEnterTransition, IUnityStyleTransition
{
    public IModifier CreateModifier(TimeSpan timeElapsed) => new UnityTransitionModifier(this, timeElapsed);
    public void UpdateStyle(UnityStyleModifierNode node, TimeSpan timeElapsed)
    {
        var progress = AnimationSpec.GetProgress(timeElapsed);
        var scale = Mathf.LerpUnclamped(InitialScale, 1f, progress);
        node.Set<StyleScale>(node.Styles.Scale, new Vector2(scale, scale));
        node.Set<StyleTransformOrigin>(node.Styles.TransformOrigin, new UnityEngine.UIElements.TransformOrigin(
            new Length(TransformOrigin.PivotFractionX * 100f, LengthUnit.Percent),
            new Length(TransformOrigin.PivotFractionY * 100f, LengthUnit.Percent)));
    }





    public TimeSpan TotalDuration => AnimationSpec.TotalDuration;
}
