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
            using (
                Box(
                    contentAlignment: Alignment.Center,
                    modifier: Modifier
                        .FillMaxSize()
                )
            )
            {
                {
                    var isSwitched = Remember(() => MutableStateOf(true));
                    using (
                        Box(
                            contentAlignment: Alignment.Center,
                            modifier: Modifier
                                .Clickable(() => isSwitched.Value = !isSwitched.Value)
                                .Background(Color.LightCoral)
                                .Clip(RoundedCornerShape(32.Dp()))
                                .AnimateContentSize(Tween(3_000), alignment: Alignment.Center)
                        )
                    )
                    {
                        Text(
                            text: isSwitched.Value
                                ? "Short"
                                : "Looooooooooooooooooooong\nLooooooooooooooooooooong\nLooooooooooooooooooooong",
                            fontSize: 64.Sp(),
                            color: Color.White,
                            modifier: Modifier
                                .Padding(16.Dp())
                        );
                    }
                }
            }
        }
    }
}