using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Compose.Net;
using static Compose.Net.ComposeFunctions;

namespace Compose.Net.Unity.Samples.Behaviors.Content;

/// <summary>Interactive gallery of pager layout, state, data and input behavior.</summary>
public partial class PagerDemo : ComposeUI
{
    [Composable] protected override void Content() => Screen(Remember(() => new PagerDemoState()));
    [Composable] protected override void Preview() => Screen(Remember(() => new PagerDemoState()));

    [Composable] public static void Screen(PagerDemoState state)
    {
        Column(modifier: Modifier.FillMaxSize().Background(new Color(.04f, .055f, .085f))
            .VerticalScroll(RememberScrollState()).Padding(16.Dp()).TestTag("pager-demo"), content: () =>
        {
            Text("Pager", fontSize: 32.Sp(), color: Color.White);
            Text("Swipe on touch · scroll wheel · arrow keys after focus · use the controls below each example", fontSize: 14.Sp(), color: Color.LightGray);
            for (var row = 0; row < 7; row++)
            {
                var first = row * 3;
                Row(content: () => { Card(state.Cases[first]); Card(state.Cases[first + 1]); Card(state.Cases[first + 2]); });
            }
        });
    }

    [Composable] public static void Card(PagerDemoCase item)
    {
        var revision = item.Revision.Value;
        var count = item.Count.Value;
        var width = item.Width.Value;
        var enabled = item.Enabled.Value;
        var reverse = item.Reverse.Value;
        var state = RememberPagerState(() => item.Count.Value, initialPage: item.InitialPage, initialPageOffsetFraction: item.InitialOffset);
        item.State = state;
        Column(modifier: Modifier.Width(344.Dp()).Height(360.Dp()).Padding(5.Dp())
            .Background(new Color(.085f, .11f, .17f), RoundedCornerShape(10.Dp())).Padding(10.Dp()), content: () =>
        {
            Text(item.Title, fontSize: 18.Sp(), color: Color.White);
            Text(item.Description, fontSize: 12.Sp(), color: Color.LightGray, modifier: Modifier.Height(44.Dp()));
            var modifier = Modifier.Size(width.Dp(), 150.Dp()).Background(new Color(.025f, .04f, .065f))
                .TestTag("pager." + item.Id);
            if (item.Vertical)
                VerticalPager(state, pageContent: index => Page(item, index), modifier: modifier, contentPadding: item.Padding,
                    pageSize: item.Size, pageSpacing: item.Spacing.Dp(), reverseLayout: reverse,
                    userScrollEnabled: enabled, beyondViewportPageCount: item.Beyond,
                    horizontalAlignment: item.Id == "align-end" ? Alignment.End : Alignment.CenterHorizontally,
                    snapPosition: item.Snap, key: item.Keyed ? index => item.Keys[index] : null,
                    flingBehavior: PagerDefaults.FlingBehavior(PagerSnapDistance.AtMost(item.MaxFlingPages)));
            else
                HorizontalPager(state, pageContent: index => Page(item, index), modifier: modifier, contentPadding: item.Padding,
                    pageSize: item.Size, pageSpacing: item.Spacing.Dp(), reverseLayout: reverse,
                    userScrollEnabled: enabled, beyondViewportPageCount: item.Beyond,
                    verticalAlignment: item.Id == "align-top" ? Alignment.Top : Alignment.CenterVertically,
                    snapPosition: item.Snap, key: item.Keyed ? index => item.Keys[index] : null,
                    flingBehavior: PagerDefaults.FlingBehavior(PagerSnapDistance.AtMost(item.MaxFlingPages)));
            Text($"current {state.CurrentPage} · settled {state.SettledPage} · target {state.TargetPage}\noffset {state.CurrentPageOffsetFraction:F2} · count {count} · {(state.IsScrollInProgress ? "scrolling" : "idle")}",
                color: Color.LightGray, fontSize: 12.Sp(), modifier: Modifier.Height(36.Dp()).TestTag("pager." + item.Id + ".status"));
            Row(content: () =>
            {
                Control(item, "previous", "←", () => item.Animate(state.CurrentPage - 1));
                Control(item, "next", "→", () => item.Animate(state.CurrentPage + 1));
                Control(item, "first", "First", () => state.ScrollToPage(0));
                Control(item, "last", "Last", () => state.ScrollToPage(count - 1));
            });
            Row(content: () =>
            {
                Control(item, "lock", enabled ? "Lock" : "Unlock", () => item.Enabled.Value = !item.Enabled.Value);
                Control(item, "reverse", "Reverse", () => item.Reverse.Value = !item.Reverse.Value);
                Control(item, "offset", "+0.25", () => state.ScrollToPage(Math.Min(1, count - 1), .25f));
                Control(item, "resize", "Resize", () => item.Width.Value = item.Width.Value == 314 ? 250 : 314);
            });
            Row(content: () =>
            {
                Control(item, "add", "+ page", item.Add);
                Control(item, "remove", "− page", item.Remove);
                Control(item, "prepend", "Prepend", item.Prepend);
                Control(item, "refresh", "Refresh", () => item.Revision.Value++);
            });
        });
    }

