using Compose.Net;
using UnityEngine.UIElements;

// ReSharper disable ArrangeNamespaceBody

namespace Compose.Net.Unity.Samples.Behaviors.Content
{
    public partial class AnimateContentSizeSample : ComposeUI
    {
        [Composable]
        protected override void Content() => Layout();

        [Composable]
        protected override void Preview() => Layout();

        [Composable]
        private static void Layout()
        {
            Box(
                contentAlignment: Alignment.Center,
                modifier: Modifier
                    .FillMaxSize(),
                content: () =>
                {
                    var isSwitched = Remember(() => MutableStateOf(true));
                    Box(
                        contentAlignment: Alignment.Center,
                        modifier: Modifier
                            .Clip(RoundedCornerShape(32.Dp()))
                            .Padding(16.Dp())
                            .Background(Color.LightCoral)
                            .AnimateContentSize(Tween(3_000))
                            .Clickable(
                                () => isSwitched.Value = !isSwitched.Value
                            )
                        ,
                        content: () => Text(
                            text: isSwitched.Value
                                ? "Short"
                                : "Looooooooooooooooooooong\nLooooooooooooooooooooong\nLooooooooooooooooooooong",
                            fontSize: 64.Sp(),
                            color: Color.White
                        )
                    );
                }
            );
        }
    }
}