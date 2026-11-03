using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Compose.Net;
using static Compose.Net.ComposeFunctions;

namespace Compose.Net.Unity.Samples.Behaviors.Content;

/// <summary>24 executable examples of the low-level interaction modifiers.</summary>
public partial class InteractionDemo : ComposeUI
{
    [Composable] protected override void Content() => Screen(Remember(() => new InteractionDemoState()));
    [Composable] protected override void Preview() => Screen(Remember(() => new InteractionDemoState()));
    [Composable] public static void Screen(InteractionDemoState state)
    {
        Column(modifier: Modifier.FillMaxSize().Background(new Color(.035f, .05f, .08f))
            .VerticalScroll(RememberScrollState()).Padding(16.Dp()).TestTag("interaction-demo"), content: () =>
        {
            Text("Interaction lab", color: Color.White, fontSize: 30.Sp());
            Text("DRAG · mouse, pen or touch     SCROLL · wheel, trackpad or touch     HOVER · move the pointer", color: Color.LightGray, fontSize: 13.Sp());
            Text("These modifiers report input. The marker moves only because the example applies the delta. Try switching controls during a gesture.", color: Color.LightGray, fontSize: 12.Sp());
            for (var row = 0; row < 8; row++)
            {
                var first = row * 3;
                Row(content: () => { Card(state.Cases[first]); Spacer(Modifier.Width(8.Dp())); Card(state.Cases[first + 1]); Spacer(Modifier.Width(8.Dp())); Card(state.Cases[first + 2]); });
                Spacer(Modifier.Height(8.Dp()));
            }
        });
    }
    [Composable] public static void Card(InteractionDemoCase item)
    {
        var source = item.Source.Value;
        var hovered = source.CollectIsHoveredAsState().Value;
        var dragged = source.CollectIsDraggedAsState().Value;
        var multiplier = item.Multiplier.Value;
        item.Drag = RememberDraggableState(delta => item.Consume(delta * multiplier));
        item.Scroll = RememberScrollableState(delta => item.Consume(delta * multiplier) / multiplier);
        Action<Offset> onStarted = offset => item.Start.Value = offset;
        Action<float> onStopped = velocity => item.Velocity.Value = velocity;
        var parent = RememberScrollableState(delta => { item.Parent.Value += delta; return delta; });
        LaunchedEffect(source, async scope => await source.Interactions.Collect(interaction => item.Record(interaction)));
        Column(modifier: Modifier.Width(344.Dp()).Height(350.Dp()).Padding(5.Dp())
            .Background(new Color(.075f, .1f, .15f), RoundedCornerShape(9.Dp())).Padding(10.Dp()).TestTag("case." + item.Id), content: () =>
        {
            Text(item.Title, color: Color.White, fontSize: 17.Sp());
            Text(item.Description, color: Color.LightGray, fontSize: 12.Sp(), modifier: Modifier.Height(44.Dp()));
            var target = Modifier.Size(314.Dp(), 110.Dp()).TestTag("interaction." + item.Id)
                .Background(hovered || dragged ? new Color(.12f, .42f, .52f) : new Color(.09f, .19f, .29f), RoundedCornerShape(6.Dp()));
            Box(modifier: Modifier.Size(314.Dp(), 110.Dp()), content: () =>
            {
            if (item.Visible.Value)
            {
                if (item.Kind == InteractionDemoKind.Drag)
                {
                    if (item.Id == "drag-click") target = target.Clickable(() => item.Clicks.Value++);
                    target = target.Draggable(item.Drag, item.Axis, item.Enabled.Value, source, item.Immediate.Value,
                        onStarted, onStopped, item.Reverse.Value);
                }
                else if (item.Kind == InteractionDemoKind.Scroll)
                    target = target.Scrollable(item.Scroll, item.Axis, item.Enabled.Value, item.Reverse.Value,
                        new FlingBehavior(item.Deceleration), source);
                else
                {
                    target = target.Hoverable(source, item.Enabled.Value);
                    if (item.Id == "hover-duplicate") target = target.Hoverable(source, item.Enabled.Value);
                }
                var outer = item.Id == "scroll-nested" ? Modifier.Scrollable(parent, item.Axis, flingBehavior: new FlingBehavior(0)) : Modifier;
                Box(modifier: outer, content: () => Box(modifier: target, contentAlignment: Alignment.Center, content: () =>
                {
                    var marker = Modifier.Size(130.Dp(), 38.Dp());
                    if (item.Kind != InteractionDemoKind.Hover)
                        marker = marker.Offset(item.Axis == Orientation.Horizontal ? item.Value.Value.Dp() : 0.Dp(),
                            item.Axis == Orientation.Vertical ? item.Value.Value.Dp() : 0.Dp());
                    marker = marker.Background(new Color(.25f, .58f, .66f), RoundedCornerShape(5.Dp())).TestTag("interaction." + item.Id + ".marker");
                    if (item.Id is "hover-nested" or "hover-shared")
                        marker = marker.Hoverable(item.Id == "hover-shared" ? source : Remember(MutableInteractionSource));
                    Box(modifier: marker, contentAlignment: Alignment.Center, content: () =>
                        Text(item.Kind == InteractionDemoKind.Hover ? (hovered ? "Hovered" : "Move over me") : (item.Axis == Orientation.Horizontal ? "← move →" : "↑ move ↓"), color: Color.White, fontSize: 12.Sp()));
                }));
            }
            else Box(modifier: Modifier.Size(314.Dp(), 110.Dp()), content: () => Text("Target removed — interactions are closed", color: Color.LightGray));
            });
            Spacer(Modifier.Height(8.Dp()));
            Text($"delta Σ {item.Value.Value:F1} · parent {item.Parent.Value:F1} · speed {item.Velocity.Value:F0}/s\nhover {hovered} · drag {dragged} · scroll {item.Scroll.IsScrollInProgress} · clicks {item.Clicks.Value}",
                fontSize: 11.Sp(), color: Color.LightGray, modifier: Modifier.Height(34.Dp()).TestTag("interaction." + item.Id + ".status"));
            Text(item.Log.Value, fontSize: 10.Sp(), color: new Color(.5f,.8f,.72f), modifier: Modifier.Height(28.Dp()).TestTag("interaction." + item.Id + ".log"));
            Row(content: () =>
            {
                Control(item, "enabled", item.Enabled.Value ? "Disable" : "Enable", () => item.Enabled.Value = !item.Enabled.Value);
                if (item.Kind != InteractionDemoKind.Hover)
                {
                    Control(item, "reverse", "Reverse", () => item.Reverse.Value = !item.Reverse.Value);
                    if (item.Kind == InteractionDemoKind.Drag)
                        Control(item, "immediate", item.Immediate.Value ? "Slop: off" : "Slop: on", () => item.Immediate.Value = !item.Immediate.Value);
                    else Control(item, "stop", "Stop", item.Stop);
                    Control(item, "program", "+25", () => item.Programmatic(25));
                }
            });
            Row(content: () =>
            {
                Control(item, "show", item.Visible.Value ? "Remove" : "Restore", () => item.Visible.Value = !item.Visible.Value);
                Control(item, "source", "New source", () => item.Source.Value = MutableInteractionSource());
                if (item.Kind != InteractionDemoKind.Hover)
                    Control(item, "callback", "Delta ×" + item.Multiplier.Value, () => item.Multiplier.Value = item.Multiplier.Value == 1 ? 2 : 1);
                Control(item, "reset", "Reset", item.Reset);
            });
        });
    }
    [Composable] private static void Control(InteractionDemoCase item, string action, string label, Action click) =>
        Box(modifier: Modifier.Size(78.Dp(), 28.Dp()), contentAlignment: Alignment.Center, content: () =>
            Box(modifier: Modifier.Size(74.Dp(), 24.Dp()).Background(new Color(.17f,.23f,.31f), RoundedCornerShape(4.Dp()))
                .TestTag("interaction." + item.Id + "." + action).Clickable(click), contentAlignment: Alignment.Center,
                content: () => Text(label, color: Color.White, fontSize: 10.Sp())));

}

