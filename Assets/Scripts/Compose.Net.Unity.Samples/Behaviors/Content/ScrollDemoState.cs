using System;
using Compose.Net;
using static Compose.Net.ComposeFunctions;

namespace Compose.Net.Unity.Samples.Behaviors.Content;

public sealed class ScrollDemoState
{
    public readonly ScrollDemoCase[] Cases =
    {
        new("vertical", "Vertical · native wheel / touch", false),
        new("horizontal", "Horizontal · wheel / trackpad", true),
        new("reverse-v", "Reverse vertical · zero at bottom", false, reverse: true),
        new("reverse-h", "Reverse horizontal · zero at right", true, reverse: true),
        new("disabled-v", "Disabled input · controls still work", false, enabled: false),
        new("disabled-h", "Disabled horizontal · clickable children", true, enabled: false),
        new("initial-v", "Initial vertical offset · 120", false, initial: 120),
        new("initial-h", "Initial horizontal offset · 120", true, initial: 120),
        new("dynamic-v", "Change content, viewport and state", false),
        new("dynamic-h", "Horizontal resize and bounds", true),
        new("short", "Content fits · no scrolling", false, count: 1),
        new("empty", "Empty content · zero range", true, count: 0),
        new("no-fling", "Native drag · inertia disabled", false),
        new("clamped", "No overscroll · hard boundaries", true),
        new("row", "Vertical scroll preserves Row layout", false),
        new("column", "Horizontal scroll preserves Column layout", true),
        new("nested-v", "Nested vertical · boundary hand-off", false),
        new("nested-h", "Horizontal inside vertical", false),
    };
    public ScrollDemoCase this[string id] => Array.Find(Cases, item => item.Id == id)!;
}

public sealed class ScrollDemoCase
{
    public readonly string Id, Title;
    public readonly bool Horizontal;
    public readonly IMutableState<IScrollState> State;
    public readonly IMutableState<bool> Enabled, Reverse, Present = MutableStateOf(true);
    public readonly IMutableState<int> Count, Clicks = MutableStateOf(0);
    public readonly IMutableState<float> Viewport = MutableStateOf(140f);
    public IScrollState? Nested;

    public ScrollDemoCase(string id, string title, bool horizontal, bool reverse = false,
        bool enabled = true, float initial = 0, int count = 10)
    {
        Id = id; Title = title; Horizontal = horizontal;
        State = MutableStateOf<IScrollState>(new ScrollState(initial));
        Enabled = MutableStateOf(enabled); Reverse = MutableStateOf(reverse); Count = MutableStateOf(count);
    }
}
