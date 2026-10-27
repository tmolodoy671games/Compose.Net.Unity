using System;
using Compose.Net;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Runtime.Adapters.Transitions.Exit;

internal record ShrinkOutExitTransitionImpl(
    AnimationSpec AnimationSpec,
    Alignment ShrinkTowards,
    bool Clip,
    Func<Size, Size> TargetSize
) : IExitTransition, IUnityStyleTransition
{
    public IModifier CreateModifier(TimeSpan timeElapsed) => new UnityTransitionModifier(this, timeElapsed);
    public void UpdateStyle(UnityStyleModifierNode node, TimeSpan timeElapsed)
    {
        var progress = AnimationSpec.GetProgress(timeElapsed);
        var size = node.Element.layout.size.ToSize();
        SizeTransitionUtils.UpdateStyle(node, size, TargetSize(size), progress, false, ShrinkTowards, Clip, true, true);
    }

    public TimeSpan TotalDuration => AnimationSpec.TotalDuration;




}
