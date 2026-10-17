// ReSharper disable ArrangeNamespaceBody

using Compose.Net;
using global::Unity.Scripting.LifecycleManagement;

namespace Compose.Net.Unity.Samples.Behaviors.Stability
{
    [NoAutoStaticsCleanup]
    internal partial class CompositionLocalSample2 : ComposeUI
    {
        private static readonly ICompositionLocal<string> LocalDebugString = CompositionLocalOf(() => "Default");

        [Composable]
        protected override void Content() => Layout();

        [Composable]
        protected override void Preview() => Layout();

        [Composable]
        private static void Layout()
        {
            CompositionLocalProvider(
                LocalTextStyle.Provides(
                    new TextStyle(
                        FontSize: 80.Sp(),
                        Color: Color.White
                    )
                ),
                () =>
                {
                    Column(
                        horizontalAlignment: Alignment.CenterHorizontally,
                        verticalArrangement: Arrangement.Center,
                        modifier: Modifier.FillMaxSize(),
                        content: () =>
                        {
                            Text(LocalDebugString.Current);
                            CompositionLocalProvider(
                                LocalDebugString.Provides("Nested"),
                                () =>
                                {
                                    Text(LocalDebugString.Current);
                                    CompositionLocalProvider(
                                        LocalDebugString.Provides("Super Nested"),
                                        () =>
                                        {
                                            Text(LocalDebugString.Current);
                                            Text(LocalDebugString.Current);
                                        }
                                    );
                                    Text(LocalDebugString.Current);
                                }
                            );
                            Text(LocalDebugString.Current);
                        }
                    );
                }
            );
        }
    }
}