    [Composable] private static void Page(PagerDemoCase item, int index)
    {
        var key = item.Keyed ? item.Keys[index] : index;
        var identity = Remember(() => new object());
        item.Remembered[key] = identity;
        DisposableEffect(identity, () =>
        {
            item.Active.Add(key);
            return OnDispose(() => { item.Active.Remove(key); item.Disposals++; });
        });
        var modifier = item.Id is "align-top" or "align-end" ? Modifier.Size(80.Dp(), 50.Dp()) : Modifier.FillMaxSize();
        if (item.Id == "locked") modifier = modifier.Clickable(() => item.PageClicks.Value++);
        Box(modifier: modifier.Background(new Color(.12f + (key % 3) * .13f, .3f + (key % 4) * .09f, .55f))
            .TestTag("pager." + item.Id + ".page-" + key), contentAlignment: Alignment.Center, content: () =>
        {
            Column(horizontalAlignment: Alignment.CenterHorizontally, content: () =>
            {
                Text($"Page {index}", fontSize: 20.Sp(), color: Color.White);
                Text($"key {key}", fontSize: 11.Sp(), color: Color.LightGray);
                if (item.Id == "locked") Text($"Tap this page: {item.PageClicks.Value}", fontSize: 12.Sp(), color: Color.White);
                if (item.Id == "nested")
                    Column(modifier: Modifier.Size(180.Dp(), 75.Dp()).VerticalScroll(RememberScrollState()), content: () =>
                    {
                        for (var line = 0; line < 12; line++) Text("Nested row " + line, color: Color.White);
                    });
            });
        });
    }

    [Composable] private static void Control(PagerDemoCase item, string action, string label, Action onClick) =>
        Box(modifier: Modifier.Size(78.Dp(), 27.Dp()).Padding(2.Dp()).Background(new Color(.18f, .23f, .32f), RoundedCornerShape(4.Dp()))
            .TestTag("pager." + item.Id + "." + action).Clickable(() => { item.Interactions++; onClick(); }),
            contentAlignment: Alignment.Center, content: () => Text(label, color: Color.White, fontSize: 11.Sp()));
}

public sealed class PagerDemoCase
{
    public string Id { get; }
    public string Title { get; }
    public string Description { get; }
    public bool Vertical, Keyed;
    public int InitialPage, Beyond, MaxFlingPages = 1;
    public float InitialOffset, Spacing;
    public PageSize Size = PageSize.Fill;
    public PaddingValues Padding;
    public SnapPosition Snap;
    public readonly IMutableState<int> PageClicks = MutableStateOf(0);
    public readonly IMutableState<int> Count = MutableStateOf(7), Revision = MutableStateOf(0), Width = MutableStateOf(314);
    public readonly IMutableState<bool> Enabled = MutableStateOf(true), Reverse = MutableStateOf(false);
    public readonly List<int> Keys = new();
    public readonly HashSet<int> Active = new();
    public readonly Dictionary<int, object> Remembered = new();
    public IPagerState State = null!;
    public int Disposals, Interactions;
    private int _nextKey = 100000;
    public PagerDemoCase(string id, string title, string description)
    {
        Id = id; Title = title; Description = description;
        for (var i = 0; i < 7; i++) Keys.Add(i);
    }
    public void Add() { Keys.Add(_nextKey++); Count.Value++; Revision.Value++; }
    public void Remove() { if (Count.Value == 0) return; if (Keys.Count > 0) Keys.RemoveAt(Keys.Count - 1); Count.Value--; Revision.Value++; }
    public void Prepend() { Keys.Insert(0, _nextKey++); Count.Value++; Revision.Value++; }
    public async void Animate(int page)
    {
        try { await State.AnimateScrollToPage(page, animationSpec: Tween(Id == "interrupt" ? 1400 : 350)); }
        catch (OperationCanceledException) { }
    }
}

