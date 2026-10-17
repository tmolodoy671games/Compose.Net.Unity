// ReSharper disable ArrangeNamespaceBody

using Compose.Net;
using UnityEngine.UIElements;

namespace UnityCompose.Samples.Behaviors.Content
{
    internal partial class SharedTransitionLayoutSample : ComposeUI
    {
        [Composable]
        protected override void Content()
        {
            Layout();
        }

        [Composable]
        protected override void Preview()
        {
            Layout();
        }

        [Composable]
        private static void Layout()
        {
            var selected = Remember(static () => MutableStateOf(-1));
            var tween = Tween(500);
            SharedTransitionLayout(
                modifier: Modifier.FillMaxSize(),
                content: transitionScope =>
                {
                    AnimatedContent(
                        targetState: selected.Value,
                        transitionSpec: _ => FadeIn(tween).TogetherWith(FadeOut(tween)),
                        contentAlignment: Alignment.Center,
                        modifier: Modifier.FillMaxSize(),
                        content: (scope, it) =>
                        {
                            if (it < 0)
                            {
                                Row(() =>
                                {
                                    Thumb(transitionScope, scope, 0, Color.DodgerBlue, selected);
                                    Thumb(transitionScope, scope, 1, Color.Coral, selected);
                                    Thumb(transitionScope, scope, 2, Color.MediumSeaGreen, selected);
                                });
                            }
                            else
                            {
                                Detail(transitionScope, scope, it, it switch
                                {
                                    0 => Color.DodgerBlue,
                                    1 => Color.Coral,
                                    _ => Color.MediumSeaGreen
                                }, selected);
                            }
                        }
                    );
                }
            );
        }

        [Composable]
        private static void Thumb(SharedTransitionScope s, AnimatedVisibilityScope a, int index, Color color, IMutableState<int> selected)
        {
            Box(
                contentAlignment: Alignment.Center,
                modifier: Modifier
                    .Padding(all: 16.Dp())
                    .Size(200.Dp())
                    .SharedBounds(s.RememberSharedContentState($"card{index}"), a, boundsTransform: (_, _) => Tween(500))
                    .Background(color, RoundedCornerShape(24.Dp()))
                    .Clickable(() => selected.Value = index, indication: null, interactionSource: null),
                content: () => Text(
                    $"#{index}",
                    color: Color.White,
                    fontSize: 48.Sp(),
                    modifier: Modifier
                        .SharedBounds(s.RememberSharedContentState($"text {index}"), a, boundsTransform: (_, _) => Tween(500))
                )
            );
        }

        [Composable]
        private static void Detail(SharedTransitionScope s, AnimatedVisibilityScope a, int index, Color color, IMutableState<int> selected)
        {
            Box(
                contentAlignment: Alignment.TopLeft,
                modifier: Modifier
                    .FillMaxSize()
                    .SharedBounds(s.RememberSharedContentState($"card{index}"), a, boundsTransform: (_, _) => Tween(500))
                    .Background(color)
                    .Clickable(() => selected.Value = -1, indication: null, interactionSource: null),
                content: () => Text(
                    $"#{index}",
                    color: Color.White,
                    fontSize: 128.Sp(),
                    modifier: Modifier.Padding(all: 32.Dp())
                        .SharedBounds(s.RememberSharedContentState($"text {index}"), a, boundsTransform: (_, _) => Tween(500))
                )
            );
        }
    }
}