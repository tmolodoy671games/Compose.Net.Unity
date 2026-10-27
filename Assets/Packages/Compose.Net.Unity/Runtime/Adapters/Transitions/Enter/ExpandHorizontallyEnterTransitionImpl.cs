using System;
using Compose.Net;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Runtime.Adapters.Transitions.Enter;

internal record ExpandHorizontallyEnterTransitionImpl(
    AnimationSpec AnimationSpec,
    Alignment.Horizontal ExpandFrom,
    bool Clip,
    Func<float, float> InitialWidth
) : IEnterTransition, IUnityStyleTransition
{
    public IModifier CreateModifier(TimeSpan timeElapsed) => new UnityTransitionModifier(this, timeElapsed);
    public void UpdateStyle(UnityStyleModifierNode node, TimeSpan timeElapsed)
    {
        var progress = AnimationSpec.GetProgress(timeElapsed);
        var size = node.Element.layout.size.ToSize();
        SizeTransitionUtils.UpdateStyle(node, size, new Size(InitialWidth(size.Width), size.Height), progress, true, ExpandFrom, Clip, true, false);
    }

    public TimeSpan TotalDuration => AnimationSpec.TotalDuration;




}
