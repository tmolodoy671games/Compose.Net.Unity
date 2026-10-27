using System;
using Compose.Net;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Runtime.Adapters.Transitions.Enter;

internal record SlideInVerticallyTransitionImpl(
    AnimationSpec AnimationSpec,
    Func<float, float> InitialOffsetY
) : IEnterTransition, IUnityStyleTransition
{
    public IModifier CreateModifier(TimeSpan timeElapsed) => new UnityTransitionModifier(this, timeElapsed);
    public void UpdateStyle(UnityStyleModifierNode node, TimeSpan timeElapsed)
    {
        var progress = AnimationSpec.GetProgress(timeElapsed);
        var size = node.Element.LayoutCoordinates().Size;
        var offset = new Offset(0, InitialOffsetY(size.Height));
        var value = Offset.LerpUnclamped(offset, Offset.Zero, progress);
        node.Set<StyleTranslate>(node.Styles.Translate, new Translate(value.X, value.Y));
    }





    public TimeSpan TotalDuration => AnimationSpec.TotalDuration;
}