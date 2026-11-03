using System;
using System.Collections.Generic;
using Compose.Net;
using static Compose.Net.ComposeFunctions;

namespace Compose.Net.Unity.Samples.Behaviors.Content;

/// <summary>Interactive parity gallery for both lazy-grid orientations.</summary>
public partial class GridDemo : ComposeUI
{
    [Composable] protected override void Content() => Screen(Remember(() => new GridDemoState()));
    [Composable] protected override void Preview() => Screen(Remember(() => new GridDemoState()));

    [Composable] public static void Screen(GridDemoState demo)
    {
        var scroll = RememberScrollState();
        Column(modifier: Modifier.FillMaxSize().Background(new Color(.035f, .05f, .075f))
            .VerticalScroll(scroll).Padding(18.Dp()).TestTag("grid-demo"), content: () =>
        {
            Text("Lazy grid lab", color: Color.White, fontSize: 32.Sp());
            Text("24 live examples · scroll, resize, tap cells, change data and inspect position", color: Color.LightGray, fontSize: 15.Sp());
            Text("Columns in vertical grids · rows in horizontal grids · spans occupy cross-axis tracks", color: Color.LightGray, fontSize: 13.Sp());
            for (var row = 0; row < demo.Cases.Count / 3; row++)
            {
                var start = row * 3;
                Row(content: () => { Card(demo.Cases[start]); Card(demo.Cases[start + 1]); Card(demo.Cases[start + 2]); });
            }
        });
    }

