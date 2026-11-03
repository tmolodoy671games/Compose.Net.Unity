using Compose.Net;
using static Compose.Net.ComposeFunctions;

namespace Compose.Net.Unity.Samples.Behaviors.Content;

public sealed class LazyListDemoState
{
    public readonly LazyListDemoCase[] Cases =
    {
        new("column", "01 · LazyColumn", false), new("row", "02 · LazyRow", true),
        new("reverse-column", "03 · Column · reverse", false, reverse: true), new("reverse-row", "04 · Row · reverse", true, reverse: true),
        new("variable-column", "05 · Variable heights", false), new("variable-row", "06 · Variable widths", true),
        new("padding-column", "07 · Padding + spacing", false), new("padding-row", "08 · Padding + spacing", true),
        new("keys-column", "09 · Stable keys · prepend", false), new("keys-row", "10 · Stable keys · prepend", true),
        new("sticky-column", "11 · Sticky section headers", false), new("sticky-row", "12 · Sticky section headers", true),
        new("initial-column", "13 · Start at item 500", false, initial: 500), new("initial-row", "14 · Start at item 500", true, initial: 500),
        new("disabled", "15 · User input disabled", false, enabled: false), new("massive", "16 · 100,000 items", true, count: 100000),
        new("nested", "17 · Rows inside a column", false), new("short", "18 · Parent sizing / empty", true, count: 2),
    };
}
public sealed class LazyListDemoCase
{
    public readonly string Id, Title;
    public readonly bool Horizontal;
    public readonly IMutableState<ILazyListState> State;
    public readonly IMutableState<int> Count, Prefix = MutableStateOf(0), Viewport = MutableStateOf(150), ItemSize = MutableStateOf(54);
    public readonly IMutableState<int> Active = MutableStateOf(0), Created = MutableStateOf(0), Clicks = MutableStateOf(0);
    public readonly IMutableState<bool> Enabled, Reverse, Present = MutableStateOf(true), Physics = MutableStateOf(true);
    public LazyListDemoCase(string id, string title, bool horizontal, int count = 1000, int initial = 0, bool enabled = true, bool reverse = false)
    {
        Id = id; Title = title; Horizontal = horizontal; Count = MutableStateOf(count);
        State = MutableStateOf<ILazyListState>(new LazyListState(initial, initial == 0 ? 0 : 9));
        Enabled = MutableStateOf(enabled); Reverse = MutableStateOf(reverse);
    }
}
