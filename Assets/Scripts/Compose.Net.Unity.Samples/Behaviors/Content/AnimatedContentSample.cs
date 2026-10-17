// ReSharper disable ArrangeNamespaceBody

using Compose.Net;

namespace Compose.Net.Unity.Samples.Behaviors.Content
{
    internal partial class AnimatedContentSample : ComposeUI
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
            Box(
                contentAlignment: Alignment.Center,
                modifier: Modifier
                    .FillMaxSize(),
                content: () =>
                {
                    var state = Remember(() => MutableStateOf(1L));
                    var tween = Tween(1_000);
                    AnimatedContent(
                        targetState: state.Value,
                        transitionSpec: _ => SlideInVertically(tween, it => it)
                            .TogetherWith(SlideOutVertically(tween, it => -it)),
                        modifier: Modifier
                            .Background(Color.LightBlue)
                            .Padding(horizontal: 16.Dp(), vertical: 8.Dp())
                            .Clip(RoundedCornerShape(8.Dp()))
                            .AnimateContentSize(tween)
                            .Clickable(() => state.Value *= 10),
                        content: it => Text(
                            text: it.ToString(),
                            fontSize: 128.Sp(),
                            color: Color.White
                        )
                    );
                }
            );
        }
    }
}