using System;
using System.Collections.Generic;
using Compose.Net;
using static Compose.Net.ComposeFunctions;

namespace Compose.Net.Unity.Samples.Behaviors.Content;

public sealed class SharedLayoutDemoState
{
    public readonly SharedLayoutCase[] Cases =
    {
        new("element", "SharedElement", "Only incoming content renders; shared geometry, no fade."),
        new("bounds", "SharedBounds", "Different content shares bounds and crossfades."),
        new("remeasure", "RemeasureToBounds", "Native layout reflows text at each animated size."),
        new("fill-width", "Scale · FillWidth", "Default: stable layout, uniform scale, centered."),
        new("fit", "Scale · Fit", "Keep all content visible inside the shared bounds."),
        new("crop", "Scale · Crop", "Fill the bounds; explicit rounded overlay clip."),
        new("fill-bounds", "Scale · FillBounds", "Independent horizontal and vertical scale."),
        new("fill-height", "Scale · FillHeight", "Uniform scale using the animated height."),
        new("inside", "Scale · Inside", "Shrink content to fit; never enlarge it."),
        new("no-scale", "Scale · None", "Original size aligned to the bottom right."),
        new("placeholder", "AnimatedSize placeholder", "The following sibling follows the animated height."),
        new("local", "Overlay disabled", "Content inherits parent fade and clipping."),
        new("clip", "Rounded overlay clip", "Native overflow clipping at the moving bounds."),
        new("nested", "Nested shared content", "Child moves independently and inherits parent clip."),
        new("z-order", "Overlay z-index", "Two crossing elements with explicit layer order."),
        new("unmatched", "Unmatched keys", "Different keys use ordinary parent transitions."),
        new("disabled", "Disabled matching", "SharedContentConfig disables sharing."),
        new("interrupt", "Interrupt / reverse", "Press A/B/C during motion; positions stay continuous."),
        new("retarget", "Moving target", "Resize changes the destination during the transition."),
        new("caller", "Caller managed visibility", "Both layouts persist; only the visible element draws."),
        new("delay", "Independent timing", "Bounds delay 300 ms; enter 700 ms; exit 200 ms."),
        new("instant", "Zero duration", "Immediate placement and cleanup."),
        new("spring", "Default spring", "Critically damped bounds spring preserves momentum."),
        new("recompose", "Remembered identity", "Refresh recomposes content without restarting motion."),
        new("visibility", "AnimatedVisibility scopes", "Two independent visibility containers share the same key."),
        new("empty", "Zero-size destination", "Animate to an empty layout, then restore it."),
        new("custom-placeholder", "Custom placeholder", "Reserve the larger width while the content moves.")
    };
    public SharedLayoutCase this[string id] => Array.Find(Cases, item => item.Id == id)!;
    public void SelectAll(int page) { foreach (var item in Cases) item.Select(page); }
}

public sealed class SharedLayoutCase
{
    public readonly string Id, Title, Description;
    public readonly IMutableState<int> Page = MutableStateOf(0), Revision = MutableStateOf(0);
    public readonly IMutableState<bool> Large = MutableStateOf(false);
    public SharedTransitionScope? Scope;
    public readonly Dictionary<int, SharedContentState> States = new();
    public readonly HashSet<int> Active = new();
    public int Interactions;
    public int ParentDuration = 180;
    public readonly IMutableState<bool> Enabled = MutableStateOf(true);
    public bool KeepEnabledForOngoingAnimation = true;
    public SharedLayoutCase(string id, string title, string description) { Id = id; Title = title; Description = description; Enabled.Value = id != "disabled"; }
    public void Select(int page) { Interactions++; Page.Value = page; }
    public void Refresh() { Interactions++; if (Id == "disabled") Enabled.Value = !Enabled.Value; else if (Id == "retarget") Large.Value = !Large.Value; else Revision.Value++; }
}
