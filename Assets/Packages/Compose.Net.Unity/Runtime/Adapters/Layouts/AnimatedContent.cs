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
    [Composable]
    public void AnimatedContent<T>(Transition<T> transition, [Composable] Action<AnimatedContentScope, T> content,
        Func<AnimatedContentTransitionScope<T>, ContentTransform>? transitionSpec = null,
        Alignment? contentAlignment = null, IModifier? modifier = null, Func<T, object?>? contentKey = null)
    {
        var model = Remember(transition, () => new AnimatedContentModel<T>(transition, ComposeFunctions.Adapter.FrameClock));
        var revision = model.Revision.Value;
        var resolvedSpec = transitionSpec;
        if (resolvedSpec == null) resolvedSpec = scope => DefaultContentTransform(scope);
        model.Update(resolvedSpec!, contentKey ?? (state => state),
            contentAlignment ?? Alignment.TopStart, LocalAnimatedContentRetain.Current);
        BoxNode(() => new AnimatedContentLayoutNode(new AnimatedContent()), contentAlignment: contentAlignment,
            modifier: modifier.OrEmpty() + new AnimatedContentLayoutModifier(model.Layout, revision, model.Layout.ContentAlignment, transition.Label), content: () =>
            {
                foreach (var entry in model.Entries.OrderBy(entry => entry.Transform.TargetContentZIndex).ToArray())
                    Key(entry.Id, () => AnimatedContentItem(model, entry, entry.Value, model.Revision.Value, content));
            });
        // Start after composing the children, so child animation durations participate in disposal.
        LaunchedEffect(model.WorkVersion, async scope =>
        {
            model.Tick();
            while (model.IsRunning)
            {
                await model.Clock.NextFrame(scope.Token);
                scope.Token.ThrowIfCancellationRequested();
                model.Tick();
            }
        });
    }

    private static ContentTransform DefaultContentTransform<T>(AnimatedContentTransitionScope<T> _) =>
        (FadeIn(Tween(220, 90, AnimatedContentDefaults.Easing)) + ScaleIn(Tween(220, 90, AnimatedContentDefaults.Easing), initialScale: .92f)).TogetherWith(FadeOut(Tween(90, easing: AnimatedContentDefaults.Easing)));

    [Composable]
    private static void AnimatedContentItem<T>(AnimatedContentModel<T> model, AnimatedContentEntry<T> entry, T value, int revision,
        [Composable] Action<AnimatedContentScope, T> content)
    {
        var scope = entry.Scope;
        var transitionModifier = new ContentTransitionModifier(entry.Transform.Enter, entry.Transform.Exit,
            entry.Entering, scope.Elapsed, entry.Phase, entry.Finished, revision);
        var itemModifier = Modifier.ZIndex(entry.Transform.TargetContentZIndex)
            + new AnimatedContentItemModifier(entry.Id, entry.Id == model.Target.Id, entry.Hidden, model.Revision.Value);
        if (entry.Hidden) itemModifier = itemModifier.Visible(false).Size(0.Dp()).Clip();
        BoxNode(() => new AnimatedContentItemNode(new AnimatedContentItem()), modifier: itemModifier,
            content: () => Box(modifier: transitionModifier,
                content: () => CompositionLocalProvider(LocalAnimatedVisibilityScope.Provides(scope),
                    () => content(scope, value))));
    }
}
