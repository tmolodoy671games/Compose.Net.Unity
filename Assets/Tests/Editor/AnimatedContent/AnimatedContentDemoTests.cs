using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Compose.Net;
using static Compose.Net.ComposeFunctions;
using Compose.Net.Unity.Packages.Compose.Net.Unity.Runtime.Adapters;
using Compose.Net.Unity.Samples.Behaviors.Content;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Compose.Net.Unity.Samples.Tests;

public sealed partial class AnimatedContentDemoTests : UITestFixture
{
    private SynchronizationContext? _previousContext;
    private readonly UiContext _context = new();
    private IComposer _composer = null!;
    private DemoAdapter _adapter = null!;
    private AnimatedContentDemoState _state = null!;
    [SetUp] public void Mount()
    {
        _previousContext = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(_context);
        panelSize = new Vector2(1100, 2500); simulate.timePerSimulatedFrameMs = 0;
        _adapter = new DemoAdapter(); _state = new AnimatedContentDemoState(); _composer = IComposer.Create();
        _probeState.Value = 0; _probeModifier = Modifier; _probeAlignment = Alignment.TopStart;
        _probeSpec = _ => IEnterTransition.None.TogetherWith(IExitTransition.None); _probeScopes.Clear();
        Bootstrap(_composer, _adapter, false, new ComposeConfig(Logging: false), new RootNode(rootVisualElement), Content);
        Flush();
    }
    [Composable] private void Content() => AnimatedContentDemo.Screen(_state);
    [TearDown] public void Unmount() { _composer?.Dispose(); _context.Drain(); SynchronizationContext.SetSynchronizationContext(_previousContext); simulate.ResetTimePerSimulatedFrameToDefault(); }
    private void Flush(int milliseconds = 0)
    {
        SynchronizationContext.SetSynchronizationContext(_context);
        _context.Drain();
        _adapter.Clock.Advance(milliseconds);
        for (var i = 0; i < 6; i++) { _context.Drain(); _adapter.Invalidator.Tick(); simulate.FrameUpdateMs(0); }
    }
    private VisualElement E(string tag) => rootVisualElement.Q(tag) ?? throw new AssertionException("Missing " + tag);
    private VisualElement Container(string id) => E("ac." + id + ".container");
    private VisualElement Page(string id, int value) => E("ac." + id + ".page-" + value);
    private VisualElement Motion(string id, int value) => Page(id, value).parent;
    private IEnumerator Click(string id, string control)
    {
        var element = E("ac." + id + "." + control);
        var before = _state[id].InteractionCount;
        var picked = element.panel.Pick(element.worldBound.center);
        Assert.That(picked == element || element.Contains(picked) || (picked != null && picked.Contains(element)), Is.True,
            $"Covered control {element.name}: {picked?.name} {element.worldBound}");
        simulate.MouseDown(element);
        for (var i = 0; i < 2; i++) { yield return null; Flush(); }
        simulate.MouseUp(element);
        for (var i = 0; i < 2; i++) { yield return null; Flush(); }
        for (var i = 0; i < 120 && _state[id].InteractionCount == before; i++) { yield return null; Flush(); }
        Assert.That(_state[id].InteractionCount, Is.EqualTo(before + 1), "Click was not delivered: " + element.name);
    }
    private void Only(string id, int page)
    {
        for (var i = 0; i < 3; i++) Assert.That(rootVisualElement.Q("ac." + id + ".page-" + i) != null,
            Is.EqualTo(i == page && !(id == "empty" && page == 2)), id + " page " + i);
        Assert.That(_state[id].Active.Count, Is.EqualTo(1), id + " active compositions");
    }
    private static void SizeIs(VisualElement element, float width, float height)
    {
        Assert.That(element.layout.width, Is.EqualTo(width).Within(1), element.name + " width");
        Assert.That(element.layout.height, Is.EqualTo(height).Within(1), element.name + " height");
    }
    private static float Alpha(VisualElement element) => element.resolvedStyle.opacity;
    private void Near(float a, float b) => Assert.That(a, Is.EqualTo(b).Within(.02));

