using System;
using Compose.Net;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Runtime.Adapters.Transitions.Enter;

internal record ExpandVerticallyEnterTransitionImpl(
    AnimationSpec AnimationSpec,
    Alignment.Vertical ExpandFrom,
    bool Clip,
    Func<float, float> InitialHeight
) : IEnterTransition, IUnityStyleTransition
{
    public IModifier CreateModifier(TimeSpan timeElapsed) => new UnityTransitionModifier(this, timeElapsed);
    public void UpdateStyle(UnityStyleModifierNode node, TimeSpan timeElapsed)
    {
        var progress = AnimationSpec.GetProgress(timeElapsed);
        var size = node.Element.layout.size.ToSize();
        SizeTransitionUtils.UpdateStyle(node, size, new Size(size.Width, InitialHeight(size.Height)), progress, true, ExpandFrom, Clip, false, true);
    }

    public TimeSpan TotalDuration => AnimationSpec.TotalDuration;




}
