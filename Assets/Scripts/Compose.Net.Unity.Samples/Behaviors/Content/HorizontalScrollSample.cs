// ReSharper disable ArrangeNamespaceBody

using Compose.Net;

namespace Compose.Net.Unity.Samples.Behaviors.Content
{
    internal partial class HorizontalScrollSample : ComposeUI
    {
        [Composable]
        protected override void Content() => Layout();

        [Composable]
        protected override void Preview() => Layout();

        [Composable]
        private static void Layout()
        {
            Box(
                contentAlignment: Alignment.Center,
                modifier: Modifier.FillMaxSize(),
                content: () =>
                {
                    var scrollState = RememberScrollState();
                    Row(
                        modifier: Modifier
                            .Width(700.Dp())
                            // .Height(700.Dp())
                            .Background(Color.White)
                            .HorizontalScroll(scrollState),
                        content: () =>
                        {
                            for (var i = 0; i < 100; i++)
                            {
                                Text($"Bla {i}", fontSize: 64.Sp());
                            }
                        }
                    );
                }
            );
        }
    }
}