    [UnityTest] public IEnumerator TwentyOneExamplesMountAtIntrinsicSizeWithoutInitialAnimations()
    {
        yield return null; Flush(); Assert.That(_state.Cases.Length, Is.EqualTo(21));
        foreach (var item in _state.Cases)
        {
            Only(item.Id, 0); SizeIs(Container(item.Id), 100, 48); Near(Alpha(Motion(item.Id, 0)), 1);
            Assert.That(item.Disposals, Is.Zero, item.Id);
        }
    }
    [UnityTest] public IEnumerator DefaultButtons() => Exercise("default");
    [UnityTest] public IEnumerator FadeButtons() => Exercise("fade");
    [UnityTest] public IEnumerator HorizontalButtons() => Exercise("horizontal");
    [UnityTest] public IEnumerator VerticalButtons() => Exercise("vertical");
    [UnityTest] public IEnumerator DirectionButtons() => Exercise("direction");
    [UnityTest] public IEnumerator SizeButtons() => Exercise("size");
    [UnityTest] public IEnumerator UnclippedButtons() => Exercise("unclipped");
    [UnityTest] public IEnumerator NoSizeButtons() => Exercise("no-size");
    [UnityTest] public IEnumerator AlignmentButtons() => Exercise("alignment");
    [UnityTest] public IEnumerator ZOrderButtons() => Exercise("z-order");
    [UnityTest] public IEnumerator KeyButtons() => Exercise("key");
    [UnityTest] public IEnumerator InterruptButtons() => Exercise("interrupt");
    [UnityTest] public IEnumerator KeepButtons() => Exercise("keep");
    [UnityTest] public IEnumerator ChildButtons() => Exercise("child");
    [UnityTest] public IEnumerator NoneButtons() => Exercise("none");
    [UnityTest] public IEnumerator EmptyButtons() => Exercise("empty");
    [UnityTest] public IEnumerator TransitionButtons() => Exercise("transition");
    [UnityTest] public IEnumerator RecomposeButtons() => Exercise("recompose");
    [UnityTest] public IEnumerator ExpandButtons() => Exercise("expand");
    [UnityTest] public IEnumerator SizeOnlyButtons() => Exercise("size-only");
    [UnityTest] public IEnumerator DelayButtons() => Exercise("delay");
    private IEnumerator Exercise(string id)
    {
        foreach (var pair in new[] { ("b",1), ("c",2), ("a",0) })
        {
            yield return Click(id, pair.Item1); Flush(2000); Only(id, pair.Item2);
            if (!(id == "empty" && pair.Item2 == 2)) SizeIs(Container(id), pair.Item2 == 0 ? 100 : pair.Item2 == 1 ? 220 : 150,
                pair.Item2 == 0 ? 48 : pair.Item2 == 1 ? 112 : 76);
        }
    }
    [UnityTest] public IEnumerator DefaultTimingsIncludeDelayAndScale()
    {
        yield return Click("default", "b");
        Near(Alpha(Motion("default",1)), 0); Near(Motion("default",1).resolvedStyle.scale.value.x, .92f);
        Flush(45); Assert.That(Alpha(Motion("default",0)), Is.InRange(.01f,.99f)); Near(Alpha(Motion("default",1)),0);
        Flush(45); Assert.That(rootVisualElement.Q("ac.default.page-0"), Is.Null);
        Flush(110); Assert.That(Alpha(Motion("default",1)), Is.InRange(.01f,.99f));
        Flush(110); Only("default",1); Near(Alpha(Motion("default",1)),1); Near(Motion("default",1).resolvedStyle.scale.value.x,1);
    }
    [UnityTest] public IEnumerator DifferentDurationsDisposeOnlyAfterExit()
    {
        yield return Click("fade","b"); Flush(250);
        Near(Alpha(Motion("fade",0)),.5f); Near(Alpha(Motion("fade",1)),.25f);
        Flush(250); Only("fade",1); Near(Alpha(Motion("fade",1)),.5f); Flush(500); Near(Alpha(Motion("fade",1)),1);
    }
    [UnityTest] public IEnumerator ReturnToOutgoingContentPreservesIdentityAndEveryRenderedProperty()
    {
        var original = _state["interrupt"].Remembered[0];
        yield return Click("interrupt","b"); Flush(240);
        var a = Motion("interrupt",0); var b = Motion("interrupt",1);
        var ap = a.resolvedStyle.translate; var bp = b.resolvedStyle.translate;
        var aa = Alpha(a); var ba = Alpha(b); var size = Container("interrupt").layout.size;
        yield return Click("interrupt","a");
        Assert.That(_state["interrupt"].Remembered[0], Is.SameAs(original));
        Near(Alpha(a),aa); Near(Alpha(b),ba); Near(a.resolvedStyle.translate.x,ap.x); Near(b.resolvedStyle.translate.x,bp.x);
        SizeIs(Container("interrupt"),size.x,size.y);
        Flush(1200); Only("interrupt",0); Near(Alpha(Motion("interrupt",0)),1); Near(Motion("interrupt",0).resolvedStyle.translate.x,0);
    }
    [UnityTest] public IEnumerator ThreeTargetsCoexistAndEveryOutgoingCompositionIsDisposed()
    {
        yield return Click("interrupt","b"); Flush(100); yield return Click("interrupt","c");
        Assert.That(_state["interrupt"].Active.Count, Is.EqualTo(3)); Flush(2000); Only("interrupt",2);
        Assert.That(_state["interrupt"].Disposals,Is.EqualTo(2));
    }
    [UnityTest] public IEnumerator ThirtyRetargetsDoNotLeakOrRestartTheFinalContent()
    {
        for (var i=0;i<30;i++) { yield return Click("interrupt",new[]{"a","b","c"}[i%3]); Flush(25); }
        Flush(2000); Only("interrupt",2); Near(Alpha(Motion("interrupt",2)),1);
        var identity=_state["interrupt"].Remembered[2]; yield return Click("interrupt","c"); Flush(1000);
        Assert.That(_state["interrupt"].Remembered[2],Is.SameAs(identity)); Only("interrupt",2);
    }
    [UnityTest] public IEnumerator SizeAndNeighborFollowTheInterpolatedDimensions()
    {
        var y=E("ac.size.neighbor").worldBound.y;
        yield return Click("size","b"); SizeIs(Container("size"),100,48); Flush(500);
        SizeIs(Container("size"),160,80); Near(E("ac.size.neighbor").worldBound.y-y,32);
        Flush(500); SizeIs(Container("size"),220,112); Near(E("ac.size.neighbor").worldBound.y-y,64);
    }
    [UnityTest] public IEnumerator SizeRetargetBeginsAtCurrentSize()
    {
        yield return Click("size","b"); Flush(500); yield return Click("size","a");
        SizeIs(Container("size"),160,80); Flush(500); SizeIs(Container("size"),130,64); Flush(500); SizeIs(Container("size"),100,48);
    }
    [UnityTest] public IEnumerator SameKeyUpdatesDataWithoutReplacingTheComposition()
    {
        var identity=_state["key"].Remembered[0]; yield return Click("key","refresh");
        Only("key",0); Assert.That(_state["key"].Remembered[0],Is.SameAs(identity));
        Assert.That(E("ac.key.page-0.text").GetType().GetProperty("text").GetValue(E("ac.key.page-0.text")),Is.EqualTo("A · 1"));
        Assert.That(_state["key"].Disposals,Is.Zero); Near(Alpha(Motion("key",0)),1);
    }
    [UnityTest] public IEnumerator SameKeyDuringEnterKeepsTheTimeline()
    {
        yield return Click("key","b"); Flush(250); var identity=_state["key"].Remembered[1];
        yield return Click("key","refresh"); Near(Alpha(Motion("key",1)),.25f);
        Flush(750); Only("key",1); Near(Alpha(Motion("key",1)),1); Assert.That(_state["key"].Remembered[1],Is.SameAs(identity));
    }
    [UnityTest] public IEnumerator RecompositionDoesNotReevaluateTheTransitionSegment()
    {
        yield return Click("recompose","b"); Flush(300); var count=_state["recompose"].Segments.Count;
        yield return Click("recompose","refresh"); Flush(700); Only("recompose",1); Near(Alpha(Motion("recompose",1)),1);
        Assert.That(_state["recompose"].Segments.Count,Is.EqualTo(count));
    }
    [UnityTest] public IEnumerator KeepUntilFinishedWaitsForIncomingAnimation()
    {
        yield return Click("keep","b"); Flush(500); Assert.That(_state["keep"].Active.Count,Is.EqualTo(2));
        Flush(500); Only("keep",1);
    }
    [UnityTest] public IEnumerator ParentWaitsForLongerChildExit()
    {
        yield return Click("child","b"); Flush(1000); Assert.That(_state["child"].Active.Count,Is.EqualTo(2));
        Flush(500); Only("child",1);
    }
    [UnityTest] public IEnumerator EmptyContentReachesZeroSizeAndCanReturn()
    {
        yield return Click("empty","c"); Flush(2000); Only("empty",2); SizeIs(Container("empty"),0,0);
        yield return Click("empty","a"); Flush(2000); Only("empty",0); SizeIs(Container("empty"),100,48);
    }
    [UnityTest] public IEnumerator NullSizeTransformAndNoTransitionsAreImmediate()
    {
        yield return Click("none","b"); Only("none",1); SizeIs(Container("none"),220,112);
        Assert.That(Container("none").resolvedStyle.overflow,Is.EqualTo(Overflow.Visible));
        yield return Click("no-size","b"); SizeIs(Container("no-size"),220,112);
    }
    [UnityTest] public IEnumerator ClippingAndUnclippedTransformsHaveDistinctOverflow()
    {
        yield return Click("size","b"); yield return Click("unclipped","b");
        Assert.That(Container("size").resolvedStyle.overflow,Is.EqualTo(Overflow.Hidden));
        Assert.That(Container("unclipped").resolvedStyle.overflow,Is.EqualTo(Overflow.Visible));
    }
    [UnityTest] public IEnumerator AllNineAlignmentsCanChangeDuringTheAnimation()
    {
        yield return Click("alignment","b"); Flush(250);
        for(var i=1;i<=9;i++)
        {
            yield return Click("alignment","align");
            var anchor=AnimatedContentDemoCase.Alignments[i%9]; var wrapper=Page("alignment",1).parent.parent;
            Near(wrapper.layout.x,(Container("alignment").contentRect.width-wrapper.layout.width)*AnimatedContentLayout.Bias(anchor,true));
            Near(wrapper.layout.y,(Container("alignment").contentRect.height-wrapper.layout.height)*AnimatedContentLayout.Bias(anchor,false));
        }
        Flush(750); Only("alignment",1);
    }
    [UnityTest] public IEnumerator ExternalTransitionReportsCurrentTargetAndRunning()
    {
        var t=_state["transition"].Transition!; Assert.That(t.CurrentState,Is.Zero); Assert.That(t.IsRunning,Is.False);
        yield return Click("transition","b"); Assert.That(t.TargetState,Is.EqualTo(10)); Assert.That(t.CurrentState,Is.Zero); Assert.That(t.IsRunning,Is.True);
        Flush(2000); Assert.That(t.CurrentState,Is.EqualTo(10)); Assert.That(t.IsRunning,Is.False);
    }
    [UnityTest] public IEnumerator DrawOrderUsesZIndexAndIncomingWinsTies()
    {
        yield return Click("z-order","b");
        Assert.That(Page("z-order",0).parent.parent.resolvedStyle.zIndex.value,Is.GreaterThan(Page("z-order",1).parent.parent.resolvedStyle.zIndex.value));
        yield return Click("fade","b");
        Assert.That(Container("fade").IndexOf(Page("fade",1).parent.parent),Is.GreaterThan(Container("fade").IndexOf(Page("fade",0).parent.parent)));
    }
    [UnityTest] public IEnumerator ContainerSlidesUseDifferentWidthsAndHeights()
    {
        yield return Click("horizontal","b"); yield return Click("vertical","b");
        Near(Motion("horizontal",1).resolvedStyle.translate.x,100);
        Near(Motion("vertical",1).resolvedStyle.translate.y,48);
        Flush(500); Assert.That(Motion("horizontal",0).resolvedStyle.translate.x,Is.LessThan(0));
        Assert.That(Motion("vertical",0).resolvedStyle.translate.y,Is.LessThan(0));
    }
    [UnityTest] public IEnumerator DisposeDuringTransitionReleasesAllCompositions()
    {
        yield return Click("interrupt","b"); Flush(200); _composer.Dispose(); Flush(2000);
        foreach(var item in _state.Cases) Assert.That(item.Active.Count,Is.Zero,item.Id);
    }
    [UnityTest] public IEnumerator ShrinkingWithoutSizeTransformKeepsTheLargestOutgoingContentUntilExit()
    {
        yield return Click("no-size", "b"); Flush(2000); yield return Click("no-size", "a");
        SizeIs(Container("no-size"), 220, 112); Flush(499); SizeIs(Container("no-size"), 220, 112);
        Flush(1); SizeIs(Container("no-size"), 100, 48);
    }
    [UnityTest] public IEnumerator ChildOpacityDoesNotJumpWhenParentIsReversed()
    {
        yield return Click("child", "b"); Flush(250);
        var a = E("ac.child.page-0.text"); var b = E("ac.child.page-1.text");
        var aa = Alpha(a); var ba = Alpha(b);
        yield return Click("child", "a"); Near(Alpha(a), aa); Near(Alpha(b), ba);
        Flush(2000); Only("child", 0); Near(Alpha(E("ac.child.page-0.text")), 1);
    }
    [UnityTest] public IEnumerator ChangingTargetEveryFrameKeepsTheContainerSizeContinuous()
    {
        for (var i = 0; i < 15; i++)
        {
            var before = Container("size").layout.size;
            yield return Click("size", new[] { "b", "c", "a" }[i % 3]);
            SizeIs(Container("size"), before.x, before.y); Flush(16);
        }
        Flush(2000); Only("size", 0); SizeIs(Container("size"), 100, 48);
    }
    [UnityTest] public IEnumerator ScaleRemainsContinuousWhenDefaultTransitionIsReversed()
    {
        yield return Click("default", "b"); Flush(160);
        var page = Motion("default", 1); var alpha = Alpha(page); var scale = page.resolvedStyle.scale.value.x;
        yield return Click("default", "a"); Near(Alpha(page), alpha); Near(page.resolvedStyle.scale.value.x, scale);
        Flush(2000); Only("default", 0);
    }
    [UnityTest] public IEnumerator ClickingContentSelectsTheNextPage()
    {
        var page = Page("fade", 0);
        simulate.MouseDown(page); yield return null; Flush();
        simulate.MouseUp(page); yield return null; Flush();
        Assert.That(_state["fade"].Target.Value, Is.EqualTo(10)); Flush(2000); Only("fade", 1);
    }
    [UnityTest] public IEnumerator AllControlsKeepIndependentCompositions()
    {
        var button = E("ac.all-b"); simulate.MouseDown(button); yield return null; Flush();
        simulate.MouseUp(button); yield return null; Flush(); Flush(2000);
        foreach (var item in _state.Cases) Only(item.Id, 1);
        button = E("ac.all-a"); simulate.MouseDown(button); yield return null; Flush();
        simulate.MouseUp(button); yield return null; Flush(); Flush(2000);
        foreach (var item in _state.Cases) Only(item.Id, 0);
    }
    [UnityTest] public IEnumerator SmallViewportScrollsToLastRecipeAndItStillResponds()
    {
        panelSize = new Vector2(1100, 600); Flush();
        var last = E("ac.delay.b");
        Assert.That(last.worldBound.yMin, Is.GreaterThan(rootVisualElement.worldBound.yMax));
        for (var i = 0; i < 120 && last.worldBound.yMax > rootVisualElement.worldBound.yMax; i++)
        {
            // Small wheel steps keep the control visible without jumping into native elastic overscroll.
            simulate.ScrollWheel(new Vector2(0, 5), new Vector2(600, 400));
            yield return null; Flush(16);
        }
        Assert.That(last.worldBound.yMin, Is.GreaterThanOrEqualTo(rootVisualElement.worldBound.yMin));
        Assert.That(last.worldBound.yMax, Is.LessThanOrEqualTo(rootVisualElement.worldBound.yMax));
        yield return Click("delay", "b"); Flush(2000); Only("delay", 1);
    }
    [UnityTest] public IEnumerator SizeTransitionsRestoreAuthoredMinimums()
    {
        var parent = new VisualElement { style = { flexShrink = 0 } };
        var element = new VisualElement { style = { width = 120, height = 80, minWidth = 40, minHeight = 30 } };
        parent.Add(element); rootVisualElement.Add(parent); Flush();
        var node = new UnityReusableComposeNode(element);
        var enter = _adapter.EnterTransitionsFactory.ExpandIn(Tween(1000), Alignment.Center, true, _ => new Size(0, 0));
        enter.Apply(node, TimeSpan.FromMilliseconds(250));
        Near(element.style.minWidth.value.value, 120); Near(element.style.minHeight.value.value, 80);
        enter.Revert(node, TimeSpan.FromMilliseconds(250));
        Near(element.style.minWidth.value.value, 40); Near(element.style.minHeight.value.value, 30);
        enter.Apply(node, TimeSpan.FromMilliseconds(1000));
        Near(element.style.minWidth.value.value, 40); Near(element.style.minHeight.value.value, 30);
        enter.Revert(node, TimeSpan.FromMilliseconds(1000));
        var exit = _adapter.ExitTransitionsFactory.ShrinkOut(Tween(1000), Alignment.Center, true, _ => new Size(0, 0));
        exit.Apply(node, TimeSpan.FromMilliseconds(250));
        Near(element.style.minWidth.value.value, 120); Near(element.style.minHeight.value.value, 80);
        exit.Revert(node, TimeSpan.FromMilliseconds(250));
        Near(element.style.minWidth.value.value, 40); Near(element.style.minHeight.value.value, 30);
        yield return null;
    }

