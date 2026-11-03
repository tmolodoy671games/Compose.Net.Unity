using System;
using System.Collections.Generic;
using Compose.Net;

namespace Compose.Net.Unity.Samples.Behaviors.Content;

public sealed class PointerInputDemoState
{
    public static readonly string[] Ids = { "raw", "passes", "consume", "tap", "double", "long", "press", "drag", "horizontal", "vertical", "hold-drag", "transform", "pan-lock", "keys", "latest", "lifetime", "siblings", "timeout" };
    private readonly Dictionary<string, PointerInputDemoCase> _cases = new();
    public PointerInputDemoCase this[string id] => _cases[id];
    public PointerInputDemoState() { foreach (var id in Ids) _cases[id] = new PointerInputDemoCase(id); }
}

public sealed class PointerInputDemoCase
{
    public readonly string Id;
    public readonly IMutableState<int> Revision = MutableStateOf(0);
    public readonly IMutableState<int> Key = MutableStateOf(0);
    public readonly IMutableState<bool> Visible = MutableStateOf(true);
    public readonly IMutableState<bool> Enabled = MutableStateOf(true);
    public readonly IMutableState<int> Step = MutableStateOf(1);
    public int Events, Taps, DoubleTaps, LongPresses, Starts, Ends, Cancels, Runs, Stops, Presses, Releases;
    public Offset Pan;
    public float Zoom = 1, Rotation;
    public string Status = "Ready";
    public readonly List<string> Trace = new();
    public PointerInputDemoCase(string id) => Id = id;
    public void Show(string status) { Status = status; Revision.Value++; }
    public void Log(string line)
    {
        Trace.Add(line); if (Trace.Count > 6) Trace.RemoveAt(0);
        Show(string.Join("\n", Trace));
    }
    public void Tap(Offset _) { Taps += Step.Value; Show($"Tap {Taps} · double {DoubleTaps} · hold {LongPresses}"); }
    public void DoubleTap(Offset _) { DoubleTaps++; Show($"Tap {Taps} · double {DoubleTaps} · hold {LongPresses}"); }
    public void LongPress(Offset _) { LongPresses++; Show($"Tap {Taps} · double {DoubleTaps} · hold {LongPresses}"); }
    public void Drag(IPointerInputChange _, Offset delta) { Pan += delta; Show($"Δ total ({Pan.X:F1}, {Pan.Y:F1})\nstart {Starts} · end {Ends} · cancel {Cancels}"); }
    public void Start(Offset _) { Starts++; Show("Dragging — move beyond the card"); }
    public void End() { Ends++; Show($"End {Ends} · ({Pan.X:F1}, {Pan.Y:F1})"); }
    public void Cancel() { Cancels++; Show($"Canceled {Cancels}"); }
    public void Transform(Offset centroid, Offset pan, float zoom, float rotation)
    { Pan += pan; Zoom *= zoom; Rotation += rotation; Show($"Center ({centroid.X:F0}, {centroid.Y:F0})\nPan ({Pan.X:F0}, {Pan.Y:F0})\nZoom {Zoom:F2} · rotation {Rotation:F1}°"); }
}
