// ReSharper disable ArrangeNamespaceBody

using Compose.Net;

namespace Compose.Net.Unity.Samples.Behaviors.Stability
{
    internal partial class ContentSwapSample : ComposeUI
    {
        [Composable]
        protected override void Content() => Layout();

        [Composable]
        protected override void Preview() => Layout();

        [Composable]
        private static void Layout()
        {
            Column(
                horizontalAlignment: Alignment.CenterHorizontally,
                verticalArrangement: Arrangement.Center,
                modifier: Modifier.FillMaxSize(),
                content: () =>
                {
                    var isSwitched = Remember(() => MutableStateOf(false));
                    if (!isSwitched.Value)
                        Content2();
                    else
                        Content1();

                    Text(
                        text: "Switch",
                        color: Color.White,
                        fontSize: 62.Sp(),
                        modifier: Modifier
                            .Padding(top: 16.Dp())
                            .Clip(RoundedCornerShape(16.Dp()))
                            .Background(Color.BlueColor)
                            .Padding(horizontal: 20.Dp(), vertical: 12.Dp())
                            .Clickable(() => isSwitched.Value = !isSwitched.Value)
                    );
                }
            );
        }

        [Composable]
        private static void Content1()
        {
            Spacer(
                Modifier
                    .Size(100.Dp())
                    .Background(Color.GreenColor)
            );
        }

        [Composable]
        private static void Content2()
        {
            Row(() =>
            {
                Spacer(
                    Modifier
                        .Size(100.Dp())
                        .Background(Color.RedColor)
                );
                Spacer(
                    Modifier
                        .Size(100.Dp())
                        .Background(Color.RedColor)
                );
            });
        }
    }
}