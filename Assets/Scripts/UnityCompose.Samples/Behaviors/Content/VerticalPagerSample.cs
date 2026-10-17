// ReSharper disable ArrangeNamespaceBody

using System;
using Compose.Net;

namespace UnityCompose.Samples.Behaviors.Content
{
    internal partial class VerticalPagerSample : ComposeUI
    {
        [Composable]
        protected override void Content() => Layout();

        [Composable]
        protected override void Preview() => Layout();

        [Composable]
        private static void Layout()
        {
            Row(
                modifier: Modifier.FillMaxSize(),
                content: () =>
                {
                    const int pageCount = 5;
                    var pagerState = RememberPagerState(() => pageCount);
                    var coroutineScope = RememberCoroutineScope();
                    VerticalPager(
                        state: pagerState,
                        // pageSize: new PageSize.Fixed(400.Dp()),
                        contentPadding: PaddingValues(64.Dp()),
                        pageSpacing: 32.Dp(),
                        modifier: Modifier
                            .FillMaxHeight()
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
                    Column(
                        verticalArrangement: Arrangement.Center,
                        modifier: Modifier
                            .Padding(horizontal: 8.Dp())
                            .FillMaxHeight(),
                        content: () =>
                        {
                            for (var i = 0; i < pageCount; i++)
                            {
                                var page = i;
                                Box(
                                    modifier: Modifier
                                        .Padding(vertical: 8.Dp())
                                        .Background(Color.CadetBlue)
                                        .Padding(horizontal: 16.Dp(), vertical: 8.Dp())
                                        .Clip(RoundedCornerShape(4.Dp()))
                                        .Clickable(() =>
                                            {
                                                coroutineScope.Launch(async _ =>
                                                {
                                                    await pagerState.AnimateScrollToPage(
                                                        page,
                                                        animationSpec: Tween(1_000)
                                                    );
                                                });
                                            }
                                        ),
                                    content: () =>
                                    {
                                        Text(
                                            text: page.ToString(),
                                            fontSize: 32.Sp(),
                                            color: Color.White
                                        );
                                    }
                                );
                            }
                        }
                    );
                }
            );
        }
    }
}