// ReSharper disable ArrangeNamespaceBody

using Compose.Net;

namespace UnityCompose.Samples.Behaviors.Content
{
    internal partial class SharedElementSample : ComposeUI
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
            var expanded = Remember(() => MutableStateOf(false));
            var tween = Tween(500);
            SharedTransitionLayout(
                modifier: Modifier.FillMaxSize(),
                content: sharedScope =>
                {
                    AnimatedContent(
                        targetState: expanded.Value,
                        transitionSpec: _ => FadeIn(tween).TogetherWith(FadeOut(tween)),
                        contentAlignment: Alignment.Center,
                        modifier: Modifier.FillMaxSize(),
                        content: (animatedScope, it) =>
                        {
                            var cardModifier = Modifier.SharedBounds(
                                sharedContentState: sharedScope.RememberSharedContentState("card"),
                                animatedVisibilityScope: animatedScope,
                                enter: IEnterTransition.None,
                                exit: IExitTransition.None,
                                boundsTransform: (_, _) => tween
                            );
                            if (it)
                            {
                                Box(
                                    contentAlignment: Alignment.Center,
                                    modifier: cardModifier
                                        .FillMaxSize()
                                        .Background(Color.DodgerBlue)
                                        .Clickable(() => expanded.Value = false),
                                    content: () => Text("Экран", color: Color.White, fontSize: 96.Sp())
                                );
                            }
                            else
                            {
                                Box(
                                    contentAlignment: Alignment.Center,
                                    modifier: cardModifier
                                        .Size(400.Dp(), 120.Dp())
                                        .Clip(RoundedCornerShape(24.Dp()))
                                        .Background(Color.DodgerBlue)
                                        .Clickable(() => expanded.Value = true),
                                    content: () => Text("Открыть", color: Color.White, fontSize: 48.Sp())
                                );
                            }
                        }
                    );
                }
            );
        }
    }
}
