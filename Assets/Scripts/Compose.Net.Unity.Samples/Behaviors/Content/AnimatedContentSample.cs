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
            using (
                Box(
                    contentAlignment: Alignment.Center,
                    modifier: Modifier
                        .FillMaxSize()
                )
            )
            {
                var state = Remember(() => MutableStateOf(1L));
                var tween = Tween(1_000);
                using (
                    var currentState = AnimatedContent(
                        targetState: state.Value,
                        transitionSpec: _ => SlideInVertically(tween, it => it)
                            .TogetherWith(SlideOutVertically(tween, it => -it)),
                        modifier: Modifier
                            .Clickable(() => state.Value *= 10)
                            .Background(Color.LightBlue)
                            .Padding(horizontal: 16.Dp(), vertical: 8.Dp())
                            .Clip(RoundedCornerShape(8.Dp()))
                    )
                )
                {
                    var (_, value) = currentState.Value;
                    Text(
                        text: value.ToString(),
                        fontSize: 128.Sp(),
                        color: Color.White
                    );
                }
            }
        }
    }
}