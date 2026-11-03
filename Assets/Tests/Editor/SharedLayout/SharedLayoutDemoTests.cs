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

public sealed partial class SharedLayoutDemoTests : UITestFixture
{
    private SynchronizationContext? _previous;
    private readonly UiContext _context = new();
    private IComposer _composer = null!;
    private DemoAdapter _adapter = null!;
    private SharedLayoutDemoState _state = null!;
    [SetUp] public void Mount()
    {
        _previous = SynchronizationContext.Current; SynchronizationContext.SetSynchronizationContext(_context);
        panelSize = new Vector2(1100, 3000); simulate.timePerSimulatedFrameMs = 0;
        _adapter = new DemoAdapter(); _state = new SharedLayoutDemoState(); _composer = IComposer.Create();
        Bootstrap(_composer, _adapter, false, new ComposeConfig(Logging: false), new RootNode(rootVisualElement), Content);
        Flush();
    }
    [Composable] private void Content() => SharedLayoutDemo.Screen(_state);
    [TearDown] public void Unmount()
    {
        _composer.Dispose(); _context.Drain(); SynchronizationContext.SetSynchronizationContext(_previous);
        simulate.ResetTimePerSimulatedFrameToDefault();
    }
    private void Flush(int milliseconds = 0)
    {
        SynchronizationContext.SetSynchronizationContext(_context); _context.Drain(); _adapter.Clock.Advance(milliseconds);
        for (var i = 0; i < 12; i++) { _context.Drain(); _adapter.Invalidator.Tick(); simulate.FrameUpdateMs(0); _adapter.Clock.Advance(0); }
    }
    private VisualElement E(string tag) => rootVisualElement.Q(tag) ?? throw new AssertionException("Missing " + tag);
    private VisualElement Shared(string id, int page) => E("sl." + id + ".shared-" + page);
    private VisualElement Stage(string id) => E("sl." + id + ".stage");
    private static VisualElement? Layer(VisualElement element)
    {
        for (var p = element.parent; p != null; p = p.parent) if (p.name == "shared-transition-layer") return p;
        return null;
    }
    private static float Alpha(VisualElement element)
    {
        var alpha = 1f; for (var p = element; p != null; p = p.parent) alpha *= p.resolvedStyle.opacity;
        return alpha;
    }
    private void Select(string id, int page) { _state[id].Select(page); Flush(); }
    private static void Near(float actual, float expected, float tolerance = .6f) => Assert.That(actual, Is.EqualTo(expected).Within(tolerance));
    [Test] public void AllExamplesMountWithoutInitialAnimation()
    {
        Assert.That(_state.Cases.Length, Is.EqualTo(27));
        foreach (var item in _state.Cases)
        {
            Assert.That(item.Scope!.IsTransitionActive, Is.False, item.Id);
            Assert.That(item.States[0].IsMatchFound, Is.EqualTo(item.Id == "caller"), item.Id);
            Assert.That(Layer(Shared(item.Id, 0)), Is.Null, item.Id);
        }
    }
    [TestCase("element")][TestCase("bounds")][TestCase("remeasure")][TestCase("fill-width")]
    [TestCase("fit")][TestCase("crop")][TestCase("fill-bounds")][TestCase("fill-height")]
    [TestCase("inside")][TestCase("no-scale")][TestCase("placeholder")][TestCase("local")]
    [TestCase("clip")][TestCase("nested")][TestCase("z-order")][TestCase("unmatched")]
    [TestCase("disabled")][TestCase("interrupt")][TestCase("retarget")][TestCase("caller")]
    [TestCase("delay")][TestCase("instant")][TestCase("spring")][TestCase("recompose")]
    [TestCase("visibility")][TestCase("empty")][TestCase("custom-placeholder")]
    public void EveryExampleCompletesForwardAndReverse(string id)
    {
        Select(id, 1); Flush(5000); Select(id, 2); Flush(5000); Select(id, 0); Flush(5000);
        Assert.That(_state[id].Scope!.IsTransitionActive, Is.False, id);
        Assert.That(Layer(Shared(id, 0)), Is.Null, id);
        Near(Shared(id, 0).layout.width, 80); Near(Shared(id, 0).layout.height, 60);
        Assert.That(_state[id].Active.Count, Is.EqualTo(id == "caller" ? 3 : 1), id);
        Assert.That(Stage(id).Query<VisualElement>("shared-transition-placeholder").ToList(), Is.Empty, id);
    }
    [Test] public void ElementEscapesParentOpacityAndOnlyIncomingDraws()
    {
        Select("element", 1); Flush(100);
        var incoming = Shared("element", 1); var outgoing = Shared("element", 0);
        Assert.That(Layer(incoming)!.parent.name, Is.EqualTo("shared-transition-overlay"));
        Near(Alpha(incoming), 1); Assert.That(outgoing.resolvedStyle.visibility, Is.EqualTo(Visibility.Hidden));
        Assert.That(_state["element"].States[0].IsMatchFound, Is.True);
        Flush(400); Near(incoming.worldBound.width, 120); Near(incoming.worldBound.height, 80);
        Assert.That(_state["element"].Active.Count, Is.EqualTo(2), "parent exit is shorter than shared transition");
    }
    [Test] public void BoundsCrossfadeUsesOwnTimingAndKeepsBothContents()
    {
        Select("bounds", 1); Flush(100);
        Assert.That(Shared("bounds", 0).resolvedStyle.visibility, Is.EqualTo(Visibility.Visible));
        Assert.That(Alpha(Shared("bounds", 0)), Is.InRange(.01f,.99f));
        Assert.That(Alpha(Shared("bounds", 1)), Is.InRange(.01f,.99f));
        Flush(500); Near(Alpha(Shared("bounds", 1)), 1);
    }
    [Test] public void LocalRenderingRetainsParentFade()
    {
        Select("local", 1); Flush(90);
        var incoming = Shared("local", 1);
        Assert.That(Layer(incoming)!.parent.name, Is.EqualTo("shared-transition-placeholder"));
        Assert.That(Alpha(incoming), Is.LessThan(incoming.parent.resolvedStyle.opacity));
    }
    [Test] public void ResizeModesUseNativeLayoutOrStableScale()
    {
        Select("remeasure", 1); Select("fill-bounds", 1); Flush(500);
        Near(Shared("remeasure", 1).layout.width,120); Near(Shared("remeasure", 1).resolvedStyle.scale.value.x,1);
        Near(Shared("fill-bounds", 1).layout.width,160); Near(Shared("fill-bounds", 1).resolvedStyle.scale.value.x,.75f,.01f);
        Near(Shared("fill-bounds", 1).resolvedStyle.scale.value.y,.8f,.01f);
    }
    [Test] public void FitCropInsideAndNoneUseTheirDeclaredScaleAndAlignment()
    {
        foreach(var id in new[]{"fit","crop","inside","no-scale","fill-height"}) Select(id,1);
        Flush(500);
        Near(Shared("fit",1).resolvedStyle.scale.value.x,.75f,.01f);
        Near(Shared("crop",1).resolvedStyle.scale.value.x,.8f,.01f);
        Near(Shared("inside",1).resolvedStyle.scale.value.x,.75f,.01f);
        Near(Shared("no-scale",1).resolvedStyle.scale.value.x,1,.01f);
        Near(Shared("no-scale",1).resolvedStyle.translate.x,-40);
        Near(Shared("fill-height",1).resolvedStyle.scale.value.x,.8f,.01f);
    }
    [Test] public void AnimatedPlaceholderMovesFollowingSibling()
    {
        Select("placeholder",1); var sibling=E("sl.placeholder.sibling-1"); var top=sibling.worldBound.y;
        Flush(500); Near(sibling.worldBound.y-top,20); Flush(500); Near(sibling.worldBound.y-top,40);
    }
    [Test] public void NestedChildrenUseIndependentOverlayAndInheritRoundedClip()
    {
        Select("nested",1); Flush(500);
        var child=E("sl.nested.text-1"); var parent=Shared("nested",1);
        Assert.That(Layer(child),Is.Not.SameAs(Layer(parent)));
        Assert.That(Layer(child)!.resolvedStyle.overflow,Is.EqualTo(Overflow.Hidden));
        Near(Layer(child)!.worldBound.width,Layer(parent)!.worldBound.width);
        Near(Layer(child)!.resolvedStyle.borderTopLeftRadius,18);
    }
    [Test] public void ExplicitOverlayZIndexSortsNativeSiblings()
    {
        Select("z-order",1); Flush(500);
        var overlay=Layer(Shared("z-order",1))!.parent;
        Assert.That(overlay.ElementAt(overlay.childCount-1),Is.SameAs(Layer(Shared("z-order",1))));
    }
    [Test] public void MatchedLayersStayElevatedUntilEverySharedTransitionSettles()
    {
        Select("z-order",1); Flush(1500);
        Assert.That(_state["z-order"].Scope!.IsTransitionActive,Is.True);
        Assert.That(Layer(Shared("z-order",1)),Is.Not.Null); Near(Shared("z-order",1).layout.width,160);
        Flush(500); Assert.That(Layer(Shared("z-order",1)),Is.Null);
    }
    [Test] public void DisabledAndUnmatchedContentUsesNormalParentTransition()
    {
        foreach(var id in new[]{"disabled","unmatched"})
        {
            Select(id,1); Assert.That(_state[id].States[1].IsMatchFound,Is.False);
            Assert.That(Layer(Shared(id,1)),Is.Null); Flush(200); Assert.That(_state[id].Active.Count,Is.EqualTo(1));
        }
    }
    [Test] public void ReverseAndThirdTargetStartAtCurrentBounds()
    {
        Select("interrupt",1); Flush(400); var before=Layer(Shared("interrupt",1))!.worldBound;
        Select("interrupt",0); var reverse=Layer(Shared("interrupt",0))!.worldBound;
        Near(reverse.x,before.x); Near(reverse.width,before.width);
        Flush(100); before=Layer(Shared("interrupt",0))!.worldBound;
        Select("interrupt",2); var third=Layer(Shared("interrupt",2))!.worldBound;
        Near(third.x,before.x); Near(third.width,before.width);
        Flush(2000); Assert.That(_state["interrupt"].Active.Count,Is.EqualTo(1));
    }
    [Test] public void ReversingBoundsPreservesBothRenderedOpacities()
    {
        Select("bounds",1); Flush(100); var a=Alpha(Shared("bounds",0)); var b=Alpha(Shared("bounds",1));
        Select("bounds",0); Near(Alpha(Shared("bounds",0)),a,.02f); Near(Alpha(Shared("bounds",1)),b,.02f);
        Flush(1500); Near(Alpha(Shared("bounds",0)),1);
    }
    [Test] public void RetargetingBoundsDoesNotRestartContentFade()
    {
        Select("retarget",1); Flush(100); var before=Alpha(Shared("retarget",1));
        _state["retarget"].Refresh(); Flush(); Near(Alpha(Shared("retarget",1)),before,.02f);
        Flush(250); Near(Alpha(Shared("retarget",1)),1,.02f);
    }
    [Test] public void ChangedTargetGeometryRetargetsWithoutJumping()
    {
        Select("retarget",1); Flush(400); var before=Layer(Shared("retarget",1))!.worldBound;
        _state["retarget"].Refresh(); Flush(); Near(Layer(Shared("retarget",1))!.worldBound.width,before.width);
        Flush(1500); Near(Shared("retarget",1).layout.width,210);
    }
    [Test] public void CallerManagedVisibilityKeepsLayoutsAndHidesInactiveContent()
    {
        Assert.That(Shared("caller",1).resolvedStyle.visibility,Is.EqualTo(Visibility.Hidden));
        Select("caller",1); Flush(500); Assert.That(_state["caller"].Active.Count,Is.EqualTo(3));
        Flush(1000); Assert.That(Shared("caller",0).resolvedStyle.visibility,Is.EqualTo(Visibility.Hidden));
        Assert.That(Shared("caller",1).resolvedStyle.visibility,Is.EqualTo(Visibility.Visible));
    }
    [Test] public void BoundsDelayAndEnterExitDurationsAreIndependent()
    {
        Select("delay",1); Flush(250); Near(Layer(Shared("delay",1))!.layout.width,80);
        Near(Alpha(Shared("delay",1)),250f/700,.02f); Near(Alpha(Shared("delay",0)),0);
        Flush(550); Near(Layer(Shared("delay",1))!.layout.width,120); Assert.That(_state["delay"].Scope!.IsTransitionActive,Is.True);
        Flush(500); Assert.That(_state["delay"].Scope!.IsTransitionActive,Is.False);
    }
    [Test] public void RefreshPreservesStateAndDoesNotRestartAnimation()
    {
        Select("recompose",1); Flush(400); var state=_state["recompose"].States[1];
        _state["recompose"].Refresh(); Flush(); Assert.That(_state["recompose"].States[1],Is.SameAs(state));
        Flush(600); Assert.That(_state["recompose"].Scope!.IsTransitionActive,Is.False);
    }
    [Test] public void ScopesWithEqualKeysNeverShareAcrossCards()
    {
        Select("element",1); Assert.That(_state["bounds"].Scope!.IsTransitionActive,Is.False);
        Assert.That(_state["bounds"].States[0].IsMatchFound,Is.False);
    }
    [Test] public void DisposalDuringTransitionRemovesOverlayAndPlaceholders()
    {
        _state.SelectAll(1); Flush(); Flush(300); _composer.Dispose(); Flush();
        Assert.That(rootVisualElement.Query<VisualElement>("shared-transition-layer").ToList(),Is.Empty);
        Assert.That(rootVisualElement.Query<VisualElement>("shared-transition-placeholder").ToList(),Is.Empty);
        foreach(var item in _state.Cases) Assert.That(item.Active,Is.Empty,item.Id);
    }
    [Test] public void ZeroDurationParentRetainsSourceUntilSharedTransitionCompletes()
    {
        _state["element"].ParentDuration=0; Select("element",1); Flush(500);
        Assert.That(_state["element"].Active.Count,Is.EqualTo(2)); Near(Shared("element",1).worldBound.width,120);
        Flush(500); Assert.That(_state["element"].Active.Count,Is.EqualTo(1));
    }
    [Test] public void ConfigurationCanEnablePreviouslyDisabledMatches()
    {
        _state["disabled"].Refresh(); Flush(); Select("disabled",1); Flush(300);
        Assert.That(_state["disabled"].Scope!.IsTransitionActive,Is.True);
        Assert.That(_state["disabled"].States[1].IsMatchFound,Is.True);
    }
    [Test] public void DisablingDuringAnimationKeepsCurrentTransitionByDefault()
    {
        Select("bounds",1); Flush(300); _state["bounds"].Enabled.Value=false; Flush();
        Assert.That(_state["bounds"].Scope!.IsTransitionActive,Is.True);
        Flush(1000); Assert.That(_state["bounds"].Scope!.IsTransitionActive,Is.False);
    }
    [Test] public void ConfigurationCanCancelAnOngoingTransition()
    {
        Select("bounds",1); Flush(300); _state["bounds"].KeepEnabledForOngoingAnimation=false;
        _state["bounds"].Enabled.Value=false; Flush();
        Assert.That(_state["bounds"].Scope!.IsTransitionActive,Is.False);
        Assert.That(Layer(Shared("bounds",1)),Is.Null);
    }
    [Test] public void ScopedAnimatedVisibilitySharesAndDisposesTheExitingLayout()
    {
        Select("visibility",1); Flush(500); Assert.That(_state["visibility"].Active.Count,Is.EqualTo(2));
        Assert.That(_state["visibility"].Scope!.IsTransitionActive,Is.True);
        Flush(1000); Assert.That(_state["visibility"].Active.Count,Is.EqualTo(1));
    }
    [Test] public void ZeroSizeTargetCompletesWithoutNaNOrRetainedSource()
    {
        Select("empty",1); Flush(1500); Near(Shared("empty",1).layout.width,0);
        Assert.That(_state["empty"].Scope!.IsTransitionActive,Is.False);
        Assert.That(_state["empty"].Active.Count,Is.EqualTo(1));
    }
    [UnityTest] public IEnumerator ElementButtonsDeliverPointerInput() => ClickButtons("element");
    [UnityTest] public IEnumerator BoundsButtonsDeliverPointerInput() => ClickButtons("bounds");
    [UnityTest] public IEnumerator RetargetButtonsDeliverPointerInput() => ClickButtons("retarget");
    [UnityTest] public IEnumerator CallerButtonsDeliverPointerInput() => ClickButtons("caller");
    [UnityTest] public IEnumerator MovingSharedContentReceivesNativePointerInput()
    {
        Select("element",1); Flush(400); var item=Shared("element",1); var before=_state["element"].Interactions;
        var picked=item.panel.Pick(item.worldBound.center);
        Assert.That(picked==item || item.Contains(picked),Is.True,"Moving shared content must be pickable in overlay");
        simulate.MouseDown(item); yield return null; Flush(); simulate.MouseUp(item);
        for(var i=0;i<120 && _state["element"].Interactions==before;i++) { yield return null; Flush(); }
        Assert.That(_state["element"].Interactions,Is.EqualTo(before+1)); Flush(2000);
        Assert.That(_state["element"].Page.Value,Is.EqualTo(2));
    }
    private IEnumerator ClickButtons(string id)
    {
        foreach(var suffix in new[]{"b","c","a","refresh"})
        {
            var button=E("sl."+id+"."+suffix); var before=_state[id].Interactions;
            simulate.MouseDown(button); yield return null; Flush(); simulate.MouseUp(button);
            for(var i=0;i<120 && _state[id].Interactions==before;i++) { yield return null; Flush(); }
            Assert.That(_state[id].Interactions,Is.EqualTo(before+1)); Flush(2000);
        }
    }
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
    private sealed class RootNode(VisualElement element) : UnityReusableComposeNode(element), IReusableComposeNode { public new bool IsRoot => true; }
    private sealed class DemoAdapter : IComposeAdapter
    {
        internal readonly TestClock Clock=new(); internal readonly ComposeInvalidator Invalidator=new();
        private IComposeAdapter Native => ComposeUnityAdapter.Instance;
        public IFrameClock FrameClock=>Clock;
        public ComposeInvalidator ComposeInvalidatorInstance=>Invalidator;
        public IModifiersFactory ModifiersFactory=>Native.ModifiersFactory;
        public IReusableNodeFactory NodeFactory=>Native.NodeFactory;
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
