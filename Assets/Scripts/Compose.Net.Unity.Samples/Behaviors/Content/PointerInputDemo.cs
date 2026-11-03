using System;
using System.Linq;
using System.Threading.Tasks;
using Compose.Net;

namespace Compose.Net.Unity.Samples.Behaviors.Content;

/// <summary>The scene component, Editor window and UI tests use the same interactive gallery.</summary>
public partial class PointerInputDemo : ComposeUI
{
    [Composable] protected override void Content() => Screen(Remember(() => new PointerInputDemoState()));
    [Composable] protected override void Preview() => Screen(Remember(() => new PointerInputDemoState()));

    [Composable]
    public static void Screen(PointerInputDemoState state)
    {
        Column(modifier: Modifier.FillMaxSize().Background(new Color(.045f, .06f, .09f))
            .VerticalScroll(RememberScrollState()).Padding(20.Dp()).TestTag("pointer-demo"), content: () =>
        {
            Text("PointerInput / event laboratory", color: Color.White, fontSize: 28.Sp());
            Text("18 live examples · mouse, touch and pen · scroll for more · each card keeps its own counters", color: Color.LightGray);
            Text("Transform cards require two touches for zoom/rotation; one pointer demonstrates pan. Raw input has no automatic ripple or keyboard click.", color: Color.LightGray);
            for (var row = 0; row < 6; row++)
            {
                var offset = row * 3;
                Row(content: () =>
                {
                    for (var col = 0; col < 3; col++) Card(state[PointerInputDemoState.Ids[offset + col]]);
                });
            }
        });
    }

    [Composable]
    public static void Card(PointerInputDemoCase item)
    {
        _ = item.Revision.Value;
        var enabled = item.Enabled.Value;
        var key = item.Key.Value;
        var step = item.Step.Value;
        var visible = item.Visible.Value;
        Column(modifier: Modifier.Width(330.Dp()).Height(290.Dp()).Padding(8.Dp()).TestTag("pointer." + item.Id + ".card"), content: () =>
        {
            Text(Title(item.Id), color: Color.White, fontSize: 18.Sp());
            Text(Hint(item.Id), color: Color.LightGray, fontSize: 12.Sp(), modifier: Modifier.Height(44.Dp()));
            if (visible)
            {
                var modifier = Modifier.Size(310.Dp(), 130.Dp()).Background(new Color(.10f, .20f, .29f)).TestTag("pointer." + item.Id);
                if (item.Id == "keys") modifier = modifier.PointerInput(key, enabled, s => Lifetime(s, item, enabled, step));
                else if (item.Id == "latest") modifier = modifier.PointerInput("stable", s => Lifetime(s, item, true, null));
                else if (item.Id == "lifetime") modifier = modifier.PointerInput(new object?[] { enabled, "lifetime", 1 }, s => Lifetime(s, item, enabled, null));
                else modifier = modifier.PointerInput(item, s => Handle(s, item));
                Column(modifier: modifier, content: () =>
                {
                    if (item.Id == "consume")
                        Text("Child consumes / parent still observes", color: Color.White,
                            modifier: Modifier.Size(285.Dp(), 34.Dp()).TestTag("pointer.consume.child")
                                .PointerInput(item, s => s.DetectTapGestures(onTap: item.Tap)));
                    if (item.Id == "siblings")
                        Row(content: () =>
                        {
                            Text("Left", color: Color.White, modifier: Modifier.Size(130.Dp(), 40.Dp()).TestTag("pointer.siblings.left")
                                .PointerInput("left", s => s.DetectTapGestures(onTap: _ => item.Log("Left tap"))));
                            Text("Right", color: Color.White, modifier: Modifier.Size(130.Dp(), 40.Dp()).TestTag("pointer.siblings.right")
                                .PointerInput("right", s => s.DetectTapGestures(onTap: _ => item.Log("Right tap"))));
                        });
                    Text(item.Status, color: Color.White, fontSize: 12.Sp(), modifier: Modifier.TestTag("pointer." + item.Id + ".status"));
                    if (item.Id is "drag" or "horizontal" or "vertical" or "hold-drag" or "transform" or "pan-lock")
                        Box(modifier: Modifier.Size(22.Dp()).Background(Color.Orange)
                            .Offset(Math.Clamp(item.Pan.X, -60, 180).Dp(), Math.Clamp(item.Pan.Y, -10, 10).Dp())
                            .Scale(Math.Clamp(item.Zoom, .5f, 2)).Rotate(item.Rotation).TestTag("pointer." + item.Id + ".marker"), content: () => { });
                });
            }
            else Text("Handler removed — active waits canceled", color: Color.LightGray, modifier: Modifier.Height(130.Dp()));
            if (item.Id == "keys" || item.Id == "latest" || item.Id == "lifetime")
                Row(content: () =>
                {
                    if (item.Id == "keys") Control(item.Id + ".key", "Key +", () => item.Key.Value++);
                    if (item.Id != "lifetime") Control(item.Id + ".step", "Step " + step, () => item.Step.Value = item.Step.Value == 1 ? 10 : 1);
                    if (item.Id != "latest") Control(item.Id + ".enabled", enabled ? "Disable" : "Enable", () => item.Enabled.Value = !item.Enabled.Value);
                    Control(item.Id + ".visible", visible ? "Remove" : "Restore", () => item.Visible.Value = !item.Visible.Value);
                });
        });
    }