    [UnityTest] public IEnumerator AnimatedContentSampleClicksSlideAndSettle()
    {
        var sample = typeof(AnimatedContentDemo).Assembly.GetType(
            "Compose.Net.Unity.Samples.Behaviors.Content.AnimatedContentSample", true)!;
        var layout = sample.GetMethod("Layout", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        _composer.Dispose(); rootVisualElement.Clear();
        Bootstrap(_composer, _adapter, false, new ComposeConfig(Logging: false), new RootNode(rootVisualElement),
            (Action)layout.CreateDelegate(typeof(Action)));
        Flush();
        var container = rootVisualElement.Query<VisualElement>().ToList()
            .Single(element => element.GetType().Name == "AnimatedContent");
        foreach (var pair in new[] { ("1", "10"), ("10", "100"), ("100", "1000") })
        {
            var initialSize = container.layout.size;
            var outgoing = rootVisualElement.Query<TextElement>().ToList().Single(text => text.text == pair.Item1);
            simulate.MouseDown(outgoing); yield return null; Flush();
            simulate.MouseUp(outgoing); yield return null; Flush();
            for (var i = 0; i < 120 && rootVisualElement.Query<TextElement>().ToList().All(text => text.text != pair.Item2); i++)
            { yield return null; Flush(); }
            var incoming = rootVisualElement.Query<TextElement>().ToList().Single(text => text.text == pair.Item2);
            var motion = incoming.parent;
            Assert.That(motion.parent.style.position.value, Is.EqualTo(Position.Absolute));
            Near(motion.parent.layout.y, outgoing.parent.parent.layout.y);
            var textStyle = incoming.resolvedStyle;
            var targetSize = incoming.layout.size + new Vector2(
                32 + textStyle.marginLeft + textStyle.marginRight,
                16 + textStyle.marginTop + textStyle.marginBottom); // Sample padding and text margins.
            for (var elapsed = 25; elapsed <= 1000; elapsed += 25)
            {
                Flush(25); simulate.FrameUpdateMs(25); Flush();
                if (elapsed % 250 == 0)
                {
                    var progress = Tween(1000).GetProgress(elapsed);
                    var expected = Vector2.LerpUnclamped(initialSize, targetSize, progress);
                    SizeIs(container, expected.x, expected.y);
                    Near(motion.resolvedStyle.translate.y, motion.layout.height * (1 - progress));
                }
                if (elapsed == 500)
                    Near(outgoing.parent.resolvedStyle.translate.y, -outgoing.parent.layout.height / 2);
            }
            Assert.That(rootVisualElement.Query<TextElement>().ToList().Select(text => text.text), Is.EqualTo(new[] { pair.Item2 }));
            Near(motion.resolvedStyle.translate.y, 0);
            Assert.That(incoming.layout.width, Is.GreaterThan(0));
            Assert.That(incoming.layout.height, Is.GreaterThan(0));
            // The container must already be settled when the slide ends, with no resize tail.
            SizeIs(container, targetSize.x, targetSize.y);
            Flush(500); simulate.FrameUpdateMs(500); Flush();
            SizeIs(container, targetSize.x, targetSize.y);
        }
    }

    [UnityTest] public IEnumerator SizeModifierPreservesAnimatedContentMeasurementAndPlacement()
    {
        _probeModifier = Modifier.Padding(horizontal: 16.Dp(), vertical: 8.Dp()).Clip()
            .AnimateContentSize(Tween(1000, easing: LinearEasing));
        _probeSpec = _ => SlideInVertically(Tween(1000, easing: LinearEasing), height => height)
            .TogetherWith(SlideOutVertically(Tween(1000, easing: LinearEasing), height => -height))
            .Using(SizeTransform(sizeAnimationSpec: (_, _) => Tween(1000, easing: LinearEasing)));
        RemountProbe();
        _probeState.Value = 1; Flush();
        var outgoing = E("probe-0").parent.parent;
        var incoming = E("probe-1").parent.parent;
        Assert.That(outgoing.style.position.value, Is.EqualTo(Position.Absolute), "Outgoing content must overlap the target");
        Assert.That(incoming.style.position.value, Is.EqualTo(Position.Absolute), "Incoming content must be placed by AnimatedContent");
        Near(outgoing.layout.y, incoming.layout.y);
        Flush(250);
        Near(E("probe-0").parent.resolvedStyle.translate.y, -12);
        Near(E("probe-1").parent.resolvedStyle.translate.y, 84);
        SizeIs(incoming.parent, 130, 64);
        Flush(750);
        Assert.That(rootVisualElement.Q("probe-0"), Is.Null);
        SizeIs(incoming.parent, 220, 112);
        yield return null;
    }

    private readonly IMutableState<int?> _probeState = MutableStateOf<int?>(0);
    private IModifier _probeModifier = Modifier;
    private Alignment _probeAlignment = Alignment.TopStart;
    private Func<AnimatedContentTransitionScope<int?>, ContentTransform> _probeSpec = _ => IEnterTransition.None.TogetherWith(IExitTransition.None);
    private readonly Dictionary<string, AnimatedContentScope> _probeScopes = new();
    private void RemountProbe()
    {
        _composer.Dispose(); rootVisualElement.Clear();
        Bootstrap(_composer, _adapter, false, new ComposeConfig(Logging: false), new RootNode(rootVisualElement), Probe);
        Flush();
    }
    [Composable] private void Probe()
    {
        AnimatedContent(_probeState.Value, modifier: _probeModifier.TestTag("probe"), contentAlignment: _probeAlignment,
            transitionSpec: _probeSpec, label: "Probe label", content: (scope, value) =>
            {
                _probeScopes[value?.ToString() ?? "null"] = scope;
                Box(modifier: Modifier.Size((value == 0 ? 100 : 220).Dp(), (value == 0 ? 48 : 112).Dp())
                    .TestTag("probe-" + (value?.ToString() ?? "null")), content: () => { });
            });
    }
    [UnityTest] public IEnumerator AllSixContainerSlideDirectionsAndCustomOffsetAreMeasured()
    {
        foreach (SlideDirection direction in Enum.GetValues(typeof(SlideDirection)))
        {
            _probeState.Value = 0;
            _probeSpec = scope => scope.SlideIntoContainer(direction, Tween(1000, easing: LinearEasing), offset => offset / 2)
                .TogetherWith(scope.SlideOutOfContainer(direction, Tween(1000, easing: LinearEasing), offset => offset / 2))
                .Using(SizeTransform(sizeAnimationSpec: (_, _) => Tween(1000, easing: LinearEasing)));
            RemountProbe(); _probeState.Value = 1; Flush();
            var motion = E("probe-1").parent.resolvedStyle.translate;
            var expected = direction is SlideDirection.Left or SlideDirection.Start ? 50
                : direction is SlideDirection.Right or SlideDirection.End ? -110
                : direction == SlideDirection.Up ? 24 : -56;
            Near(direction is SlideDirection.Up or SlideDirection.Down ? motion.y : motion.x, expected);
            Flush(1000); Assert.That(rootVisualElement.Q("probe-0"), Is.Null);
        }
        yield return null;
    }
    [UnityTest] public IEnumerator FixedContainerConstraintsControlAlignmentAndSlideDistance()
    {
        _probeModifier = Modifier.Size(300.Dp(), 200.Dp()); _probeAlignment = Alignment.Center;
        _probeSpec = scope => scope.SlideIntoContainer(SlideDirection.Left, Tween(1000)).TogetherWith(FadeOut(Tween(1000)));
        RemountProbe(); SizeIs(E("probe"),300,200);
        Near(E("probe-0").parent.parent.layout.x,100); Near(E("probe-0").parent.parent.layout.y,76);
        _probeState.Value=1; Flush(); SizeIs(E("probe"),300,200);
        Near(E("probe-1").parent.resolvedStyle.translate.x,260);
        Flush(2000); SizeIs(E("probe"),300,200);
        yield return null;
    }
    [UnityTest] public IEnumerator PaddingIsOutsideTheAnimatedContentSize()
    {
        _probeModifier=Modifier.Background(Compose.Net.Color.White).Padding(10.Dp()); RemountProbe();
        SizeIs(E("probe"),120,68); Near(E("probe-0").parent.parent.layout.x,10); Near(E("probe-0").parent.parent.layout.y,10);
        _probeState.Value=1; Flush(); Flush(2000); SizeIs(E("probe"),240,132);
        yield return null;
    }
    [UnityTest] public IEnumerator NullableTargetsAndChildTransitionStatesAreSupported()
    {
        _probeSpec=_=>FadeIn(Tween(1000)).TogetherWith(FadeOut(Tween(500)));
        RemountProbe(); Assert.That(_probeScopes["0"].Transition.CurrentState,Is.EqualTo(EnterExitState.Visible));
        _probeState.Value=null; Flush();
        Assert.That(E("probe-null"),Is.Not.Null); Assert.That(_probeScopes["null"].Transition.CurrentState,Is.EqualTo(EnterExitState.PreEnter));
        Assert.That(_probeScopes["null"].Transition.TargetState,Is.EqualTo(EnterExitState.Visible));
        Assert.That(_probeScopes["0"].Transition.TargetState,Is.EqualTo(EnterExitState.PostExit));
        Assert.That(E("probe").tooltip,Is.EqualTo("Probe label"));
        Flush(1500); Assert.That(rootVisualElement.Q("probe-0"),Is.Null);
        Assert.That(_probeScopes["null"].Transition.CurrentState,Is.EqualTo(EnterExitState.Visible));
        yield return null;
    }
    [UnityTest] public IEnumerator FractionalZIndicesPreserveTheOriginalOrdering()
    {
        _probeSpec=scope=>new ContentTransform(FadeIn(Tween(1000)),FadeOut(Tween(1000)),
            targetContentZIndex: scope.TargetState==0 ? .25f : .1f).Using(null);
        RemountProbe(); _probeState.Value=1; Flush();
        Assert.That(E("probe").IndexOf(E("probe-0").parent.parent),Is.GreaterThan(E("probe").IndexOf(E("probe-1").parent.parent)));
        yield return null;
    }
    private Transition<int?>? _shared;
    [Composable] private void SharedProbe()
    {
        _shared=UpdateTransition(_probeState.Value);
        Column(content:()=>
        {
            _shared.AnimatedContent(content: value=>Box(content:()=>{},modifier:Modifier.Size(50.Dp()).TestTag("fast-"+value)),
                transitionSpec:_=>FadeIn(Tween(200)).TogetherWith(FadeOut(Tween(200))).Using(null));
            _shared.AnimatedContent(content: value=>Box(content:()=>{},modifier:Modifier.Size(50.Dp()).TestTag("slow-"+value)),
                transitionSpec:_=>FadeIn(Tween(1000)).TogetherWith(FadeOut(Tween(1000))).Using(null));
        });
    }
    [UnityTest] public IEnumerator SharedTransitionWaitsForEveryAnimatedContentHost()
    {
        _composer.Dispose(); rootVisualElement.Clear();
        Bootstrap(_composer,_adapter,false,new ComposeConfig(Logging:false),new RootNode(rootVisualElement),SharedProbe); Flush();
        _probeState.Value=1; Flush(); Flush(200);
        Assert.That(_shared!.CurrentState,Is.EqualTo(0)); Assert.That(_shared.IsRunning,Is.True);
        Flush(800); Assert.That(_shared.CurrentState,Is.EqualTo(1)); Assert.That(_shared.IsRunning,Is.False);
        yield return null;
    }
    [UnityTest] public IEnumerator SizeTransformRunsEvenWithoutEnterOrExitAnimations()
    {
        _probeSpec = _ => IEnterTransition.None.TogetherWith(IExitTransition.None)
            .Using(SizeTransform(sizeAnimationSpec: (_, _) => Tween(1000, easing: LinearEasing)));
        RemountProbe(); _probeState.Value = 1; Flush(); SizeIs(E("probe"), 100, 48);
        Flush(500); SizeIs(E("probe"), 160, 80); Flush(500); SizeIs(E("probe"), 220, 112);
        yield return null;
    }
    [UnityTest] public IEnumerator DefaultSizeSpringPreservesVelocityAcrossRetargets()
    {
        _probeSpec = _ => FadeIn(Tween(1000)).TogetherWith(FadeOut(Tween(1000)));
        RemountProbe(); _probeState.Value = 1; Flush(); Flush(100);
        var before = E("probe").layout.width;
        Assert.That(before, Is.InRange(101,219));
        _probeState.Value = 0; Flush(); Near(E("probe").layout.width,before); Flush(1);
        Assert.That(E("probe").layout.width,Is.GreaterThanOrEqualTo(before));
        Flush(2000); SizeIs(E("probe"),100,48);
        yield return null;
    }
    // Run UI async continuations deterministically while keeping the animation clock frozen.
    private sealed class UiContext : SynchronizationContext
    {
        private readonly Queue<(SendOrPostCallback Callback, object? State)> _queue = new();
        public override void Post(SendOrPostCallback callback, object? state)
        { lock (_queue) _queue.Enqueue((callback, state)); }
        internal void Drain()
        {
            for (var i = 0; i < 10000; i++)
            {
                (SendOrPostCallback Callback, object? State) work;
                lock (_queue) { if (_queue.Count == 0) return; work = _queue.Dequeue(); }
                work.Callback(work.State);
            }
            throw new AssertionException("UI continuations did not settle");
        }
    }
    [UnityTest] public IEnumerator ExpansionUsesNaturalTargetSizeAndReversesWithoutJumping()
    {
        yield return Click("expand", "b"); Flush(250);
        SizeIs(Container("expand"),130,64);
        var wrapper=Motion("expand",1).parent;
        var before=wrapper.resolvedStyle.maxWidth.value;
        var left=Motion("expand",1).style.left.value.value;
        yield return Click("expand", "a");
        Near(wrapper.resolvedStyle.maxWidth.value,before); Near(Motion("expand",1).style.left.value.value,left);
        Flush(2000); Only("expand",0); SizeIs(Container("expand"),100,48);
    }
    [UnityTest] public IEnumerator DelayedTransitionsRespectBothDelayWindows()
    {
        yield return Click("delay", "b"); Flush(299);
        Near(Alpha(Motion("delay",0)),1); Near(Alpha(Motion("delay",1)),0);
        Flush(101); Near(Alpha(Motion("delay",1)),0); Assert.That(Alpha(Motion("delay",0)),Is.LessThan(1));
        Flush(200); Only("delay",1); Near(Alpha(Motion("delay",1)),1f/3);
        Flush(400); Near(Alpha(Motion("delay",1)),1);
    }
    [UnityTest] public IEnumerator DisablingSizeTransformDoesNotDisableTheCallersClipModifier()
    {
        _probeModifier = Modifier.Clip();
        _probeSpec = _ => FadeIn(Tween(1000)).TogetherWith(FadeOut(Tween(1000))).Using(null);
        RemountProbe(); Assert.That(E("probe").resolvedStyle.overflow, Is.EqualTo(Overflow.Hidden));
        _probeState.Value=1; Flush(); Flush(2000);
        Assert.That(E("probe").resolvedStyle.overflow, Is.EqualTo(Overflow.Hidden));
        yield return null;
    }
    private sealed class RootNode(VisualElement element) : UnityReusableComposeNode(element), IReusableComposeNode { public new bool IsRoot => true; }
    private sealed class DemoAdapter : IComposeAdapter
    {
        internal readonly TestClock Clock=new(); internal readonly ComposeInvalidator Invalidator=new();
        private IComposeAdapter Native => ComposeUnityAdapter.Instance;
        public IFrameClock FrameClock=>Clock;
        public ComposeInvalidator ComposeInvalidatorInstance=>Invalidator;
        public IModifiersFactory ModifiersFactory=>Native.ModifiersFactory;
        public IReusableNodeFactory NodeFactory=>Native.NodeFactory;
        public IComposeLayouts Layouts => Native.Layouts;
        public IComposeLogger Logger=>Native.Logger;
        public IEnterTransitionsFactory EnterTransitionsFactory=>Native.EnterTransitionsFactory;
        public IExitTransitionsFactory ExitTransitionsFactory=>Native.ExitTransitionsFactory;
        public IBrushFactory BrushFactory=>Native.BrushFactory;
        public IDisposable StartTimeCoroutine(IEnumerable<TimeSpan?> coroutine)=>Native.StartTimeCoroutine(coroutine);
        public int Framerate()=>60;
    }
    private sealed class TestClock : IFrameClock
    {
        private readonly List<TaskCompletionSource<TimeSpan>> _pending=new();
        public TimeSpan Now {get; private set;}
        public Task<TimeSpan> NextFrame(CancellationToken token)
        {
            var source=new TaskCompletionSource<TimeSpan>(); var registration=token.Register(()=>source.TrySetCanceled(token));
            _pending.Add(source); return Await(source.Task,registration);
        }
        private static async Task<TimeSpan> Await(Task<TimeSpan> task,CancellationTokenRegistration registration)
        { try{return await task;} finally{registration.Dispose();} }
        internal void Advance(int milliseconds)
        {
            Now+=TimeSpan.FromMilliseconds(milliseconds); var pending=_pending.ToArray(); _pending.Clear();
            foreach(var source in pending) source.TrySetResult(Now);
        }
    }
}