public sealed class PagerDemoState
{
    public readonly PagerDemoCase[] Cases =
    {
        new("horizontal", "Horizontal / Fill", "Equal pages; native touch, wheel and keyboard input."),
        new("vertical", "Vertical / Fill", "The same state and API on the vertical axis.") { Vertical = true },
        new("fixed", "Fixed page size", "100 dp stays 100 dp, independently of padding.") { Size = new PageSize.Fixed(100.Dp()), Padding = new PaddingValues(12.Dp()), Spacing = 8 },
        new("padding", "Asymmetric padding", "Before / after padding applies to the entire strip.") { Padding = new PaddingValues(Top: 8.Dp(), Bottom: 12.Dp(), Left: 24.Dp(), Right: 40.Dp()) },
        new("spacing", "Page spacing", "24 dp between pages, with no outer spacing.") { Spacing = 24 },
        new("initial", "Initial page + offset", "Starts on page 2 at a quarter-page offset.") { InitialPage = 2, InitialOffset = .25f, Spacing = 10 },
        new("reverse", "Reverse horizontal", "Logical next goes left; page 0 starts at the right."),
        new("reverse-vertical", "Reverse vertical", "Logical next goes up; page 0 starts at the bottom.") { Vertical = true },
        new("locked", "User scroll disabled", "Controls still work while gestures are disabled."),
        new("align-top", "Cross-axis alignment", "Small content aligned to the top of each page."),
        new("align-end", "Vertical / End alignment", "Small content aligned to the right of each page.") { Vertical = true },
        new("center", "Center snap", "Fixed pages snap to the center, within scroll bounds.") { Size = new PageSize.Fixed(110.Dp()), Snap = SnapPosition.Center, Spacing = 10, InitialPage = 2 },
        new("end", "End snap", "Fixed pages snap to the trailing edge.") { Size = new PageSize.Fixed(110.Dp()), Snap = SnapPosition.End, Spacing = 10, InitialPage = 3 },
        new("custom", "Custom PageSize", "Exactly three pages in the available content space.") { Size = new ThreePages(), Spacing = 8, Padding = new PaddingValues(10.Dp()) },
        new("beyond", "Beyond viewport", "Composes two extra pages on each side.") { Beyond = 2, InitialPage = 3 },
        new("large", "10,000 lazy pages", "Jump to last: only the viewport is composed."),
        new("dynamic", "Dynamic page count", "Remove pages at the end; the selected page clamps."),
        new("keys", "Stable keys", "Prepend preserves the selected item and its state.") { Keyed = true, InitialPage = 2 },
        new("interrupt", "Interruptible animation", "Press arrows or First during a long animation."),
        new("nested", "Nested scrolling", "A vertical list inside a horizontal pager."),
        new("empty", "Empty → populated", "Zero pages is valid. Add a page to populate it.")
    };
    public PagerDemoState()
    {
        this["reverse"].Reverse.Value = true;
        this["reverse-vertical"].Reverse.Value = true;
        this["locked"].Enabled.Value = false;
        this["large"].Count.Value = 10000;
        this["empty"].Count.Value = 0;
        this["empty"].Keys.Clear();
    }
    public PagerDemoCase this[string id] => Array.Find(Cases, item => item.Id == id)!;
    private sealed record ThreePages : PageSize
    {
        public override float CalculateMainAxisPageSize(IDensity density, float availableSpace, float pageSpacing) => Math.Max(0, (availableSpace - 2 * pageSpacing) / 3);
    }
}
