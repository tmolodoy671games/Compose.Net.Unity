using System;
using Compose.Net;
using static Compose.Net.ComposeFunctions;

namespace Compose.Net.Unity.Samples.Behaviors.Content;

public enum AnimateSizeScenario
{
    Default, Width, Height, Both, Delay, Snap, Alignment, Interrupt,
    Parameters, Modifier, Content, Text, List, Neighbors, Recompose
}

[AiGenerated]
public sealed class AnimateContentSizeDemoState
{
    public readonly AnimateContentSizeDemoCase[] Cases =
    {
        new("default", "01 / Default", "AnimateContentSize() · click the blue content", AnimateSizeScenario.Default),
        new("width", "02 / Width only", "Height stays fixed · linear, 1000 ms", AnimateSizeScenario.Width),
        new("height", "03 / Height only", "Width stays fixed · linear, 1000 ms", AnimateSizeScenario.Height),
        new("both", "04 / Both axes + callback", "Initial → target size after completion", AnimateSizeScenario.Both),
        new("delay", "05 / Delayed start", "500 ms pause, then 1000 ms motion", AnimateSizeScenario.Delay),
        new("snap", "06 / Zero duration", "Immediate layout change · one callback", AnimateSizeScenario.Snap),
        new("alignment", "07 / All nine alignments", "Content stays anchored while bounds change", AnimateSizeScenario.Alignment),
        new("interrupt", "08 / Interrupt / reverse", "Choose another size before motion finishes", AnimateSizeScenario.Interrupt),
        new("parameters", "09 / Update parameters", "New speed applies to the next animation", AnimateSizeScenario.Parameters),
        new("modifier", "10 / Remove the modifier", "Turn animation off, resize, then reattach", AnimateSizeScenario.Modifier),
        new("content", "11 / Remove the content", "Empty content and later insertion", AnimateSizeScenario.Content),
        new("text", "12 / Intrinsic text size", "One line ↔ four lines, measured by UI Toolkit", AnimateSizeScenario.Text),
        new("list", "13 / Dynamic children", "One, two or three rows in a Column", AnimateSizeScenario.List),
        new("neighbors", "14 / Parent layout", "The following sibling moves with the bounds", AnimateSizeScenario.Neighbors),
        new("recompose", "15 / Recomposition", "Change a prefix or repeat the current target", AnimateSizeScenario.Recompose),
    };
    public readonly Action ExpandAll, CollapseAll;
    public AnimateContentSizeDemoState()
    {
        ExpandAll = () => { foreach (var item in Cases) item.Large(); };
        CollapseAll = () => { foreach (var item in Cases) item.Small(); };
    }
    public AnimateContentSizeDemoCase this[string id] => Array.Find(Cases, item => item.Id == id)
        ?? throw new ArgumentException("Unknown demo case: " + id);
}

[AiGenerated]
public sealed class AnimateContentSizeDemoCase
{
    public static readonly Alignment[] Alignments =
    {
        Alignment.TopStart, Alignment.TopCenter, Alignment.TopEnd,
        Alignment.CenterStart, Alignment.Center, Alignment.CenterEnd,
        Alignment.BottomStart, Alignment.BottomCenter, Alignment.BottomEnd
    };
    public static readonly string[] AlignmentNames =
    {
        "TopStart", "TopCenter", "TopEnd", "CenterStart", "Center", "CenterEnd",
        "BottomStart", "BottomCenter", "BottomEnd"
    };
    public readonly string Id, Title, Description;
    public readonly AnimateSizeScenario Scenario;
    public readonly IMutableState<int> Target = MutableStateOf(0);
    public readonly IMutableState<int> AlignmentIndex = MutableStateOf(0);
    public readonly IMutableState<bool> AnimationEnabled = MutableStateOf(true);
    public readonly IMutableState<bool> Visible = MutableStateOf(true);
    public readonly IMutableState<bool> Slow = MutableStateOf(false);
    public readonly IMutableState<bool> AlternateListener = MutableStateOf(false);
    public readonly IMutableState<bool> Decorated = MutableStateOf(false);
    public readonly IMutableState<int> Revision = MutableStateOf(0);
    public readonly IMutableState<int> CompletedA = MutableStateOf(0), CompletedB = MutableStateOf(0);
    public readonly IMutableState<string> LastResult = MutableStateOf("No completion yet");
    public readonly Action Small, Medium, Large, Toggle, NextAlignment, ToggleSpec,
        ToggleListener, ToggleModifier, ToggleVisible, ToggleDecoration, NoChange;
    public readonly Action<Size, Size> Completed, AlternativeCompleted;

    public AnimateContentSizeDemoCase(string id, string title, string description, AnimateSizeScenario scenario)
    {
        Id = id; Title = title; Description = description; Scenario = scenario;
        Small = () => Target.Value = 0;
        Medium = () => Target.Value = 1;
        Large = () => Target.Value = 2;
        Toggle = () => Target.Value = Target.Value == 0 ? 2 : 0;
        NextAlignment = () => AlignmentIndex.Value = (AlignmentIndex.Value + 1) % Alignments.Length;
        ToggleSpec = () => Slow.Value = !Slow.Value;
        ToggleListener = () => AlternateListener.Value = !AlternateListener.Value;
        ToggleModifier = () => AnimationEnabled.Value = !AnimationEnabled.Value;
        ToggleVisible = () => Visible.Value = !Visible.Value;
        ToggleDecoration = () => Decorated.Value = !Decorated.Value;
        NoChange = () => Revision.Value++;
        Completed = (a, b) => { CompletedA.Value++; Record(a, b); };
        AlternativeCompleted = (a, b) => { CompletedB.Value++; Record(a, b); };
    }

    public Size TargetSize => new(
        Scenario == AnimateSizeScenario.Height ? 96 : 96 + Target.Value * 60,
        Scenario == AnimateSizeScenario.Width ? 48 : 48 + Target.Value * 36);
    public AnimationSpec Spec => Scenario switch
    {
        AnimateSizeScenario.Default => AnimationSpec.Default,
        AnimateSizeScenario.Snap => Tween(0),
        AnimateSizeScenario.Delay => Tween(1000, 500, LinearEasing),
        AnimateSizeScenario.Parameters => Tween(Slow.Value ? 3000 : 1000, easing: EaseInOutEasing),
        _ => Tween(1000, easing: LinearEasing)
    };
    private void Record(Size a, Size b) => LastResult.Value =
        $"{a.Width:0.#} × {a.Height:0.#} → {b.Width:0.#} × {b.Height:0.#}";
}