public enum InteractionDemoKind { Drag, Scroll, Hover }
public sealed class InteractionDemoCase
{
    public string Id { get; }
    public string Title { get; }
    public string Description { get; }
    public InteractionDemoKind Kind { get; }
    public Orientation Axis = Orientation.Horizontal;
    public float Limit = float.PositiveInfinity, Deceleration = .135f;
    public readonly IMutableState<float> Value = MutableStateOf(0f), Parent = MutableStateOf(0f), Velocity = MutableStateOf(0f);
    public readonly IMutableState<bool> Enabled = MutableStateOf(true), Visible = MutableStateOf(true), Reverse = MutableStateOf(false), Immediate = MutableStateOf(false);
    public readonly IMutableState<int> Multiplier = MutableStateOf(1), Clicks = MutableStateOf(0);
    public readonly IMutableState<Offset> Start = MutableStateOf(new Offset(0,0));
    public readonly IMutableState<string> Log = MutableStateOf("Waiting for input");
    public readonly IMutableState<IMutableInteractionSource> Source = MutableStateOf<IMutableInteractionSource>(MutableInteractionSource());
    public IDraggableState Drag = null!;
    public IScrollableState Scroll = null!;
    public int Starts, Stops, Cancels, Enters, Exits;
    public InteractionDemoCase(string id, string title, string description, InteractionDemoKind kind)
    { Id = id; Title = title; Description = description; Kind = kind; }
    public float Consume(float delta)
    { var next = Math.Clamp(Value.Value + delta, -Limit, Limit); var used = next - Value.Value; Value.Value = next; return used; }
    public void Record(IInteraction interaction)
    {
        var name = interaction switch
        {
            IDragInteraction.Start => "Start #" + ++Starts,
            IDragInteraction.Stop => "Stop #" + ++Stops,
            IDragInteraction.Cancel => "Cancel #" + ++Cancels,
            IHoverInteraction.Enter => "Enter #" + ++Enters,
            IHoverInteraction.Exit => "Exit #" + ++Exits,
            _ => null
        };
        if (name != null) Log.Value = $"{name}    drag {Starts}/{Stops}/{Cancels}    hover {Enters}/{Exits}";
    }
    public async void Programmatic(float delta)
    {
        try
        {
            if (Kind == InteractionDemoKind.Scroll) await Scroll.ScrollBy(delta);
            else if (Kind == InteractionDemoKind.Drag) await Drag.Drag(scope => { scope.DragBy(delta); return Task.CompletedTask; });
        }
        catch (OperationCanceledException) { }
    }
    public async void Stop() { try { await Scroll.StopScroll(MutatePriority.UserInput); } catch (OperationCanceledException) { } }
    public void Reset() { Value.Value = Parent.Value = Velocity.Value = 0; Clicks.Value = 0; }
}
public sealed class InteractionDemoState
{
    public readonly InteractionDemoCase[] Cases =
    {
        new("drag-horizontal", "01 · Horizontal drag", "Drag with mouse, pen or touch. Delta follows the pointer; no inertia.", InteractionDemoKind.Drag),
        new("drag-vertical", "02 · Vertical drag", "Only vertical travel counts. Cross-axis movement leaves the gesture free.", InteractionDemoKind.Drag) { Axis = Orientation.Vertical },
        new("drag-reverse", "03 · Reverse direction", "Delta and release velocity reverse. Programmatic +25 keeps its sign.", InteractionDemoKind.Drag),
        new("drag-immediate", "04 · Start immediately", "Claims the press without touch slop; useful for catching an animation.", InteractionDemoKind.Drag),
        new("drag-disabled", "05 · Disabled drag", "Disabled input still permits programmatic +25. Enable to start dragging.", InteractionDemoKind.Drag),
        new("drag-bounded", "06 · Bounded drag", "The callback clamps the marker to ±60. The modifier has no layout policy.", InteractionDemoKind.Drag) { Limit = 60 },
        new("drag-callback", "07 · Updated callbacks", "Change Delta × during a gesture. State and the active Start survive recomposition.", InteractionDemoKind.Drag),
        new("drag-click", "08 · Tap or drag", "Tap increments clicks; a drag cancels the press. Try Slop: off too.", InteractionDemoKind.Drag),
        new("scroll-horizontal", "09 · Horizontal scroll", "Wheel/trackpad or touch. Mouse dragging is reserved for draggable.", InteractionDemoKind.Scroll),
        new("scroll-vertical", "10 · Vertical scroll", "Vertical deltas and native UI Toolkit fling animation.", InteractionDemoKind.Scroll) { Axis = Orientation.Vertical },
        new("scroll-reverse", "11 · Reversed scroll", "Reverse input while preserving programmatic direction and layout.", InteractionDemoKind.Scroll),
        new("scroll-bounded", "12 · Partial consumption", "Clamps at ±60 and returns only consumed delta; bounds stop the fling.", InteractionDemoKind.Scroll) { Limit = 60 },
        new("scroll-no-fling", "13 · No fling", "FlingBehavior(0): stopping the finger stops movement immediately.", InteractionDemoKind.Scroll) { Deceleration = 0 },
        new("scroll-fling", "14 · Catch the fling", "A new touch, wheel or Disable interrupts inertia. Watch scroll progress.", InteractionDemoKind.Scroll) { Deceleration = .35f },
        new("scroll-disabled", "15 · Disabled scroll", "Input is ignored; programmatic +25 is available. Content is never clipped.", InteractionDemoKind.Scroll),
        new("scroll-nested", "16 · Nested consumption", "Child consumes up to ±25; the parent receives the remainder, including fling.", InteractionDemoKind.Scroll) { Limit = 25 },
        new("hover-basic", "17 · Enter / Exit", "Move in and out. Each Exit refers to its own Enter; no duplicates on movement.", InteractionDemoKind.Hover),
        new("hover-disabled", "18 · Disabled hover", "Enable to listen. Disabling an active hover closes its interaction.", InteractionDemoKind.Hover),
        new("hover-nested", "19 · Nested hover", "Child has its own source. Crossing its edge does not exit the parent.", InteractionDemoKind.Hover),
        new("hover-shared", "20 · Shared hover source", "Parent and child share a source. Parent remains hovered after leaving the child.", InteractionDemoKind.Hover),
        new("hover-duplicate", "21 · Two hover modifiers", "Each modifier owns one pair, even when both use the same source.", InteractionDemoKind.Hover),
        new("hover-source", "22 · Replace source", "New source closes the old Enter. Leave and re-enter to start the new pair.", InteractionDemoKind.Hover),
        new("hover-remove", "23 · Remove / restore", "Removing a hovered target closes its interaction; no stuck hover state.", InteractionDemoKind.Hover),
        new("hover-recompose", "24 · Stable on updates", "Press controls and move between siblings. Recomposition does not replay Enter.", InteractionDemoKind.Hover)
    };
    public InteractionDemoState()
    {
        this["drag-reverse"].Reverse.Value = this["scroll-reverse"].Reverse.Value = true;
        this["drag-immediate"].Immediate.Value = true;
        this["drag-disabled"].Enabled.Value = this["scroll-disabled"].Enabled.Value = this["hover-disabled"].Enabled.Value = false;
    }
    public InteractionDemoCase this[string id] => Array.Find(Cases, item => item.Id == id)!;
}
