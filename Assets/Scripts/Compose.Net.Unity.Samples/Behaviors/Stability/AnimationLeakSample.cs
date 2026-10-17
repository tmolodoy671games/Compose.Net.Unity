// ReSharper disable ArrangeNamespaceBody

using Compose.Net;
using SharpExtensions;

namespace Compose.Net.Unity.Samples.Behaviors.Stability
{
    [DisallowMultipleComponent]
    internal partial class AnimationLeakSample : ComposeUI
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
                modifier: Modifier
                    .FillMaxSize(),
                content: () =>
                {
                    var showMovingSquare = Remember(() => MutableStateOf(true));
                    if (showMovingSquare.Value)
                    {
                        var isSwitched = Remember(() => MutableStateOf(false));
                        var offset = AnimateFloatAsState(
                            targetValue: isSwitched.Value ? 100 : -100,
                            animationSpec: Tween(duration: 3)
                        ).Value;
                        Box(() =>
                            Box(() =>
                                Spacer(
                                    Modifier
                                        .Size(100.Dp())
                                        .Background(Color.GreenColor)
                                        .Offset(offset.Dp())
                                        .Clickable(() => isSwitched.Value = !isSwitched.Value)
                                )
                            )
                        );
                    }

                    var interactionSource = Remember(MutableInteractionSource);
                    var hovered = interactionSource.CollectIsHoveredAsState().Value;
                    Text(
                        text: "Switch",
                        color: Color.White,
                        fontSize: 32.Sp(),
                        modifier: Modifier
                            .Padding(top: 32.Dp())
                            .Background(Color.BlueColor)
                            .Padding(
                                horizontal: 32.Dp() + 32 * AnimateFloatAsState(hovered.ToInt()).Value.Dp(),
                                vertical: 16.Dp()
                            )
                            .Clip(RoundedCornerShape(16.Dp()))
                            .Clickable(() => showMovingSquare.Value = !showMovingSquare.Value)
                            .Hoverable(interactionSource)
                    );
                }
            );
        }
    }
}