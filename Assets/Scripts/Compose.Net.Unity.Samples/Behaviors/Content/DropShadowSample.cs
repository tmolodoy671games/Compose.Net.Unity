using Compose.Net;
using SharpExtensions;

// ReSharper disable ArrangeNamespaceBody

namespace Compose.Net.Unity.Samples.Behaviors.Content
{
    internal partial class DropShadowSample : ComposeUI
    {
        [Composable]
        protected override void Content() => Layout();

        [Composable]
        protected override void Preview() => Layout();

        [Composable]
        private static void Layout()
        {
            Column(
                verticalArrangement: Arrangement.Center,
                horizontalAlignment: Alignment.CenterHorizontally,
                modifier: Modifier
                    .FillMaxSize()
                    .Background(Color.White),
                content: () =>
                {
                    var hasShadow = Remember(() => MutableStateOf(true));
                    var tween = Tween();
                    Spacer(
                        Modifier
                            .Size(100.Dp())
                            .Background(Color.Gray)
                            .DropShadow(
                                shape: RoundedCornerShape(0.Dp()),
                                shadow: Shadow(
                                    radius: AnimateFloatAsState(32 * hasShadow.Value.ToInt(), tween).Value.Dp(),
                                    spread: 10.Dp(),
                                    color: AnimateColorAsState(hasShadow.Value ? Color.Black : new Color(), tween).Value
                                    ,
                                    offset: new Offset()
                                )
                            )
                    );
                    Text(
                        text: "Switch",
                        fontSize: 32.Sp(),
                        color: Color.White,
                        modifier: Modifier
                            .Padding(top: 16.Dp())
                            .Padding(top: 16.Dp())
                            .Padding(top: 16.Dp())
                            .Padding(top: 16.Dp())
                            .Background(Color.RoyalBlue)
                            .Padding(16.Dp())
                            .Padding(top: 16.Dp())
                            .Padding(top: 16.Dp())
                            .Padding(top: 16.Dp())
                            .Padding(top: 16.Dp())
                            .Clip(RoundedCornerShape(16.Dp()))
                            .Clickable(() => hasShadow.Value = !hasShadow.Value)
                    );
                }
            );
        }
    }
}