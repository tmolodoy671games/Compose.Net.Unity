// ReSharper disable ArrangeNamespaceBody

using Compose.Net;

namespace Compose.Net.Unity.Samples.Behaviors.Stability
{
    internal partial class StateLeakSample : ComposeUI
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
            CompositionLocalProvider(
                LocalTextStyle.Provides(
                    new TextStyle(
                        FontSize: 40.Sp(),
                        Color: Color.Black
                    )
                ),
                content: () =>
                {
                    Box(
                        contentAlignment: Alignment.Center,
                        modifier: Modifier
                            .FillMaxSize(),
                        content: () =>
                        {
                            Column(
                                modifier: Modifier,
                                content: () =>
                                {
                                    var showFirst = Remember(() => MutableStateOf(false));
                                    if (showFirst.Value)
                                    {
                                        var firstCount = Remember(() => MutableStateOf(0));
                                        Text(
                                            text: $"Clicked {firstCount.Value} times",
                                            textAlign: TextAlign.MiddleCenter,
                                            modifier: Modifier
                                                .FillMaxWidth()
                                                .Background(Color.RedColor)
                                                .Padding(all: 20.Dp())
                                                .Clip(RoundedCornerShape(16.Dp()))
                                                .Clickable(() => firstCount.Value++)
                                                .TestTag("first-button")
                                        );
                                    }

                                    var secondCount = Remember(() => MutableStateOf(0));
                                    Text(
                                        text: $"Clicked {secondCount.Value} times",
                                        textAlign: TextAlign.MiddleCenter,
                                        modifier: Modifier
                                            .Padding(top: 16.Dp())
                                            .FillMaxWidth()
                                            .Background(Color.GreenColor)
                                            .Padding(all: 20.Dp())
                                            .Clip(RoundedCornerShape(16.Dp()))
                                            .Clickable(() => secondCount.Value++)
                                            .TestTag("second-button")
                                    );
                                    Text(
                                        text: "Switch",
                                        textAlign: TextAlign.MiddleCenter,
                                        modifier: Modifier
                                            .Padding(top: 16.Dp())
                                            .FillMaxWidth()
                                            .Background(Color.BlueColor)
                                            .Padding(all: 20.Dp())
                                            .Clip(RoundedCornerShape(16.Dp()))
                                            .Clickable(() => showFirst.Value = !showFirst.Value)
                                            .TestTag("switch-button")
                                    );
                                }
                            );
                        }
                    );
                }
            );
        }
    }
}