    [Composable] public static void Card(GridDemoCase item)
    {
        var revision = item.Revision.Value;
        var state = Remember(item.State.Value, () => item.State.Value);
        var scope = RememberCoroutineScope();
        var tag = "grid." + item.Id;
        Column(modifier: Modifier.Width(370.Dp()).Height(420.Dp()).Padding(6.Dp())
            .Background(new Color(.075f, .105f, .16f), RoundedCornerShape(10.Dp())).Padding(10.Dp()), content: () =>
        {
            Text(item.Title, color: Color.White, fontSize: 16.Sp(), modifier: Modifier.Height(26.Dp()));
            Text(item.Description, color: Color.LightGray, fontSize: 12.Sp(), modifier: Modifier.Height(40.Dp()));
            Status(item, state, tag);
            Box(modifier: Modifier.Size(336.Dp(), 168.Dp()).Background(new Color(.02f, .03f, .05f)), content: () =>
            {
                if (!item.Present.Value) { Text("Grid removed", color: Color.White); return; }
                var modifier = Modifier.Size(item.Width.Value.Dp(), item.Height.Value.Dp()).TestTag(tag + ".viewport");
                var prefix = item.Prefix.Value; var count = item.Count.Value;
                Action<ILazyGridScope> content = grid =>
                {
                    if (item.Sticky)
                    {
                        for (var section = 0; section < 6; section++)
                        {
                            var part = section;
                            grid.StickyHeader(index => Cell(item, "Section " + (part + 1), index, true), "header-" + section, "header");
                            grid.Items(12, i => Cell(item, "" + (part * 12 + i), part * 13 + i + 1, false), i => part * 13 + i + 1);
                        }
                    }
                    else grid.Items(count, i => Cell(item, "" + (i - prefix), i - prefix, false),
                        key: i => i - prefix,
                        span: item.Spans ? (s, i) => new GridItemSpan(i % 7 == 0 ? s.MaxLineSpan : i % 7 == 1 ? Math.Min(2, s.MaxLineSpan) : 1) :
                            item.RemainingSpan ? (s, i) => new GridItemSpan(i % 4 == 1 ? s.MaxCurrentLineSpan : 1) : null,
                        contentType: i => i % 2);
                };
                if (item.Horizontal) LazyHorizontalGrid(item.Cells, content, modifier, state,
                    item.Padding, item.Reverse.Value, item.Main as Arrangement.Horizontal, item.Cross as Arrangement.Vertical,
                    item.NoFling ? new FlingBehavior(0) : null, item.Enabled.Value,
                    item.Elastic ? OverscrollEffect.Default : (OverscrollEffect?)null);
                else LazyVerticalGrid(item.Cells, content, modifier, state,
                    item.Padding, item.Reverse.Value, item.Main as Arrangement.Vertical, item.Cross as Arrangement.Horizontal,
                    item.NoFling ? new FlingBehavior(0) : null, item.Enabled.Value,
                    item.Elastic ? OverscrollEffect.Default : (OverscrollEffect?)null);
            });
            Row(content: () =>
            {
                Button(tag + ".start", "Start", () => { item.Interactions++; state.ScrollToItem(0); });
                Button(tag + ".jump", "Jump 45", () => { item.Interactions++; state.ScrollToItem(45, 8); }, 76);
                Button(tag + ".end", "End", () => { item.Interactions++; state.ScrollToItem(Math.Max(0, item.Count.Value - 1)); });
                Button(tag + ".animate", "Animate", () => { item.Interactions++; scope.Launch(async _ =>
                {
                    try { await state.AnimateScrollToItem(state.FirstVisibleItemIndex < 12 ? 12 : 0, animationSpec: Tween(700)); }
                    catch (OperationCanceledException) { }
                }); }, 80);
            });
            Row(content: () =>
            {
                Button(tag + ".resize", "Resize", () => { item.Interactions++; item.Width.Value = item.Width.Value == 320 ? 220 : 320; item.Height.Value = item.Height.Value == 160 ? 110 : 160; }, 76);
                Button(tag + ".input", item.Enabled.Value ? "Input: on" : "Input: off", () => { item.Interactions++; item.Enabled.Value = !item.Enabled.Value; }, 94);
                Button(tag + ".reverse", "Reverse", () => { item.Interactions++; item.Reverse.Value = !item.Reverse.Value; }, 82);
                Button(tag + ".size", "Size", () => { item.Interactions++; item.CellSize.Value = item.CellSize.Value == 54 ? 82 : 54; });
            });
            Row(content: () =>
            {
                Button(tag + ".prepend", "+3 keys", () => { item.Interactions++; item.Prefix.Value += 3; item.Count.Value += 3; }, 82);
                Button(tag + ".count", "0 / 2 / 90", () => { item.Interactions++; item.Count.Value = item.Count.Value == 0 ? 2 : item.Count.Value == 2 ? 90 : 0; }, 90);
                Button(tag + ".state", "New state", () => { item.Interactions++; item.State.Value = new LazyGridState(21, 5); }, 90);
                Button(tag + ".remove", item.Present.Value ? "Off" : "On", () => { item.Interactions++; item.Present.Value = !item.Present.Value; }, 52);
            });
        });
    }
    [Composable] private static void Status(GridDemoCase item, ILazyGridState state, string tag)
    {
        Text($"First {state.FirstVisibleItemIndex} + {state.FirstVisibleItemScrollOffset:0}px · tracks {state.LayoutInfo.MaxSpan} · visible {state.LayoutInfo.VisibleItemsInfo.Count}",
            color: new Color(.35f, .8f, .9f), fontSize: 11.Sp(), modifier: Modifier.TestTag(tag + ".status"));
        Text($"← {state.CanScrollBackward} → {state.CanScrollForward} · moving {state.IsScrollInProgress} · taps {item.Clicks.Value}",
            color: Color.LightGray, fontSize: 11.Sp());
    }
    [Composable] private static void Cell(GridDemoCase item, string text, int key, bool header)
    {
        var taps = Remember(() => MutableStateOf(0));
        var size = header ? 30 : item.CellSize.Value + (item.Variable ? Math.Abs(key) % 3 * 18 : 0);
        var tag = "grid." + item.Id + ".cell-" + key;
        var modifier = item.Horizontal ? Modifier.Width(size.Dp()).FillMaxHeight() : Modifier.Height(size.Dp()).FillMaxWidth();
        Box(content: () => Text(text + (taps.Value == 0 ? "" : " · " + taps.Value), color: Color.White, fontSize: 13.Sp()),
            modifier: modifier.Background(header ? new Color(.36f, .22f, .55f) : key % 2 == 0 ? new Color(.08f, .31f, .43f) : new Color(.13f, .23f, .37f))
                .TestTag(tag).Clickable(() => { taps.Value++; item.Clicks.Value++; }));
    }
    [Composable] private static void Button(string tag, string text, Action click, float width = 65) =>
        Text(text, color: Color.White, fontSize: 11.Sp(), modifier: Modifier.Size(width.Dp(), 28.Dp())
            .Background(new Color(.17f, .24f, .34f), RoundedCornerShape(4.Dp())).TestTag(tag).Clickable(click));
}

