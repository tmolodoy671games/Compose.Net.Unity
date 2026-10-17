// ReSharper disable ArrangeNamespaceBody

using Compose.Net;

namespace Compose.Net.Unity.Samples.Behaviors.Stability
{
    internal partial class DisposableEffectSample : ComposeUI
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
            Column(
                horizontalAlignment: Alignment.CenterHorizontally,
                verticalArrangement: Arrangement.Center,
                modifier: Modifier
                    .TestTag("launched-effect-disposal")
                    .FillMaxSize(),
                content: () =>
                {
                    var isEffectRunning = Remember(() => MutableStateOf(false));
                    if (isEffectRunning.Value)
                    {
                        DisposableEffect(
                            string.Empty,
                            () =>
                            {
                                Debug.Log("DisposableEffect()");
                                return OnDispose(() => Debug.Log("OnDispose()"));
                            }
                        );
                    }

                    var onOrOff = isEffectRunning.Value ? "On" : "Off";
                    var interactionSource = Remember(MutableInteractionSource);
                    var hovered = interactionSource.CollectIsHoveredAsState().Value;
                    Text(
                        text: $"DisposableEffect is {onOrOff}",
                        color: Color.White,
                        fontSize: 40.Sp(),
                        modifier: Modifier
                            .TestTag("test-button")
                            .Padding(top: 32.Dp())
                            .Background(hovered ? Color.Cyan : Color.BlueColor)
                            .Padding(vertical: 20.Dp())
                            .Padding(horizontal: hovered ? 40.Dp() : 20.Dp())
                            .Clip(RoundedCornerShape(16.Dp()))
                            .Hoverable(interactionSource)
                            .Clickable(() => isEffectRunning.Value = !isEffectRunning.Value)
                    );
                }
            );
        }
    }
}