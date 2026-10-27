using System;
using Compose.Net;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Runtime.Adapters.Transitions.Exit;

internal record ShrinkVerticallyExitTransitionImpl(
    AnimationSpec AnimationSpec,
    Alignment.Vertical ShrinkTowards,
    bool Clip,
    Func<float, float> TargetHeight
) : IExitTransition, IUnityStyleTransition
{
    public IModifier CreateModifier(TimeSpan timeElapsed) => new UnityTransitionModifier(this, timeElapsed);
    public void UpdateStyle(UnityStyleModifierNode node, TimeSpan timeElapsed)
    {
        var progress = AnimationSpec.GetProgress(timeElapsed);
        var size = node.Element.layout.size.ToSize();
        SizeTransitionUtils.UpdateStyle(node, size, new Size(size.Width, TargetHeight(size.Height)), progress, false, ShrinkTowards, Clip, false, true);
    }

    public TimeSpan TotalDuration => AnimationSpec.TotalDuration;




}
