using System;
using Compose.Net;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Runtime.Adapters.Transitions.Enter;

internal record ExpandEnterTransitionImpl(
    AnimationSpec AnimationSpec,
    Alignment ExpandFrom,
    bool Clip,
    Func<Size, Size> InitialSize
) : IEnterTransition, IUnityStyleTransition
{
    public IModifier CreateModifier(TimeSpan timeElapsed) => new UnityTransitionModifier(this, timeElapsed);
    public void UpdateStyle(UnityStyleModifierNode node, TimeSpan timeElapsed)
    {
        var progress = AnimationSpec.GetProgress(timeElapsed);
        var size = node.Element.layout.size.ToSize();
        SizeTransitionUtils.UpdateStyle(node, size, InitialSize(size), progress, true, ExpandFrom, Clip, true, true);
    }

    public TimeSpan TotalDuration => AnimationSpec.TotalDuration;




}
