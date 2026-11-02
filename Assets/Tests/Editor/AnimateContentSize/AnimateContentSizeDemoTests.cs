using System;
using System.Collections;
using UnityEngine.TestTools;
using System.Linq;
using Compose.Net;
using static Compose.Net.ComposeFunctions;
using Compose.Net.Unity.Samples.Behaviors.Content;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Compose.Net.Unity.Samples.Tests;

[AiGenerated]
public sealed partial class AnimateContentSizeDemoTests : UITestFixture
{
    private ComposeView _view;
    private ScrollView _scroll;
    private AnimateContentSizeDemoState _state;

    [SetUp]
    public void Mount()
    {
        panelSize = new Vector2(1100, 2100);
        simulate.timePerSimulatedFrameMs = 0;
        _state = new AnimateContentSizeDemoState();
        _view = new ComposeView(); _view.style.flexShrink = 0;
        _scroll = new ScrollView(ScrollViewMode.Vertical); _scroll.style.flexGrow = 1;
        _scroll.Add(_view); rootVisualElement.Add(_scroll);
        _view.SetContent(Content); Flush();
        Assert.That(_view.Q("animate-size-demo"), Is.Not.Null);
    }

    [Composable] private void Content() => AnimateContentSizeDemo.Screen(_state);
    [TearDown] public void Unmount() { _view?.Dispose(); simulate.ResetTimePerSimulatedFrameToDefault(); }

    private void Flush(int ms = 0)
    {
        ComposeInvalidatorHolder.ComposeInvalidator.Tick();
        simulate.FrameUpdateMs(ms);
        for (var i = 0; i < 3; i++)
        {
            ComposeInvalidatorHolder.ComposeInvalidator.Tick();
            simulate.FrameUpdateMs(0);
        }
    }
    private VisualElement Element(string tag) => _view.Q(tag) ?? throw new AssertionException("Missing " + tag);
    private VisualElement Target(string id) => Element("animate." + id + ".target");
    private IEnumerator Click(string id, string control) => ClickElement(Element("animate." + id + "." + control));
    private IEnumerator ClickElement(VisualElement element)
    {
        var picked = element.panel.Pick(element.worldBound.center);
        Assert.That(picked == element || element.Contains(picked) || (picked != null && picked.Contains(element)), Is.True,
            $"{element.name} is covered by {picked?.name}; bounds {element.worldBound}");
        simulate.MouseDown(element);
        for (var i = 0; i < 2; i++) { yield return null; Flush(); }
        simulate.MouseUp(element);
        for (var i = 0; i < 2; i++) { yield return null; Flush(); }
    }
    private static void SizeIs(VisualElement element, float width, float height)
    {
        Assert.That(element.layout.width, Is.EqualTo(width).Within(1));
        Assert.That(element.layout.height, Is.EqualTo(height).Within(1));
    }

    [UnityTest]
    public IEnumerator AllFifteenCasesMountWithUniqueTagsAndNoInitialCallbacks()
    {
        yield return null; Flush();
        Assert.That(_state.Cases.Length, Is.EqualTo(15));
        foreach (var item in _state.Cases)
        {
            Assert.That(_view.Query<VisualElement>("animate." + item.Id + ".target").ToList().Count, Is.EqualTo(1));
            Assert.That(item.CompletedA.Value + item.CompletedB.Value, Is.Zero, item.Id);
            Assert.That(Target(item.Id).layout.width, Is.GreaterThan(0), item.Id);
            Assert.That(Target(item.Id).layout.height, Is.GreaterThan(0), item.Id);
        }
    }

