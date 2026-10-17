using System.Threading;
using Compose.Net;

// ReSharper disable ArrangeNamespaceBody

namespace UnityCompose.Samples.Behaviors.Content
{
    internal partial class LazyColumnSample : ComposeUI
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
                    var coroutineScope = RememberCoroutineScope();
                    var state = RememberLazyListState();
                    LazyColumn(
                        state: state,
                        modifier: Modifier
                            .Height(700.Dp())
                            .Background(Color.White),
                        content: scope =>
                        {
                            // Scroll
                            scope.Items(100, it =>
                            {
                                Box(
                                    modifier: Modifier
                                        .Clickable(() => state.AnimateScrollToItemVisibility(it))
                                        .FillMaxWidth(),
                                    content: () =>
                                    {
                                        Text(
                                            $"Bla {it}",
                                            fontSize: 64.Sp()
                                        );
                                    }
                                );
                            });
                        }
                    );
                }
            );
        }
    }
}