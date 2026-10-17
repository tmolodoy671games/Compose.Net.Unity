using Compose.Net;
using global::Unity.Scripting.LifecycleManagement;

// ReSharper disable ArrangeNamespaceBody

namespace Compose.Net.Unity.Samples.Behaviors.Stability
{
    [NoAutoStaticsCleanup]
    internal partial class CompositionLocalSample : ComposeUI
    {
        private static readonly ICompositionLocal<bool> LocalIsSwitched = CompositionLocalOf(() => false);

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
            Box(
                contentAlignment: Alignment.Center,
                modifier: Modifier
                    .TestTag("composition-local-sample")
                    .FillMaxSize(),
                content: () =>
                {
                    var isSwitched = Remember(() => MutableStateOf(false));

                    CompositionLocalProvider(
                        LocalIsSwitched.Provides(isSwitched.Value),
                        content: SampleReader
                    );

                    Text(
                        text: "Switch",
                        color: Color.White,
                        fontSize: 32.Sp(),
                        modifier: Modifier
                            .Padding(top: 80.Dp())
                            .Background(Color.BlueColor)
                            .Padding(all: 32.Dp())
                            .Clip(RoundedCornerShape(16.Dp()))
                            .Clickable(() => isSwitched.Value = !isSwitched.Value)
                    );
                }
            );
        }

        [Composable]
        private static void SampleReader()
        {
            Box(() =>
            {
                Box(() =>
                {
                    Spacer(
                        modifier: Modifier
                            .Background(
                                AnimateColorAsState(
                                    LocalIsSwitched.Current ? Color.GreenColor : Color.RedColor
                                ).Value
                            )
                            .Padding(all: 100.Dp())
                    );
                });
            });
        }
    }
}