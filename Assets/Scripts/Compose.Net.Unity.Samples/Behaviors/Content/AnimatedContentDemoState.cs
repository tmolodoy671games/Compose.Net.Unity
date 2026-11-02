using System;
using System.Collections.Generic;
using Compose.Net;
using static Compose.Net.ComposeFunctions;

namespace Compose.Net.Unity.Samples.Behaviors.Content;

public sealed class AnimatedContentDemoState
{
    public readonly AnimatedContentDemoCase[] Cases =
    {
        new("default", "Default fade through", "90 ms exit; delayed 220 ms fade + scale."),
        new("fade", "Independent durations", "500 ms exit, 1000 ms enter. A/B/C overlap."),
        new("horizontal", "Horizontal container slide", "Different content widths; slide to the left."),
        new("vertical", "Vertical container slide", "Different heights; slide upwards."),
        new("direction", "Increment / decrement", "Direction follows InitialState → TargetState."),
        new("size", "Container size", "1000 ms size interpolation; following sibling moves."),
        new("unclipped", "Unclipped size transform", "Content may draw outside the animated bounds."),
        new("no-size", "SizeTransform = null", "No size interpolation or automatic clipping."),
        new("alignment", "All nine alignments", "Cycle the anchor during an active transition."),
        new("z-order", "Content z-index", "Earlier pages stay on top while leaving."),
        new("key", "Stable contentKey", "Refresh updates data while preserving remembered state."),
        new("interrupt", "Rapid retarget / reverse", "Press A, B, C at any point during motion."),
        new("keep", "KeepUntilTransitionsFinished", "Outgoing page survives until the full transition ends."),
        new("child", "Child AnimateEnterExit", "Child exit lasts 1500 ms, longer than its parent."),
        new("none", "No enter / exit", "Immediate replacement with no visual transition."),
        new("empty", "Empty content", "C has no content; animate to and from zero size."),
        new("transition", "Transition.AnimatedContent", "A shared transition exposes current / target / running."),
        new("recompose", "Unchanged target", "Recompose during motion without restarting it."),
        new("expand", "Expand / shrink", "Clip from the center; reverse at any point."),
        new("size-only", "Size-only transition", "No enter or exit; the container still animates."),
        new("delay", "Delayed enter / exit", "400 ms enter delay; 300 ms exit delay.")
    };
    public AnimatedContentDemoCase this[string id] => Array.Find(Cases, item => item.Id == id)!;
    public void SelectAll(int page) { foreach (var item in Cases) item.Select(page); }
}

public sealed class AnimatedContentDemoCase
{
    public readonly string Id, Title, Description;
    public readonly IMutableState<int> Target = MutableStateOf(0), Revision = MutableStateOf(0), AlignmentIndex = MutableStateOf(0);
    public readonly Dictionary<int, object> Remembered = new();
    public readonly HashSet<object> Active = new();
    public int Disposals;
    public int InteractionCount { get; private set; }
    public Transition<int>? Transition;
    public readonly List<(int Initial, int Target)> Segments = new();
    public AnimatedContentDemoCase(string id, string title, string description) { Id = id; Title = title; Description = description; }
    public void Select(int page) { InteractionCount++; Target.Value = page * 10; }
    public void Refresh() { InteractionCount++; if (Id == "key") Target.Value++; else Revision.Value++; }
    public void Align() { InteractionCount++; AlignmentIndex.Value = (AlignmentIndex.Value + 1) % Alignments.Length; }
    public static readonly Alignment[] Alignments = { Alignment.TopStart, Alignment.TopCenter, Alignment.TopEnd,
        Alignment.CenterStart, Alignment.Center, Alignment.CenterEnd, Alignment.BottomStart, Alignment.BottomCenter, Alignment.BottomEnd };
    public static readonly string[] AlignmentNames = { "Top start", "Top center", "Top end", "Center start", "Center", "Center end", "Bottom start", "Bottom center", "Bottom end" };
}