    [UnityTest] public IEnumerator DefaultRecipeRespondsToButtonsAndContent() => SizeButtonsExercise("default");
    [UnityTest] public IEnumerator WidthRecipeRespondsToButtonsAndContent() => SizeButtonsExercise("width");
    [UnityTest] public IEnumerator HeightRecipeRespondsToButtonsAndContent() => SizeButtonsExercise("height");
    [UnityTest] public IEnumerator BothRecipeRespondsToButtonsAndContent() => SizeButtonsExercise("both");
    [UnityTest] public IEnumerator DelayRecipeRespondsToButtonsAndContent() => SizeButtonsExercise("delay");
    [UnityTest] public IEnumerator SnapRecipeRespondsToButtonsAndContent() => SizeButtonsExercise("snap");
    [UnityTest] public IEnumerator AlignmentRecipeRespondsToButtonsAndContent() => SizeButtonsExercise("alignment");
    [UnityTest] public IEnumerator InterruptRecipeRespondsToButtonsAndContent() => SizeButtonsExercise("interrupt");
    [UnityTest] public IEnumerator ParametersRecipeRespondsToButtonsAndContent() => SizeButtonsExercise("parameters");
    [UnityTest] public IEnumerator ModifierRecipeRespondsToButtonsAndContent() => SizeButtonsExercise("modifier");
    [UnityTest] public IEnumerator ContentRecipeRespondsToButtonsAndContent() => SizeButtonsExercise("content");
    [UnityTest] public IEnumerator NeighborsRecipeRespondsToButtonsAndContent() => SizeButtonsExercise("neighbors");
    [UnityTest] public IEnumerator RecomposeRecipeRespondsToButtonsAndContent() => SizeButtonsExercise("recompose");

    private IEnumerator SizeButtonsExercise(string id)
    {
        yield return Click(id, "large"); Flush(4000);
        var size = _state[id].TargetSize;
        SizeIs(Target(id), size.Width, size.Height);
        Assert.That(_state[id].Target.Value, Is.EqualTo(2));
        yield return ClickElement(Element("animate." + id + ".content")); Flush(); Flush(4000);
        SizeIs(Target(id), 96, 48);
        Assert.That(_state[id].Target.Value, Is.Zero);
        if (id != "default") Assert.That(_state[id].CompletedA.Value, Is.EqualTo(2));
    }

    [UnityTest]
    public IEnumerator WidthAndHeightCasesAnimateOnlyTheirSelectedAxis()
    {
        yield return Click("width", "large"); yield return Click("height", "large"); Flush(500);
        SizeIs(Target("width"), 156, 48); SizeIs(Target("height"), 96, 84);
        Assert.That(_state["width"].CompletedA.Value + _state["height"].CompletedA.Value, Is.Zero);
    }

    [UnityTest]
    public IEnumerator DefaultOverloadUsesTheLibraryDefaultSpec()
    {
        yield return Click("default", "large");
        var duration = (int)AnimationSpec.Default.TotalDurationMillis;
        Flush(duration / 2);
        Assert.That(Target("default").layout.width, Is.InRange(97, 215));
        Flush(duration - duration / 2);
        SizeIs(Target("default"), 216, 120);
    }