public sealed class GridDemoState
{
    public IReadOnlyList<GridDemoCase> Cases { get; } = new GridDemoCase[]
    {
        new("vertical", "01 · Fixed columns", "Vertical · three equal columns"),
        new("horizontal", "02 · Fixed rows", "Horizontal · three equal rows", true),
        new("adaptive", "03 · Adaptive columns", "Minimum 85 dp · resize to reflow") { Cells = GridCells.Adaptive(85.Dp()) },
        new("adaptive-h", "04 · Adaptive rows", "Minimum 45 dp · resize to reflow", true) { Cells = GridCells.Adaptive(45.Dp()) },
        new("fixed-size", "05 · FixedSize columns", "70 dp columns · remaining space centered") { Cells = GridCells.FixedSize(70.Dp()), Cross = Arrangement.Center },
        new("fixed-size-h", "06 · FixedSize rows", "42 dp rows · aligned to the bottom", true) { Cells = GridCells.FixedSize(42.Dp()), Cross = Arrangement.Bottom },
        new("spans", "07 · Column spans", "Full-width section, double and single cells") { Spans = true },
        new("spans-h", "08 · Row spans", "Full-height section, double and single cells", true) { Spans = true },
        new("remaining", "09 · Remaining span", "MaxCurrentLineSpan fills the rest of a line") { RemainingSpan = true },
        new("reverse", "10 · Reverse vertical", "Starts at the bottom; column order is preserved") { Reverse = MutableStateOf(true) },
        new("reverse-h", "11 · Reverse horizontal", "Starts at the right; row order is preserved", true) { Reverse = MutableStateOf(true) },
        new("spacing", "12 · Padding and gaps", "Asymmetric padding · 10 dp between rows · 6 dp between columns") { Padding = new(Top: 9.Dp(), Bottom: 15.Dp(), Left: 12.Dp(), Right: 18.Dp()), Main = Arrangement.SpacedBy(10.Dp()), Cross = Arrangement.SpacedBy(6.Dp()) },
        new("variable", "13 · Variable heights", "Row height equals its tallest cell") { Variable = true },
        new("variable-h", "14 · Variable widths", "Column width equals its widest cell", true) { Variable = true },
        new("initial", "15 · Initial position", "Starts at item 30 with an 8 px offset") { State = MutableStateOf<ILazyGridState>(new LazyGridState(30, 8)) },
        new("disabled", "16 · User input disabled", "State controls and cell clicks still work") { Enabled = MutableStateOf(false) },
        new("no-fling", "17 · No inertia", "Native scroll with deceleration disabled", true) { NoFling = true },
        new("elastic", "18 · Elastic overscroll", "Native touch elasticity at both edges") { Elastic = true },
        new("keys", "19 · Stable keys", "Jump, tap, prepend: position and taps survive"),
        new("sticky", "20 · Sticky rows", "Full-span section headers pin at the leading edge") { Sticky = true },
        new("sticky-h", "21 · Sticky columns", "Full-span section headers in horizontal flow", true) { Sticky = true },
        new("million", "22 · Million items", "Only visible lines are composed") { Count = MutableStateOf(1000000) },
        new("short", "23 · Short content", "Two cells centered along the scroll axis") { Count = MutableStateOf(2), Main = Arrangement.Center },
        new("empty", "24 · Empty and lifecycle", "Add data, replace state, remove and restore") { Count = MutableStateOf(0) },
    };
}
public sealed class GridDemoCase
{
    public readonly string Id, Title, Description;
    public readonly bool Horizontal;
    public GridCells Cells = GridCells.Fixed(3);
    public PaddingValues Padding;
    public Arrangement? Main, Cross;
    public bool Variable, Spans, RemainingSpan, Sticky, NoFling, Elastic;
    public IMutableState<ILazyGridState> State = MutableStateOf<ILazyGridState>(new LazyGridState());
    public IMutableState<bool> Enabled = MutableStateOf(true), Reverse = MutableStateOf(false), Present = MutableStateOf(true);
    public IMutableState<int> Count = MutableStateOf(90), Prefix = MutableStateOf(0), Width = MutableStateOf(320), Height = MutableStateOf(160),
        CellSize = MutableStateOf(54), Clicks = MutableStateOf(0), Revision = MutableStateOf(0);
    public int Interactions;
    public GridDemoCase(string id, string title, string description, bool horizontal = false)
    { Id = id; Title = title; Description = description; Horizontal = horizontal; }
}
