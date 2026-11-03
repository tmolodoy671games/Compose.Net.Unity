using System;
using Compose.Net;
using static Compose.Net.ComposeFunctions;

namespace Compose.Net.Unity.Samples.Behaviors.Content;

public partial class LazyListDemo : ComposeUI
{
    [Composable] protected override void Content() => Screen(Remember(() => new LazyListDemoState()));
    [Composable] protected override void Preview() => Screen(Remember(() => new LazyListDemoState()));
    [Composable] public static void Screen(LazyListDemoState demo)
    {
        Column(modifier: Modifier.FillMaxSize().Background(new Color(.035f, .05f, .075f))
            .VerticalScroll(RememberScrollState()).Padding(18.Dp()).TestTag("lazy-demo"), content: () =>
        {
            Text("Lazy list lab", color: Color.White, fontSize: 32.Sp());
            Text("18 live examples · only visible items are composed · wheel, trackpad and touch", color: Color.LightGray, fontSize: 15.Sp());
            Text("Try Jump, Prepend and Resize. Reverse changes item order. Item taps keep their local state while visible.", color: Color.LightGray, fontSize: 13.Sp());
            for (var row = 0; row < 6; row++)
            {
                var first = row * 3;
                Row(content: () => { Card(demo.Cases[first]); Card(demo.Cases[first + 1]); Card(demo.Cases[first + 2]); });
            }
        });
    }
    [Composable] public static void Card(LazyListDemoCase item)
    {
        var state = item.State.Value; var coroutine = RememberCoroutineScope(); var tag = "lazy." + item.Id;
        Column(modifier: Modifier.Size(360.Dp(), 440.Dp()).Padding(6.Dp())
            .Background(new Color(.075f, .105f, .16f), RoundedCornerShape(10.Dp())).Padding(10.Dp()), content: () =>
        {
            Text(item.Title, color: Color.White, fontSize: 16.Sp(), modifier: Modifier.Height(26.Dp()));
            Status(item, state, tag);
            Box(modifier: Modifier.Size(326.Dp(), 166.Dp()).Background(new Color(.02f, .03f, .05f)), content: () => List(item, state, tag));
            Row(content: () =>
            {
                Button(tag + ".start", "Start", () => state.ScrollToItem(0));
                Button(tag + ".end", "End", () => state.ScrollToItem(Math.Max(0, item.Count.Value - 1)));
                Button(tag + ".jump", "Jump", () => state.ScrollToItem(Math.Max(0, item.Count.Value / 2), 11));
                Button(tag + ".animate", "Animate", () => coroutine.Launch(async _ =>
                {
                    try { await state.AnimateScrollToItem(state.FirstVisibleItemIndex + 4, animationSpec: Tween(900)); }
                    catch (OperationCanceledException) { }
                }), 90);
            });
            Row(content: () =>
            {
                Button(tag + ".prepend", "Prepend 3", () => { item.Prefix.Value += 3; item.Count.Value += 3; }, 100);
                Button(tag + ".enabled", item.Enabled.Value ? "Input: on" : "Input: off", () => item.Enabled.Value = !item.Enabled.Value, 100);
                Button(tag + ".reverse", "Reverse", () => item.Reverse.Value = !item.Reverse.Value, 90);
            });
            Row(content: () =>
            {
                Button(tag + ".count", "Empty / fill", () => item.Count.Value = item.Count.Value == 0 ? 1000 : 0, 100);
                Button(tag + ".resize", "Viewport", () => item.Viewport.Value = item.Viewport.Value == 150 ? 90 : 150, 100);
                Button(tag + ".size", "Item size", () => item.ItemSize.Value = item.ItemSize.Value == 54 ? 78 : 54, 90);
            });
            Row(content: () =>
            {
                Button(tag + ".replace", "New state", () => item.State.Value = new LazyListState(10, 5), 100);
                Button(tag + ".remove", item.Present.Value ? "Remove" : "Restore", () => item.Present.Value = !item.Present.Value, 100);
                Button(tag + ".physics", item.Physics.Value ? "Fling + bounce" : "Clamped", () => item.Physics.Value = !item.Physics.Value, 110);
            });
        });
    }
    [Composable] private static void Status(LazyListDemoCase item, ILazyListState state, string tag)
    {
        Text($"first {state.FirstVisibleItemIndex} + {state.FirstVisibleItemScrollOffset:0}px · visible {state.LayoutInfo.VisibleItemsInfo.Count}",
            color: Color.LightGray, fontSize: 12.Sp(), modifier: Modifier.TestTag(tag + ".status"));
        Text($"active {item.Active.Value} / {state.LayoutInfo.TotalItemsCount} · created {item.Created.Value} · taps {item.Clicks.Value}",
            color: new Color(.35f, .8f, .9f), fontSize: 11.Sp(), modifier: Modifier.TestTag(tag + ".lifetime"));
        Text($"← {state.CanScrollBackward}  → {state.CanScrollForward}  moving {state.IsScrollInProgress}", color: Color.LightGray, fontSize: 11.Sp());
    }
    [Composable] private static void List(LazyListDemoCase item, ILazyListState state, string tag)
    {
        if (!item.Present.Value) return;
        var count = item.Count.Value; var prefix = item.Prefix.Value; var size = item.ItemSize.Value;
        var modifier = Modifier.Size(310.Dp(), item.Viewport.Value.Dp()).TestTag(tag + ".viewport");
        var padded = item.Id.StartsWith("padding"); var sticky = item.Id.StartsWith("sticky");
        Action<ILazyListScope> content = scope =>
        {
            if (sticky)
            {
                var prefixCount = Math.Min(prefix, count);
                scope.Items(prefixCount, i => Cell(item, i - prefixCount, size, tag, false), i => i - prefixCount, _ => "cell");
                var remaining = count - prefixCount;
                for (var section = 0; section < (remaining + 9) / 10; section++)
                {
                    var part = section;
                    scope.StickyHeader(index => Cell(item, part * 10, size, tag, true), "header-" + part, "header");
                    scope.Items(Math.Min(9, remaining - part * 10 - 1), index => Cell(item, part * 10 + index + 1, size, tag, false), index => part * 10 + index + 1, _ => "cell");
                }
            }
            else scope.Items(count, index => Cell(item, index - prefix, size, tag, false), index => index - prefix, index => index % 2);
        };
        if (item.Horizontal) LazyRow(content, state, modifier, reverseLayout: item.Reverse.Value, userScrollEnabled: item.Enabled.Value,
            flingBehavior: item.Physics.Value ? null : new FlingBehavior(0), overscrollEffect: item.Physics.Value ? OverscrollEffect.Default : null,
            contentPadding: padded ? new PaddingValues(12.Dp()) : default,
            horizontalArrangement: padded ? Arrangement.SpacedBy(8.Dp()) : item.Id == "short" ? Arrangement.Center : null);
        else LazyColumn(content, state, modifier, reverseLayout: item.Reverse.Value, userScrollEnabled: item.Enabled.Value,
            flingBehavior: item.Physics.Value ? null : new FlingBehavior(0), overscrollEffect: item.Physics.Value ? OverscrollEffect.Default : null,
            contentPadding: padded ? new PaddingValues(12.Dp()) : default,
            verticalArrangement: padded ? Arrangement.SpacedBy(8.Dp()) : null);
    }
    [Composable] private static void Cell(LazyListDemoCase item, int id, int baseSize, string tag, bool header)
    {
        _ = Remember(() => new Lifetime(item));
        var taps = Remember(() => MutableStateOf(0));
        var size = baseSize + (item.Id.StartsWith("variable") ? Math.Abs(id % 4) * 17 : 0);
        var nested = item.Id == "nested";
        var modifier = Modifier.Size((item.Horizontal ? size + 36 : 280).Dp(), (item.Horizontal ? 110 : nested ? 112 : size).Dp())
            .Background(header ? new Color(.55f, .32f, .1f) : id % 2 == 0 ? new Color(.08f, .31f, .43f) : new Color(.13f, .23f, .37f))
            .TestTag(tag + ".item-" + id).Clickable(() => { taps.Value++; item.Clicks.Value++; });
        if (item.Id == "short") modifier = modifier.FillParentMaxWidth(.5f);
        Box(modifier: modifier, content: () =>
        {
            if (nested)
                Column(content: () =>
                {
                    Text("Row " + id, color: Color.White);
                    LazyRow(modifier: Modifier.Size(270.Dp(), 74.Dp()), content: scope => scope.Items(100,
                        i => Box(modifier: Modifier.Size(82.Dp(), 60.Dp()).Background(new Color(.3f, .2f, .45f)),
                            content: () => Text($"{id}/{i}", color: Color.White)), key: i => i));
                });
            else Text((header ? "Section " : "Item ") + id + $" · {taps.Value}", color: Color.White, fontSize: 14.Sp());
        });
    }
    private sealed class Lifetime(LazyListDemoCase item) : IRememberObserver
    {
        public void OnRemembered() { item.Active.Value++; item.Created.Value++; }
        public void OnForgotten() => item.Active.Value--;
    }
    [Composable] private static void Button(string tag, string title, Action action, int width = 66) =>
        Text(title, color: Color.White, fontSize: 12.Sp(), modifier: Modifier.Size(width.Dp(), 28.Dp())
            .Background(new Color(.17f, .24f, .34f), RoundedCornerShape(4.Dp())).TestTag(tag).Clickable(action));
}