    private static async Task Handle(IPointerInputScope scope, PointerInputDemoCase item)
    {
        switch (item.Id)
        {
            case "tap": await scope.DetectTapGestures(onTap: item.Tap); break;
            case "double": await scope.DetectTapGestures(onDoubleTap: item.DoubleTap, onTap: item.Tap); break;
            case "long": await scope.DetectTapGestures(onLongPress: item.LongPress, onTap: item.Tap); break;
            case "press": await scope.DetectTapGestures(onTap: item.Tap, onPress: async (press, position) =>
            {
                item.Presses++; item.Show($"Pressed at {position} — release or leave bounds");
                if (await press.TryAwaitRelease()) { item.Releases++; item.Show("Released"); }
                else item.Cancel();
            }); break;
            case "drag": await scope.DetectDragGestures(item.Drag, item.Start, item.End, item.Cancel); break;
            case "horizontal": await scope.DetectHorizontalDragGestures((c, d) => item.Drag(c, new Offset(d, 0)), item.Start, item.End, item.Cancel); break;
            case "vertical": await scope.DetectVerticalDragGestures((c, d) => item.Drag(c, new Offset(0, d)), item.Start, item.End, item.Cancel); break;
            case "hold-drag": await scope.DetectDragGesturesAfterLongPress(item.Drag, item.Start, item.End, item.Cancel); break;
            case "transform": case "pan-lock": await scope.DetectTransformGestures(item.Transform, panZoomLock: item.Id == "pan-lock"); break;
            case "timeout": await scope.AwaitEachGesture(async events =>
            {
                await events.AwaitFirstDown();
                var up = await events.WithTimeoutOrNull(1000, s => s.WaitForUpOrCancellation());
                item.Show(up == null ? "No valid release within 1 second" : "Released within 1 second");
            }); break;
            default: await scope.AwaitPointerEventScope(async events =>
            {
                while (true)
                {
                    if (item.Id == "passes")
                    {
                        var initial = await events.AwaitPointerEvent(PointerEventPass.Initial);
                        var main = await events.AwaitPointerEvent();
                        var final = await events.AwaitPointerEvent(PointerEventPass.Final);
                        item.Events++; item.Show($"{initial.Type} #{item.Events}\nInitial → Main → Final\nSame event: {ReferenceEquals(initial, main) && ReferenceEquals(main, final)}");
                    }
                    else
                    {
                        var evt = await events.AwaitPointerEvent(); var c = evt.Changes[0]; item.Events++;
                        item.Show($"{evt.Type} · {c.Type} #{c.Id.Value} · {evt.Changes.Count} pointers\nLocal ({c.Position.X:F0}, {c.Position.Y:F0}) · pressure {c.Pressure:F2}\nConsumed {c.IsConsumed} · buttons {evt.Buttons.PackedValue}\nWheel {c.ScrollDelta} · shift {evt.KeyboardModifiers.IsShiftPressed}");
                    }
                }
            }); break;
        }
    }

    private static async Task Lifetime(IPointerInputScope scope, PointerInputDemoCase item, bool enabled, int? capturedStep)
    {
        item.Runs++; item.Show($"Started {item.Runs}, stopped {item.Stops}");
        try
        {
            if (!enabled) return;
            await scope.DetectTapGestures(onTap: _ =>
            { item.Taps += capturedStep ?? item.Step.Value; item.Show($"Total {item.Taps} · runs {item.Runs} · stopped {item.Stops}"); });
        }
        finally { item.Stops++; }
    }
    [Composable] private static void Control(string id, string label, Action action) =>
        Text(label, color: Color.White, fontSize: 11.Sp(), modifier: Modifier.Padding(5.Dp()).Background(new Color(.22f, .29f, .38f)).TestTag("pointer." + id).Clickable(action));

    private static string Title(string id) => id switch
    {
        "raw" => "01 / Raw event inspector", "passes" => "02 / Three event passes", "consume" => "03 / Shared consumption",
        "tap" => "04 / Tap", "double" => "05 / Double tap", "long" => "06 / Long press", "press" => "07 / Press lifecycle",
        "drag" => "08 / Free drag & capture", "horizontal" => "09 / Horizontal drag", "vertical" => "10 / Vertical drag",
        "hold-drag" => "11 / Hold, then drag", "transform" => "12 / Pan, zoom & rotation", "pan-lock" => "13 / Pan / zoom lock",
        "keys" => "14 / Restart keys", "latest" => "15 / Latest value, stable key", "lifetime" => "16 / Handler lifetime",
        "siblings" => "17 / Independent siblings", _ => "18 / Scoped timeout"
    };
    private static string Hint(string id) => id switch
    {
        "raw" => "Hover, press, move, wheel; try right click or Shift.", "passes" => "One snapshot is delivered through all three passes.",
        "consume" => "Child handles tap; parent sees IsConsumed = true.", "tap" => "Immediate onTap on release; leaving bounds cancels.",
        "double" => "Single tap waits 300 ms; double tap requires at least 40 ms.", "long" => "Hold 500 ms; long press suppresses tap.",
        "press" => "TryAwaitRelease distinguishes release from cancellation.", "drag" => "Cross slop, then drag outside: native pointer capture.",
        "horizontal" => "Only X contributes to slop and movement.", "vertical" => "Only Y contributes to slop and movement.",
        "hold-drag" => "Hold 500 ms, then drag without another slop threshold.", "transform" => "Two touches: pinch and rotate. One pointer: pan.",
        "pan-lock" => "Pan / zoom first locks rotation for this gesture.", "keys" => "Step is captured. Key + restarts and reads the new step.",
        "latest" => "Step is read from a stable state holder without restart.", "lifetime" => "Disable / remove cancels active waits and releases capture.",
        "siblings" => "Press one target and release over the other: no click.", _ => "Release within 1 s; timed-out wait is removed safely."
    };
}
