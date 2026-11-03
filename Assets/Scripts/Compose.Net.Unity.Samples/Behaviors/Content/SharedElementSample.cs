// ReSharper disable ArrangeNamespaceBody

using Compose.Net;

namespace Compose.Net.Unity.Samples.Behaviors.Content
{
    public partial class SharedElementSample : ComposeUI
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
        private static void Layout() => Screen(Remember(() => MutableStateOf(false)));

        [Composable]
        public static void Screen(IMutableState<bool> expanded)
        {
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
                            // Overlay content needs its own fade. Remeasure both cards as their aspect ratio changes.
                            var cardModifier = Modifier
                                .TestTag(it ? "shared-element.expanded" : "shared-element.collapsed")
                                .SharedBounds(
                                    sharedContentState: sharedScope.RememberSharedContentState("card"),
                                    animatedVisibilityScope: animatedScope,
                                    enter: FadeIn(tween),
                                    exit: FadeOut(tween),
                                    boundsTransform: (_, _) => tween,
                                    resizeMode: ResizeMode.RemeasureToBounds
                                );
                            if (it)
                            {
                                Box(
                                    contentAlignment: Alignment.Center,
                                    modifier: cardModifier
                                        .Clickable(() => expanded.Value = false)
                                        .FillMaxSize()
                                        .Background(Color.DodgerBlue),
                                    content: () => Text("Экран", color: Color.White, fontSize: 96.Sp())
                                );
                            }
                            else
                            {
                                Box(
                                    contentAlignment: Alignment.Center,
                                    modifier: cardModifier
                                        .Clickable(() => expanded.Value = true)
                                        .Size(400.Dp(), 120.Dp())
                                        .Clip(RoundedCornerShape(24.Dp()))
                                        .Background(Color.DodgerBlue),
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