    [UnityTest]
    public IEnumerator CallbackOverloadReportsInitialAndTargetAndDoesNotRepeat()
    {
        yield return Click("both", "large"); Flush(1000);
        Assert.That(Element("animate.both.result").GetType().GetProperty("text").GetValue(Element("animate.both.result")),
            Is.EqualTo("96 × 48 → 216 × 120"));
        Flush(5000); Assert.That(_state["both"].CompletedA.Value, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator DelayedAndInstantRecipesHaveDifferentTimelines()
    {
        yield return Click("delay", "large"); yield return Click("snap", "large");
        SizeIs(Target("snap"), 216, 120); Assert.That(_state["snap"].CompletedA.Value, Is.EqualTo(1));
        Flush(499); SizeIs(Target("delay"), 96, 48);
        Flush(501); SizeIs(Target("delay"), 156, 84);
        Flush(500); Assert.That(_state["delay"].CompletedA.Value, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator AlignmentButtonCyclesAllNineAnchorsWithoutStartingAnimations()
    {
        for (var i = 1; i <= 9; i++)
        {
            yield return Click("alignment", "align");
            Assert.That(_state["alignment"].AlignmentIndex.Value, Is.EqualTo(i % 9));
            Assert.That(_state["alignment"].CompletedA.Value, Is.Zero);
        }
    }

    [UnityTest]
    public IEnumerator AnchorCanChangeDuringMotionWithoutResettingTheTimeline()
    {
        yield return Click("alignment", "large"); Flush(500);
        for (var i = 0; i < 4; i++) yield return Click("alignment", "align");
        var content = Element("animate.alignment.content").parent;
        Assert.That(content.layout.x, Is.EqualTo(-30).Within(1));
        Assert.That(content.layout.y, Is.EqualTo(-18).Within(1));
        Flush(500); Assert.That(_state["alignment"].CompletedA.Value, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator RapidRetargetingCancelsOldCallbacksAndReportsInterruptedSize()
    {
        yield return Click("interrupt", "large"); Flush(500);
        yield return Click("interrupt", "small"); Flush(250);
        yield return Click("interrupt", "medium"); Flush(1000);
        SizeIs(Target("interrupt"), 156, 84);
        Assert.That(_state["interrupt"].CompletedA.Value, Is.EqualTo(1));
        Assert.That(_state["interrupt"].LastResult.Value, Is.EqualTo("141 × 75 → 156 × 84"));
    }

    [UnityTest]
    public IEnumerator SpeedAndListenerControlsUpdateTheRunningNodeInPlace()
    {
        yield return Click("parameters", "large"); Flush(500);
        yield return Click("parameters", "speed"); yield return Click("parameters", "listener"); Flush(500);
        Assert.That(_state["parameters"].CompletedA.Value, Is.Zero);
        Assert.That(_state["parameters"].CompletedB.Value, Is.EqualTo(1));
        yield return Click("parameters", "small"); Flush(1500);
        SizeIs(Target("parameters"), 156, 84);
        Flush(1500); Assert.That(_state["parameters"].CompletedB.Value, Is.EqualTo(2));
    }

    [UnityTest]
    public IEnumerator ModifierCanBeRemovedDuringMotionAndReattachedWithoutAnEnterAnimation()
    {
        yield return Click("modifier", "large"); Flush(500); yield return Click("modifier", "enabled");
        SizeIs(Target("modifier"), 216, 120);
        Assert.That(_state["modifier"].CompletedA.Value, Is.Zero);
        yield return Click("modifier", "small"); SizeIs(Target("modifier"), 96, 48);
        yield return Click("modifier", "enabled"); Flush(1500);
        Assert.That(_state["modifier"].CompletedA.Value, Is.Zero);
        yield return Click("modifier", "large"); Flush(1000);
        Assert.That(_state["modifier"].CompletedA.Value, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator RemovingAndRestoringContentAnimatesThroughAnEmptyContainer()
    {
        yield return Click("content", "visible"); Flush(1000);
        Assert.That(_view.Q("animate.content.content"), Is.Null);
        SizeIs(Target("content"), 0, 0);
        yield return Click("content", "visible"); Flush(1000);
        SizeIs(Target("content"), 96, 48);
        Assert.That(_state["content"].CompletedA.Value, Is.EqualTo(2));
    }

    [UnityTest] public IEnumerator TextIsMeasuredAndAnimatesBothDirections() => IntrinsicContent("text");
    [UnityTest] public IEnumerator DynamicListIsMeasuredAndAnimatesBothDirections() => IntrinsicContent("list");
    private IEnumerator IntrinsicContent(string id)
    {
        var initial = Target(id).layout.size;
        yield return Click(id, "large"); Flush(500);
        Assert.That(Target(id).layout.height, Is.GreaterThan(initial.y));
        var half = Target(id).layout.height; Flush(500);
        Assert.That(Target(id).layout.height, Is.GreaterThan(half));
        if (id == "list") Assert.That(_view.Q("animate.list.row-2"), Is.Not.Null);
        yield return Click(id, "small"); Flush(1000); SizeIs(Target(id), initial.x, initial.y);
        Assert.That(_state[id].CompletedA.Value, Is.EqualTo(2));
    }

    [UnityTest]
    public IEnumerator FollowingSiblingMovesByTheAnimatedHeight()
    {
        var before = Element("animate.neighbors.neighbor").worldBound.y;
        yield return Click("neighbors", "large"); Flush(500);
        Assert.That(Element("animate.neighbors.neighbor").worldBound.y - before, Is.EqualTo(36).Within(1));
        Flush(500);
        Assert.That(Element("animate.neighbors.neighbor").worldBound.y - before, Is.EqualTo(72).Within(1));
    }

    [UnityTest]
    public IEnumerator PrefixAndUnchangedTargetRecompositionsPreserveAnimationAndDoNotDuplicateCallbacks()
    {
        yield return Click("recompose", "large"); Flush(500);
        yield return Click("recompose", "prefix"); yield return Click("recompose", "same"); Flush(500);
        SizeIs(Target("recompose"), 216, 120);
        Assert.That(_state["recompose"].CompletedA.Value, Is.EqualTo(1));
        yield return Click("recompose", "same"); Flush(1000);
        Assert.That(_state["recompose"].CompletedA.Value, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator ExpandAndCollapseAllKeepIndependentTargetsAndCallbacks()
    {
        yield return ClickElement(Element("animate.expand-all")); Flush(); Flush(4000);
        foreach (var item in _state.Cases)
        {
            Assert.That(item.Target.Value, Is.EqualTo(2));
            if (item.Id != "default") Assert.That(item.CompletedA.Value, Is.EqualTo(1), item.Id);
        }
        yield return ClickElement(Element("animate.collapse-all")); Flush(); Flush(4000);
        foreach (var item in _state.Cases)
        {
            Assert.That(item.Target.Value, Is.Zero);
            if (item.Id != "default") Assert.That(item.CompletedA.Value, Is.EqualTo(2), item.Id);
        }
    }

    [UnityTest]
    public IEnumerator ClippedContentCannotReceiveClicksOutsideAnimatedBounds()
    {
        yield return Click("both", "large"); Flush(500);
        var target = Target("both");
        Assert.That(target.resolvedStyle.overflow, Is.EqualTo(Overflow.Hidden));
        var point = new Vector2(target.worldBound.xMax + 20, target.worldBound.center.y);
        var picked = target.panel.Pick(point);
        Assert.That(picked == target || target.Contains(picked), Is.False);
        simulate.MouseDown(point); yield return null;
        simulate.MouseUp(point); yield return null; Flush();
        Assert.That(_state["both"].Target.Value, Is.EqualTo(2));
    }

    [UnityTest]
    public IEnumerator SmallViewportCanScrollToAndOperateTheLastRecipe()
    {
        panelSize = new Vector2(1100, 600); Flush();
        var last = Element("animate.recompose.large");
        Assert.That(last.worldBound.yMin, Is.GreaterThan(_scroll.contentViewport.worldBound.yMax));
        simulate.ScrollWheel(new Vector2(0, 500), new Vector2(600, 400));
        for (var i = 0; i < 120 && last.worldBound.yMax > _scroll.contentViewport.worldBound.yMax; i++)
        { yield return null; Flush(16); }
        Assert.That(last.worldBound.yMin, Is.GreaterThanOrEqualTo(_scroll.contentViewport.worldBound.yMin));
        Assert.That(last.worldBound.yMax, Is.LessThanOrEqualTo(_scroll.contentViewport.worldBound.yMax));
        yield return Click("recompose", "large"); Flush(1000);
        Assert.That(_state["recompose"].Target.Value, Is.EqualTo(2));
        SizeIs(Target("recompose"), 216, 120);
    }

    [UnityTest]
    public IEnumerator DisposalCancelsAllAnimationsAndRemountStartsWithCleanState()
    {
        yield return Click("both", "large"); Flush(500); var previous = _state;
        _view.Dispose(); Flush(2000);
        Assert.That(previous["both"].CompletedA.Value, Is.Zero);
        _state = new AnimateContentSizeDemoState(); _view.SetContent(Content); Flush();
        yield return Click("both", "large"); Flush(1000);
        Assert.That(_state["both"].CompletedA.Value, Is.EqualTo(1));
        Assert.That(previous["both"].CompletedA.Value, Is.Zero);
    }
}
