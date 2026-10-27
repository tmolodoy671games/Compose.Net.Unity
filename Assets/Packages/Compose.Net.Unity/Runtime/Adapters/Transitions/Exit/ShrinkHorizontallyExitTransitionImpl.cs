using System;
using Compose.Net;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Runtime.Adapters.Transitions.Exit;

internal record ShrinkHorizontallyExitTransitionImpl(
    AnimationSpec AnimationSpec,
    Alignment.Horizontal ShrinkTowards,
    bool Clip,
    Func<float, float> TargetWidth
) : IExitTransition, IUnityStyleTransition
{
    public IModifier CreateModifier(TimeSpan timeElapsed) => new UnityTransitionModifier(this, timeElapsed);
    public void UpdateStyle(UnityStyleModifierNode node, TimeSpan timeElapsed)
    {
        var progress = AnimationSpec.GetProgress(timeElapsed);
        var size = node.Element.layout.size.ToSize();
        SizeTransitionUtils.UpdateStyle(node, size, new Size(TargetWidth(size.Width), size.Height), progress, false, ShrinkTowards, Clip, true, false);
    }

    public TimeSpan TotalDuration => AnimationSpec.TotalDuration;




}
