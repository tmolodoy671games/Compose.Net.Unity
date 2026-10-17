// ReSharper disable ArrangeNamespaceBody

using Compose.Net;

namespace Compose.Net.Unity.Samples.Behaviors.Stability
{
    internal partial class ComposeUpdatePerformanceTest : ComposeUI
    {
        [Composable]
        protected override void Content()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            var parentSize = Remember(() => MutableStateOf(new Size()));
            Box(
                modifier: Modifier
                    .FillMaxSize()
                    .OnGloballyPositioned(it => parentSize.Value = it.Size),
                content: () =>
                {
                    if (float.IsNaN(parentSize.Value.Width) || parentSize.Value.Width <= 0)
                        return;
                    for (var i = 0; i < 1_000; i++)
                    {
                        var currentI = i;
                        Key(
                            key: currentI,
                            content: () => Item(currentI, parentSize.Value)
                        );
                    }
                }
            );
        }

        [BoxScope]
        [Composable]
        private static void Item(int currentI, Size parentSize)
        {
            var position = Remember(static () => MutableStateOf(new Offset()));

            LaunchedEffect(
                key: 1,
                coroutine: () => PerformanceUtils.MoveRandomlyCoroutine(
                    parentSize: () => parentSize,
                    it => position.Value = it
                )
            );
            // LaunchedEffect(
            //     currentI,
            //     token => PerformanceUtils.MoveRandomlyCoroutine(
            //         parentSize: parentSize,
            //         offset => position.Value = offset,
            //         token
            //     )
            // );

            var baseModifier = Remember(currentI, () => Modifier
                .Size(50.Dp())
                .Background(
                    PerformanceUtils.GetColor(currentI)
                )
                .Float()
            );
            Spacer(
                modifier:
                baseModifier
                    .Offset(
                        x: position.Value.X.Dp(),
                        y: position.Value.Y.Dp()
                    )
            );
        }
    }
}