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

public sealed partial class PagerTests : UITestFixture
{
    private IComposer _composer = null!;
    private TestAdapter _adapter = null!;
    private PagerDemoState _gallery = null!;
    private PagerDemoCase _item = null!;
    private SynchronizationContext? _previous;
    private readonly UiContext _context = new();
    private bool _fullGallery;
    private ScrollView View => rootVisualElement.Q<ScrollView>("pager." + _item.Id);
    private IMutablePagerState State => (IMutablePagerState)_item.State;
    private VisualElement E(string name) => rootVisualElement.Q(name) ?? throw new AssertionException("Missing " + name);
    private VisualElement Page(int key) => E("pager." + _item.Id + ".page-" + key);

    [SetUp] public void SetUpPager()
    {
        _previous = SynchronizationContext.Current; SynchronizationContext.SetSynchronizationContext(_context);
        panelSize = new Vector2(1100, 2700); simulate.timePerSimulatedFrameMs = 0;
        _gallery = new PagerDemoState(); _item = _gallery["horizontal"];
        _adapter = new TestAdapter(); _fullGallery = false;
        Mount();
    }
    [TearDown] public void TearDownPager()
    {
        _composer?.Dispose(); _context.Drain(); SynchronizationContext.SetSynchronizationContext(_previous);
        simulate.ResetTimePerSimulatedFrameToDefault();
    }
    [Composable] private void Content()
    {
        if (_fullGallery) PagerDemo.Screen(_gallery); else PagerDemo.Card(_item);
    }
    private void Mount()
    {
        _composer?.Dispose(); rootVisualElement.Clear();
        _composer = IComposer.Create();
        Bootstrap(_composer, _adapter, false, new ComposeConfig(Logging: false), new RootNode(rootVisualElement), Content);
        Flush();
    }
    private void Select(string id) { _item = _gallery[id]; Mount(); }
    private void Flush(int milliseconds = 0)
    {
        SynchronizationContext.SetSynchronizationContext(_context); _context.Drain(); _adapter.Clock.Advance(milliseconds);
        for (var i = 0; i < 8; i++) { _context.Drain(); _adapter.Invalidator.Tick(); simulate.FrameUpdateMs(0); }
    }
    private static void Near(float actual, float expected) => Assert.That(actual, Is.EqualTo(expected).Within(.65f));
    private float Axis(Vector2 value) => _item.Vertical ? value.y : value.x;
    private float Main(VisualElement e) => Axis(e.layout.size);
    private float Start(VisualElement e) => Axis(e.worldBound.position) - Axis(View.contentViewport.worldBound.position);
    private IEnumerator Click(string control)
    {
        var button = E("pager." + _item.Id + "." + control); var count = _item.Interactions;
        simulate.MouseDown(button); yield return null; Flush(); simulate.MouseUp(button);
        for (var i = 0; i < 90 && _item.Interactions == count; i++) { yield return null; Flush(); }
        Assert.That(_item.Interactions, Is.EqualTo(count + 1), "Undelivered click " + control);
        Flush();
    }

