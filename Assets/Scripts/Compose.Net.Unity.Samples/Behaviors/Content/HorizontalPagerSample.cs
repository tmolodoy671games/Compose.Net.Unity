// ReSharper disable ArrangeNamespaceBody

using System;
using Compose.Net;

namespace Compose.Net.Unity.Samples.Behaviors.Content
{
    internal partial class HorizontalPagerSample : ComposeUI
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
                modifier: Modifier.FillMaxSize(),
                content: () =>
                {
                    const int pageCount = 5;
                    var pagerState = RememberPagerState(() => pageCount);
                    var coroutineScope = RememberCoroutineScope();
                    HorizontalPager(
                        state: pagerState,
                        // pageSize: new PageSize.Fixed(400.Dp()),
                        contentPadding: PaddingValues(64.Dp()),
                        pageSpacing: 32.Dp(),
                        modifier: Modifier
                            .FillMaxWidth()
                            .Weight(1f),
                        pageContent: it =>
                        {
                            Spacer(
                                Modifier
                                    .FillMaxSize()
                                    .Background(
                                        it switch
                                        {
                                            0 => Color.RedColor,
                                            1 => Color.GreenColor,
                                            2 => Color.BlueColor,
                                            3 => Color.Yellow,
                                            4 => Color.Cyan,
                                            5 => Color.Purple,
                                            _ => throw new ArgumentOutOfRangeException(nameof(it), it, null)
                                        }
                                    )
                            );
                        }
                    );
                    Row(
                        horizontalArrangement: Arrangement.Center,
                        modifier: Modifier
                            .Padding(vertical: 8.Dp())
                            .FillMaxWidth(),
                        content: () =>
                        {
                            for (var i = 0; i < pageCount; i++)
                            {
                                var page = i;
                                Text(
                                    text: page.ToString(),
                                    fontSize: 32.Sp(),
                                    color: Color.White,
                                    modifier: Modifier
                                        .Padding(horizontal: 8.Dp())
                                        .Background(Color.CadetBlue)
                                        .Padding(horizontal: 16.Dp(), vertical: 8.Dp())
                                        .Clip(RoundedCornerShape(4.Dp()))
                                        .Clickable(() =>
                                            {
                                                // pagerState.ScrollToPage(page);
                                                coroutineScope.Launch(async scope =>
                                                {
                                                    await pagerState.AnimateScrollToPage(
                                                        page,
                                                        animationSpec: Tween(1_000)
                                                    );
                                                });
                                            }
                                        )
                                );
                            }
                        }
                    );
                }
            );
        }
    }
}