using System;
using System.Collections.Generic;
using System.Linq;
using SharpExtensions;
using Compose.Net;
using Compose.Net.Unity.Runtime.Adapters.Nodes;
using static Compose.Net.ComposeFunctions;

namespace Compose.Net.Unity.Runtime.Adapters;

internal partial class UnityComposeLayouts : IComposeLayouts
{
    /// <summary>Scoped visibility participates in shared transitions and retains exiting content.</summary>
    [Composable]
    public void AnimatedVisibility(bool visible, [Composable] Action<AnimatedVisibilityScope> content,
        IEnterTransition? enter = null, IExitTransition? exit = null, IModifier? modifier = null)
    {
        ComposeFunctions.AnimatedContent(visible, content: (scope, show) => { if (show) content(scope); },
            transitionSpec: _ => (enter ?? FadeIn() + ExpandVertically())
                .TogetherWith(exit ?? FadeOut() + ShrinkVertically()).Using(null), modifier: modifier);
    }

    [Composable]
    public void AnimatedVisibility(
        bool visible,
        [Composable] Action content,
        IEnterTransition? enter = null,
        IExitTransition? exit = null,
        IModifier? modifier = null
    )
    {
        var resolvedEnter = enter ?? FadeIn() + ExpandVertically();
        var resolvedExit = exit ?? FadeOut() + ShrinkVertically();
        var duration = visible ? resolvedEnter.TotalDuration : resolvedExit.TotalDuration;
        var progressAnimationSpec = Tween(duration: duration, easing: LinearEasing);
        var visibilityProgress = AnimateFloatAsState(visible.ToInt(), progressAnimationSpec).Value;
        var absoluteProgress = visible ? visibilityProgress : 1 - visibilityProgress;
        var timeElapsed = duration * absoluteProgress;
        // if (visibilityProgress <= 0)
        //     return;
        // The transition is applied to the inner content node, the outer node clips it.
        var contentModifier = visible
            ? Modifier.EnterTransition(resolvedEnter, timeElapsed)
            : Modifier.ExitTransition(resolvedExit, timeElapsed);
        BoxNode(
            nodeFactory: () => new UnityReusableComposeNode(new AnimatedVisibility()),
            modifier: modifier.OrEmpty()
                .Clip(),
            content: () => BoxNode(
                nodeFactory: () => new UnityReusableComposeNode(new AnimatedVisibilityContent()),
                modifier: contentModifier,
                content: content
            )
        );
    }
}