    [TestCase("horizontal")][TestCase("vertical")][TestCase("reverse")][TestCase("reverse-vertical")]
    public void FillUsesNativeScrollViewAndRealPageBounds(string id)
    {
        Select(id);
        Assert.That(View, Is.InstanceOf<ScrollView>());
        Near(Main(Page(0)), Main(View.contentViewport));
        Assert.That(View.contentViewport.resolvedStyle.overflow, Is.EqualTo(Overflow.Hidden));
        Near(Start(Page(0)), 0); Assert.That(State.CurrentPage, Is.Zero);
        State.ScrollToPage(3); Flush(); Near(Start(Page(3)), 0); Assert.That(State.CurrentPage, Is.EqualTo(3));
        Assert.That(rootVisualElement.Q("pager." + id + ".page-0"), Is.Null, "Offscreen composition leaked");
        Assert.That(_item.Active.Count, Is.EqualTo(1));
    }
    [TestCase("horizontal")][TestCase("vertical")]
    public void BoundaryRequestsClampAndKeepTheLastPageVisible(string id)
    {
        Select(id); State.ScrollToPage(100000); Flush(); Assert.That(State.CurrentPage, Is.EqualTo(6));
        Near(Start(Page(6)), 0); Assert.That(State.CanScrollForward, Is.False);
        State.ScrollToPage(-100); Flush(); Near(Start(Page(0)), 0); Assert.That(State.CanScrollBackward, Is.False);
    }
    [Test] public void FixedSizeDoesNotSubtractPaddingAndSpacingIsExact()
    {
        Select("fixed"); Near(Main(Page(0)), 100); Near(Start(Page(0)), 12); Near(Start(Page(1)), 120);
        State.ScrollToPage(3); Flush(); Near(Start(Page(3)), 12); Near(Main(Page(3)), 100);
        Near(State.LayoutInfo.PageSpacing, 8);
    }
    [TestCase("horizontal")][TestCase("vertical")]
    public void AsymmetricPaddingAndSpacingApplyOnceToTheStrip(string id)
    {
        Select(id); _item.Padding = new PaddingValues(Top: 11.Dp(), Bottom: 19.Dp(), Left: 17.Dp(), Right: 29.Dp());
        _item.Spacing = 13; _item.Revision.Value++; Flush();
        var before = _item.Vertical ? 11 : 17; var after = _item.Vertical ? 19 : 29;
        Near(Main(Page(0)), Main(View.contentViewport) - before - after); Near(Start(Page(0)), before);
        State.ScrollToPage(2); Flush(); Near(Start(Page(2)), before); Near(State.PageStride, State.PageExtent + 13);
    }
    [Test] public void InitialOffsetSurvivesAttachAndIncludesSpacing()
    {
        Select("initial"); Assert.That(State.CurrentPage, Is.EqualTo(2)); Near(State.CurrentPageOffsetFraction, .25f);
        Near(State.Value, 2.25f * (314 + 10)); Near(Start(Page(2)), -(314 + 10) * .25f);
    }
    [TestCase("horizontal")][TestCase("vertical")]
    public void ResizeKeepsPageAndFraction(string id)
    {
        Select(id); State.ScrollToPage(2, .25f); Flush();
        _item.Width.Value = 250; Flush(); Near(View.layout.width, 250); Assert.That(State.CurrentPage, Is.EqualTo(2)); Near(State.CurrentPageOffsetFraction, .25f);
        Near(State.Value, State.PageStride * 2.25f);
    }
    [Test] public void StableKeysPreserveVisibleCompositionAcrossPrependAndRemoval()
    {
        Select("keys"); var identity = _item.Remembered[2]; var element = Page(2);
        _item.Prepend(); Flush(); Assert.That(State.CurrentPage, Is.EqualTo(3));
        Assert.That(_item.Remembered[2], Is.SameAs(identity)); Assert.That(Page(2), Is.SameAs(element)); Near(Start(Page(2)), 0);
        _item.Keys.RemoveAt(0); _item.Count.Value--; _item.Revision.Value++; Flush();
        Assert.That(State.CurrentPage, Is.EqualTo(2)); Assert.That(_item.Remembered[2], Is.SameAs(identity));
    }
    [Test] public void DynamicCountClampsAndEmptyCanBeRepopulated()
    {
        Select("dynamic"); State.ScrollToPage(6); Flush();
        _item.Count.Value = 2; Flush(); Assert.That(State.CurrentPage, Is.EqualTo(1)); Near(Start(Page(1)), 0);
        _item.Count.Value = 0; Flush(); Assert.That(_item.Active, Is.Empty); Assert.That(State.LayoutInfo.VisiblePagesInfo, Is.Empty);
        _item.Add(); Flush(); Assert.That(State.CurrentPage, Is.Zero); Near(Start(Page(_item.Keyed ? _item.Keys[0] : 0)), 0);
    }
    [Test] public void LargeDataSetAndBeyondViewportStayBounded()
    {
        Select("large"); Assert.That(_item.Active.Count, Is.EqualTo(1));
        State.ScrollToPage(9999); Flush(); Assert.That(_item.Active.Count, Is.EqualTo(1)); Near(Start(Page(9999)), 0);
        Select("beyond"); Assert.That(_item.Active.OrderBy(i => i), Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
        Assert.That(State.LayoutInfo.VisiblePagesInfo.Count, Is.EqualTo(1));
    }
    [Test] public void CrossAxisAlignmentUsesIntrinsicChildSize()
    {
        Select("align-top"); Near(Page(0).layout.height, 50); Near(Page(0).worldBound.y - View.contentViewport.worldBound.y, 0);
        Select("align-end"); Near(Page(0).layout.width, 80); Near(View.contentViewport.worldBound.xMax - Page(0).worldBound.xMax, 0);
    }
    [TestCase("center")][TestCase("end")]
    public void CustomSnapPositionsAlignTheRequestedPage(string id)
    {
        Select(id); State.ScrollToPage(3); Flush();
        Near(Start(Page(3)), id == "center" ? (314 - 110) / 2f : 314 - 110);
        Assert.That(State.CurrentPage, Is.EqualTo(3)); Near(State.CurrentPageOffsetFraction, 0);
    }
    [Test] public void CustomPageSizeShowsExactlyThreePagesInsidePadding()
    {
        Select("custom"); Near(Main(Page(0)), (314 - 20 - 16) / 3f);
        Near(Page(2).worldBound.xMax - View.contentViewport.worldBound.xMin, 304);
    }
    [Test] public void AnimationCurrentSettledTargetRetargetAndImmediateCancellation()
    {
        State.ScrollToPage(0); var task = State.AnimateScrollToPage(4, animationSpec: Tween(1000, easing: LinearEasing));
        Flush(400); Assert.That(State.CurrentPage, Is.EqualTo(2)); Assert.That(State.SettledPage, Is.Zero); Assert.That(State.TargetPage, Is.EqualTo(4));
        var before = View.scrollOffset; var second = State.AnimateScrollToPage(1, .25f, Tween(1000, easing: LinearEasing));
        Flush(); Near(View.scrollOffset.x, before.x); Assert.That(task.IsCanceled, Is.True);
        Flush(1000); Assert.That(second.IsCompleted, Is.True); Assert.That(State.SettledPage, Is.EqualTo(1)); Near(State.CurrentPageOffsetFraction, .25f);
        var third = State.AnimateScrollToPage(6); State.ScrollToPage(2); Flush();
        Assert.That(third.IsCanceled, Is.True); Assert.That(State.IsScrollInProgress, Is.False); Near(Start(Page(2)), 0);
    }
    [UnityTest] public IEnumerator NextPreviousAndJumpButtonsActuallyDispatchClicks()
    {
        yield return Click("next"); Flush(1000); Assert.That(State.CurrentPage, Is.EqualTo(1)); Near(Start(Page(1)), 0);
        yield return Click("previous"); Flush(1000); Assert.That(State.CurrentPage, Is.Zero);
        yield return Click("last"); Assert.That(State.CurrentPage, Is.EqualTo(6));
        yield return Click("first"); Assert.That(State.CurrentPage, Is.Zero);
        yield return Click("offset"); Near(State.CurrentPageOffsetFraction, .25f);
    }
    [UnityTest] public IEnumerator OtherDemoControlsDispatchAndUpdateThePager()
    {
        yield return Click("lock"); Assert.That(_item.Enabled.Value, Is.False);
        yield return Click("reverse"); Assert.That(_item.Reverse.Value, Is.True);
        yield return Click("resize"); Assert.That(_item.Width.Value, Is.EqualTo(250));
        Assert.That(View.layout.width, Is.EqualTo(250).Within(.65f), $"inline={View.style.width}, resolved={View.resolvedStyle.width}, min={View.resolvedStyle.minWidth}, max={View.resolvedStyle.maxWidth}, viewport={State.LayoutInfo.ViewportSize}");
        yield return Click("add"); Assert.That(State.PageCount, Is.EqualTo(8));
        yield return Click("remove"); Assert.That(State.PageCount, Is.EqualTo(7));
        yield return Click("prepend"); Assert.That(State.PageCount, Is.EqualTo(8));
        var identity = _item.Remembered[0]; yield return Click("refresh"); Assert.That(_item.Remembered[0], Is.SameAs(identity));
    }
    [UnityTest] public IEnumerator NativeWheelScrollsThenSnaps()
    {
        simulate.ScrollWheel(View, new Vector2(0, 12)); Flush(); Assert.That(State.Value, Is.GreaterThan(0));
        yield return Settle(); Assert.That(State.IsScrollInProgress, Is.False); Near(State.CurrentPageOffsetFraction, 0);
        Assert.That(State.CurrentPage, Is.EqualTo(1)); Near(Start(Page(1)), 0);
    }
    [UnityTest] public IEnumerator DisabledWheelCannotScrollButControlsCan()
    {
        Select("locked"); simulate.ScrollWheel(View, new Vector2(0, 20)); Flush(); yield return Settle(); Near(State.Value, 0);
        yield return Click("next"); Flush(1000); Assert.That(State.CurrentPage, Is.EqualTo(1));
    }
    [UnityTest] public IEnumerator HorizontalTouchDrag() => ExerciseTouch("horizontal");
    [UnityTest] public IEnumerator VerticalTouchDrag() => ExerciseTouch("vertical");
    [UnityTest] public IEnumerator ReversedHorizontalTouchDrag() => ExerciseTouch("reverse");
    [UnityTest] public IEnumerator ReversedVerticalTouchDrag() => ExerciseTouch("reverse-vertical");
    private IEnumerator ExerciseTouch(string id)
    {
        Select(id); State.ScrollToPage(2); Flush();
        var direction = (_item.Vertical ? Vector2.up : Vector2.right) * (_item.Reverse.Value ? -1 : 1);
        var extent = State.PageExtent;
        var start = View.worldBound.center + direction * extent * .35f;
        Touch(TouchPhase.Began, start);
        Touch(TouchPhase.Moved, start - direction * extent * .1f);
        Touch(TouchPhase.Moved, start - direction * extent * .7f);
        Assert.That(State.Value, Is.GreaterThan(extent * 2.1f)); Assert.That(State.SettledPage, Is.EqualTo(2));
        Touch(TouchPhase.Ended, start - direction * extent * .7f); yield return Settle();
        Assert.That(State.CurrentPage, Is.EqualTo(3)); Near(State.CurrentPageOffsetFraction, 0);
        Assert.That(State.IsScrollInProgress, Is.False);
    }
    [UnityTest] public IEnumerator DisabledScrollingStillAllowsTouchOnPageContent()
    {
        Select("locked"); var point = Page(0).worldBound.center;
        Touch(TouchPhase.Began, point, Page(0)); yield return null; Flush();
        Touch(TouchPhase.Ended, point, Page(0));
        for (var i = 0; i < 90 && _item.PageClicks.Value == 0; i++) { yield return null; Flush(); }
        Assert.That(_item.PageClicks.Value, Is.EqualTo(1)); Near(State.Value, 0);
    }
    [UnityTest] public IEnumerator DisabledTouchAndTouchCancelDoNotLeaveScrollingState()
    {
        Select("locked"); var point = View.worldBound.center;
        Touch(TouchPhase.Began, point); Touch(TouchPhase.Moved, point - new Vector2(150, 0)); Touch(TouchPhase.Ended, point - new Vector2(150, 0));
        yield return Settle(); Near(State.Value, 0);
        _item.Enabled.Value = true; Flush(); Touch(TouchPhase.Began, point); Touch(TouchPhase.Moved, point - new Vector2(100, 0)); Touch(TouchPhase.Canceled, point - new Vector2(100, 0));
        yield return Settle(); Assert.That(State.IsScrollInProgress, Is.False); Near(State.CurrentPageOffsetFraction, 0);
    }
    [TestCase("horizontal")][TestCase("vertical")][TestCase("reverse")][TestCase("reverse-vertical")]
    public void KeyboardNavigationRespectsAxisReverseAndDisabledInput(string id)
    {
        Select(id); State.ScrollToPage(3); Flush();
        var direction = _item.Vertical ? Vector2.down : Vector2.right;
        using (var evt = NavigationMoveEvent.GetPooled(direction)) { evt.target = View; View.SendEvent(evt); }
        Flush(1000); Assert.That(State.CurrentPage, Is.EqualTo(_item.Reverse.Value ? 2 : 4));
        var page = State.CurrentPage; _item.Enabled.Value = false; Flush();
        using (var evt = NavigationMoveEvent.GetPooled(direction)) { evt.target = View; View.SendEvent(evt); }
        Flush(1000); Assert.That(State.CurrentPage, Is.EqualTo(page));
    }
    [Test] public void DisposalCancelsAnimationsAndRemovesAllPageCompositions()
    {
        var task = State.AnimateScrollToPage(5); Flush(100); _composer.Dispose(); Flush(1000);
        Assert.That(task.IsCanceled, Is.True); Assert.That(_item.Active, Is.Empty);
    }
    [Test] public void AllTwentyOneGalleryCasesMountAndRemainLazy()
    {
        _fullGallery = true; Mount(); Assert.That(_gallery.Cases.Length, Is.EqualTo(21));
        foreach (var item in _gallery.Cases)
        {
            Assert.That(item.State, Is.Not.Null, item.Id);
            Assert.That(rootVisualElement.Q<ScrollView>("pager." + item.Id), Is.Not.Null, item.Id);
            Assert.That(item.Active.Count, Is.LessThanOrEqualTo(7), item.Id);
        }
    }
    [Test] public void GeneratedDemoSerializesWithConfiguredRenderer()
    {
        var path = UnityEditor.AssetDatabase.GenerateUniqueAssetPath("Assets/PagerSceneValidation.prefab");
        var original = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
        GameObject? restored = null;
        try
        {
            var demo = Compose.Net.Unity.Samples.Editor.PagerDemoScene.EnsureInScene(scene);
            Assert.That(UnityEditor.PrefabUtility.SaveAsPrefabAsset(demo, path), Is.Not.Null);
            restored = UnityEditor.PrefabUtility.LoadPrefabContents(path);
            var demos = restored.GetComponentsInChildren<PagerDemo>(true);
            Assert.That(demos.Length, Is.EqualTo(1));
            var renderer = demos[0].GetComponent<PanelRenderer>();
            Assert.That(renderer, Is.Not.Null); Assert.That(renderer.panelSettings, Is.Not.Null);
            Assert.That(renderer.visualTreeAsset, Is.Not.Null); Assert.That(demos[0].enabled, Is.True);
        }
        finally
        {
            if (restored != null) UnityEditor.PrefabUtility.UnloadPrefabContents(restored);
            UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
            if (original.IsValid()) UnityEngine.SceneManagement.SceneManager.SetActiveScene(original);
            UnityEditor.AssetDatabase.DeleteAsset(path);
        }
    }
    private void Touch(TouchPhase phase, Vector2 point, VisualElement? target = null)
    {
        var touch = new UnityEngine.Touch { fingerId = 0, position = point, rawPosition = point, phase = phase, type = TouchType.Direct, pressure = 1, maximumPossiblePressure = 1, tapCount = 1 };
        using EventBase evt = phase switch
        {
            TouchPhase.Began => PointerDownEvent.GetPooled(touch, EventModifiers.None),
            TouchPhase.Moved => PointerMoveEvent.GetPooled(touch, EventModifiers.None),
            TouchPhase.Canceled => PointerCancelEvent.GetPooled(touch, EventModifiers.None),
            _ => PointerUpEvent.GetPooled(touch, EventModifiers.None)
        };
        evt.target = target ?? View.contentContainer; (target ?? View.contentContainer).SendEvent(evt); Flush();
    }
    private IEnumerator Settle()
    {
        simulate.FrameUpdateMs(200); yield return null; Flush();
        for (var i = 0; i < 30; i++) { yield return null; Flush(40); simulate.FrameUpdateMs(20); }
    }
    private sealed class RootNode(VisualElement element) : UnityReusableComposeNode(element), IReusableComposeNode { public new bool IsRoot => true; }
    private sealed class TestAdapter : IComposeAdapter
    {
        internal readonly TestClock Clock = new(); internal readonly ComposeInvalidator Invalidator = new();
        private IComposeAdapter Native => ComposeUnityAdapter.Instance;
        public IFrameClock FrameClock => Clock;
        public ComposeInvalidator ComposeInvalidatorInstance => Invalidator;
        public IModifiersFactory ModifiersFactory => Native.ModifiersFactory;
        public IReusableNodeFactory NodeFactory => Native.NodeFactory;
        public IComposeLayouts Layouts => Native.Layouts;
        public IComposeLogger Logger => Native.Logger;
        public IEnterTransitionsFactory EnterTransitionsFactory => Native.EnterTransitionsFactory;
        public IExitTransitionsFactory ExitTransitionsFactory => Native.ExitTransitionsFactory;
        public IBrushFactory BrushFactory => Native.BrushFactory;
        public IDisposable StartTimeCoroutine(IEnumerable<TimeSpan?> coroutine) => Native.StartTimeCoroutine(coroutine);
        public int Framerate() => 60;
    }
    private sealed class TestClock : IFrameClock
    {
        private readonly List<TaskCompletionSource<TimeSpan>> _pending = new();
        public TimeSpan Now { get; private set; }
        public Task<TimeSpan> NextFrame(CancellationToken token)
        {
            var source = new TaskCompletionSource<TimeSpan>(); var registration = token.Register(() => source.TrySetCanceled(token));
            _pending.Add(source); return Await(source.Task, registration);
        }
        private static async Task<TimeSpan> Await(Task<TimeSpan> task, CancellationTokenRegistration registration)
        { try { return await task; } finally { registration.Dispose(); } }
        internal void Advance(int milliseconds)
        {
            Now += TimeSpan.FromMilliseconds(milliseconds); var pending = _pending.ToArray(); _pending.Clear();
            foreach (var source in pending) source.TrySetResult(Now);
        }
    }
    private sealed class UiContext : SynchronizationContext
    {
        private readonly Queue<(SendOrPostCallback Callback, object? State)> _queue = new();
        public override void Post(SendOrPostCallback callback, object? state) { lock (_queue) _queue.Enqueue((callback, state)); }
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
}
