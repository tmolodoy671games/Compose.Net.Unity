// ReSharper disable ArrangeNamespaceBody

using Compose.Net;

namespace Compose.Net.Unity.Samples.Behaviors.Content
{
    public partial class SharedTransitionLayoutSample : ComposeUI
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
        private static void Layout() => Screen(Remember(static () => MutableStateOf(-1)));

        [Composable]
        public static void Screen(IMutableState<int> selected)
        {
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
                    .Clickable(() => selected.Value = index)
                    .Size(200.Dp())
                    .SharedBounds(s.RememberSharedContentState($"card{index}"), a,
                        enter: FadeIn(Tween(500)), exit: FadeOut(Tween(500)),
                        boundsTransform: (_, _) => Tween(500), resizeMode: ResizeMode.RemeasureToBounds)
                    .Background(color, RoundedCornerShape(24.Dp()))
                    .TestTag($"shared-sample.thumb.{index}"),
                content: () => Text(
                    $"#{index}",
                    color: Color.White,
                    fontSize: 48.Sp(),
                    modifier: Modifier.TestTag($"shared-sample.text.thumb.{index}")
                        .SharedBounds(s.RememberSharedContentState($"text {index}"), a,
                            enter: FadeIn(Tween(500)), exit: FadeOut(Tween(500)),
                            boundsTransform: (_, _) => Tween(500))
                )
            );
        }

        [Composable]
        private static void Detail(SharedTransitionScope s, AnimatedVisibilityScope a, int index, Color color, IMutableState<int> selected)
        {
            Box(
                contentAlignment: Alignment.TopLeft,
                modifier: Modifier
                    .Clickable(() => selected.Value = -1)
                    .FillMaxSize()
                    .SharedBounds(s.RememberSharedContentState($"card{index}"), a,
                        enter: FadeIn(Tween(500)), exit: FadeOut(Tween(500)),
                        boundsTransform: (_, _) => Tween(500), resizeMode: ResizeMode.RemeasureToBounds)
                    .Background(color)
                    .TestTag($"shared-sample.detail.{index}"),
                content: () => Text(
                    $"#{index}",
                    color: Color.White,
                    fontSize: 128.Sp(),
                    modifier: Modifier.Padding(all: 32.Dp()).TestTag($"shared-sample.text.detail.{index}")
                        .SharedBounds(s.RememberSharedContentState($"text {index}"), a,
                            enter: FadeIn(Tween(500)), exit: FadeOut(Tween(500)),
                            boundsTransform: (_, _) => Tween(500))
                )
            );
        }
    }
}
