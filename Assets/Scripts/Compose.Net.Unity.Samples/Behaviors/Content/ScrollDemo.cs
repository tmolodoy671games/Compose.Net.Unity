using System;
using System.Threading.Tasks;
using Compose.Net;
using static Compose.Net.ComposeFunctions;

namespace Compose.Net.Unity.Samples.Behaviors.Content
{

public partial class ScrollDemo : ComposeUI
{
    [Composable] protected override void Content() => Screen(Remember(() => new ScrollDemoState()));
    [Composable] protected override void Preview() => Screen(Remember(() => new ScrollDemoState()));

    [Composable]
    public static void Screen(ScrollDemoState demo)
    {
        var page = RememberScrollState();
        Column(modifier: Modifier.FillMaxSize().Background(new Color(.035f, .05f, .075f))
            .VerticalScroll(page).Padding(18.Dp()).TestTag("scroll-demo"), content: () =>
        {
            Text("Scroll lab", color: Color.White, fontSize: 32.Sp());
            Text("18 live examples · wheel, trackpad, touch, nested scrolling and state control",
                color: Color.LightGray, fontSize: 15.Sp());
            Text("Positive offsets move toward MaxValue. Reverse keeps item order and starts at the far edge.",
                color: Color.LightGray, fontSize: 13.Sp());
            for (var row = 0; row < 6; row++)
            {
                var first = row * 3;
                Row(content: () => { Card(demo.Cases[first]); Card(demo.Cases[first + 1]); Card(demo.Cases[first + 2]); });
            }
        });
    }

    [Composable]
    public static void Card(ScrollDemoCase item)
    {
        var state = Remember(item.State.Value, () => item.State.Value);
        var scope = RememberCoroutineScope();
        var tag = "scroll." + item.Id;
        Column(modifier: Modifier.Width(360.Dp()).Height(390.Dp()).Padding(6.Dp())
            .Background(new Color(.075f, .105f, .16f), RoundedCornerShape(10.Dp())).Padding(10.Dp()), content: () =>
        {
            Text(item.Title, color: Color.White, fontSize: 16.Sp(), modifier: Modifier.Height(28.Dp()));
            Status(state, item.Clicks.Value, tag);
            Box(modifier: Modifier.Size(326.Dp(), 164.Dp()).Background(new Color(.02f, .03f, .05f)), content: () =>
            {
                var modifier = Modifier.Size(310.Dp(), item.Viewport.Value.Dp()).TestTag(tag + ".viewport");
                if (item.Present.Value)
                    modifier = item.Horizontal
                        ? modifier.HorizontalScroll(state, item.Id == "clamped" ? null : OverscrollEffect.Default,
                            item.Enabled.Value, reverseScrolling: item.Reverse.Value)
                        : modifier.VerticalScroll(state, enabled: item.Enabled.Value,
                            flingBehavior: item.Id == "no-fling" ? new FlingBehavior(0) : null,
                            reverseScrolling: item.Reverse.Value);
                if (item.Horizontal && item.Id != "column" || item.Id == "row")
                    Row(modifier: modifier, content: () => Items(item, tag));
                else Column(modifier: modifier, content: () => Items(item, tag));
            });
            Row(content: () =>
            {
                Button(tag + ".start", "Start", () => state.ScrollTo(0));
                Button(tag + ".end", "End", () => state.ScrollTo(state.MaxValue));
                Button(tag + ".by", "+60", () => state.ScrollBy(60));
                Button(tag + ".animate", "Animate", () => scope.Launch(async _ =>
                {
                    try { await state.AnimateScrollTo(state.Value < state.MaxValue / 2 ? state.MaxValue : 0, Tween(900)); }
                    catch (OperationCanceledException) { }
                }), 78);
            });
            Row(content: () =>
            {
                Button(tag + ".enabled", item.Enabled.Value ? "Input: on" : "Input: off", () => item.Enabled.Value = !item.Enabled.Value, 100);
                Button(tag + ".reverse", item.Reverse.Value ? "Reverse: on" : "Reverse: off", () => item.Reverse.Value = !item.Reverse.Value, 115);
                Button(tag + ".resize", "Resize", () => item.Viewport.Value = item.Viewport.Value == 140 ? 85 : 140, 75);
            });
            Row(content: () =>
            {
                Button(tag + ".count", $"Items: {item.Count.Value}", () => item.Count.Value = item.Count.Value == 10 ? 2 : item.Count.Value == 2 ? 0 : 10, 95);
                Button(tag + ".replace", "New state", () => item.State.Value = new ScrollState(40), 100);
                Button(tag + ".remove", item.Present.Value ? "Remove" : "Restore", () => item.Present.Value = !item.Present.Value, 90);
            });
        });
    }

    [Composable]
    private static void Status(IScrollState state, int clicks, string tag)
    {
        Text($"{state.Value:0.0} / {state.MaxValue:0.0} · viewport {state.ViewportSize:0} · clicks {clicks}",
            color: Color.LightGray, fontSize: 12.Sp(), modifier: Modifier.TestTag(tag + ".status"));
        Text($"← {state.CanScrollBackward}   → {state.CanScrollForward}   moving: {state.IsScrollInProgress}",
            color: new Color(.35f, .8f, .9f), fontSize: 11.Sp());
    }

    [Composable]
    private static void Items(ScrollDemoCase item, string tag)
    {
        if (item.Id is "nested-v" or "nested-h")
        {
            var nested = RememberScrollState(); item.Nested = nested;
            var inner = Modifier.Size(260.Dp(), 100.Dp()).TestTag(tag + ".nested");
            if (item.Id == "nested-h") Row(modifier: inner.HorizontalScroll(nested), content: () => NestedItems(true, tag));
            else Column(modifier: inner.VerticalScroll(nested, overscrollEffect: null), content: () => NestedItems(false, tag));
        }
        for (var i = 0; i < item.Count.Value; i++)
        {
            var index = i;
            var width = item.Horizontal ? item.Id == "column" ? 580 : 95 : 290;
            var height = item.Id == "row" ? 300 : item.Horizontal ? 100 : 42;
            Text($"{index + 1:00} · tap", color: Color.White, fontSize: 14.Sp(), modifier: Modifier
                .Size(width.Dp(), height.Dp()).Background(index % 2 == 0 ? new Color(.08f, .31f, .43f) : new Color(.13f, .23f, .37f))
                .TestTag(tag + ".item-" + index).Clickable(() => item.Clicks.Value++));
        }
    }
    [Composable] private static void NestedItems(bool horizontal, string tag)
    {
        for (var i = 0; i < 8; i++)
            Text("Inner " + (i + 1), color: Color.White, modifier: Modifier.Size((horizontal ? 100 : 240).Dp(), 42.Dp())
                .Background(new Color(.34f, .22f, .5f)).TestTag(tag + ".inner-" + i));
    }
    [Composable] private static void Button(string tag, string text, Action click, float width = 65)
    {
        Text(text, color: Color.White, fontSize: 12.Sp(), modifier: Modifier.Size(width.Dp(), 30.Dp())
            .Background(new Color(.17f, .24f, .34f), RoundedCornerShape(4.Dp())).TestTag(tag).Clickable(click));
    }
}

}
