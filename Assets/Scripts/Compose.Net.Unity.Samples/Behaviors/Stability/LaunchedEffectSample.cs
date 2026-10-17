using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Compose.Net;

// ReSharper disable ArrangeNamespaceBody

namespace Compose.Net.Unity.Samples.Behaviors.Stability
{
    internal partial class LaunchedEffectSample : ComposeUI
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
        public static void LaunchedEffect<TKey>(
            TKey key,
            Func<CancellationToken, Task> coroutine
        )
        {
            // BRUH
            // var _ = Remember(key, () =>
            // {
            //     var tokenSource = new CancellationTokenSource();
            //     var token = tokenSource.Token;
            //     coroutine(token);
            //     return new CustomComposeDisposable(() =>
            //     {
            //         tokenSource.Cancel();
            //         tokenSource.Dispose();
            //     });
            // });
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
                    var count = Remember(() => MutableStateOf(0));
                    Text(
                        text: count.Value.ToString(),
                        color: Color.White,
                        fontSize: 40.Sp(),
                        modifier: Modifier
                            .TestTag("test-label")
                            .Background(Color.RedColor)
                            .Padding(all: 10.Dp())
                    );
                    var isEffectRunning = Remember(() => MutableStateOf(false));
                    if (isEffectRunning.Value)
                    {
                        LaunchedEffect(
                            key: "",
                            coroutine: async it =>
                            {
                                while (true)
                                {
                                    await Task.Delay(1_000, it);
                                    count.Value++;
                                }
                            }
                        );
                    }

                    var onOrOff = isEffectRunning.Value ? "On" : "Off";
                    var interactionSource = Remember(MutableInteractionSource);
                    var hovered = interactionSource.CollectIsHoveredAsState().Value;
                    Text(
                        text: $"Launched Effect is {onOrOff}",
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