using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Compose.Net;
using static Compose.Net.ComposeFunctions;
using Compose.Net.Unity.Packages.Compose.Net.Unity.Runtime.Adapters;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;
using Color = Compose.Net.Color;

namespace Compose.Net.Unity.Tests;

[TestFixture(false, false), TestFixture(false, true), TestFixture(true, false), TestFixture(true, true)]
public sealed partial class LazyListTests : UITestFixture
{
    private readonly bool _horizontal, _initialReverse;
    private IComposer _composer = null!;
    private DemoAdapter _adapter = null!;
    private IMutableState<ILazyListState> _state = null!;
    private IMutableState<bool> _enabled = null!, _reverse = null!, _present = null!;
    private IMutableState<int> _count = null!, _size = null!, _revision = null!, _prefix = null!, _itemSize = null!;
    private bool _variable, _padded, _spaced, _sticky, _center, _crossEnd, _fillParent, _negativeSpace, _autoCross;
    private FlingBehavior? _fling;
    private OverscrollEffect? _overscroll;
    private int _clicks, _composed, _active, _forgotten;
    private readonly Dictionary<int, object> _remembered = new();
    private SynchronizationContext? _previousContext;
    private readonly UiContext _context = new();
    private ILazyListState State => _state.Value;
    private ScrollView Native => E("viewport").Q<ScrollView>();
    private float Axis(Vector2 v) => _horizontal ? v.x : v.y;
    private float Physical => Axis(Native.scrollOffset);
    private static readonly ICompositionLocal<string> ItemLocal = CompositionLocalOf(() => "missing");
    public LazyListTests(bool horizontal, bool reverse) { _horizontal = horizontal; _initialReverse = reverse; }
    [SetUp] public void Setup()
    {
        _previousContext = SynchronizationContext.Current; SynchronizationContext.SetSynchronizationContext(_context);
        panelSize = new Vector2(700, 500); simulate.timePerSimulatedFrameMs = 0;
        _adapter = new DemoAdapter(); _state = MutableStateOf<ILazyListState>(new LazyListState());
        _enabled = MutableStateOf(true); _reverse = MutableStateOf(_initialReverse); _present = MutableStateOf(true);
        _count = MutableStateOf(10000); _size = MutableStateOf(240); _revision = MutableStateOf(0);
        _prefix = MutableStateOf(0); _itemSize = MutableStateOf(60);
        _variable = _padded = _spaced = _sticky = _center = _crossEnd = _fillParent = _negativeSpace = _autoCross = false;
        _fling = null; _overscroll = null;
        _clicks = _composed = _active = _forgotten = 0; _remembered.Clear();
    }
    [TearDown] public void Teardown()
    {
        _composer?.Dispose(); _context.Drain(); SynchronizationContext.SetSynchronizationContext(_previousContext);
        simulate.ResetTimePerSimulatedFrameToDefault();
    }
    private IEnumerator Mount()
    {
        _composer = IComposer.Create();
        Bootstrap(_composer, _adapter, false, new ComposeConfig(Logging: false), new RootNode(rootVisualElement), Content);
        yield return Settle();
        Assert.That(Native, Is.Not.Null);
    }
    [Composable] private void Content()
    {
        if (!_present.Value) return;
        var revision = _revision.Value;
        var count = _count.Value; var prefix = _prefix.Value; var itemSize = _itemSize.Value;
        var modifier = Modifier.Size((_horizontal ? _size.Value : 220).Dp(), (_horizontal ? 180 : _size.Value).Dp()).TestTag("viewport");
        if (_autoCross) modifier = (_horizontal ? Modifier.Width(_size.Value.Dp()) : Modifier.Height(_size.Value.Dp())).TestTag("viewport");
        CompositionLocalProvider(ItemLocal.Provides("inherited"), () =>
        {
            Action<ILazyListScope> items = scope =>
            {
                if (_sticky)
                {
                    for (var section = 0; section < 4; section++)
                    {
                        var part = section;
                        scope.StickyHeader(index => Item(index, itemSize, true), "header-" + section, "header");
                        scope.Items(8, i => Item(part * 9 + i + 1, itemSize, false), i => part * 9 + i + 1, _ => "cell");
                    }
                }
                else scope.Items(count, i => Item(i - prefix, itemSize, false), i => i - prefix, i => i % 2);
            };
            if (_horizontal) LazyRow(items, state: State, modifier: modifier, reverseLayout: _reverse.Value,
                contentPadding: _padded ? new PaddingValues(12.Dp()) : default,
                horizontalArrangement: _spaced ? Arrangement.SpacedBy((_negativeSpace ? -10 : 10).Dp()) : _center ? Arrangement.Center : null,
                verticalAlignment: _crossEnd ? Alignment.Bottom : Alignment.Top,
                userScrollEnabled: _enabled.Value, flingBehavior: _fling, overscrollEffect: _overscroll);
            else LazyColumn(items, state: State, modifier: modifier, reverseLayout: _reverse.Value,
                contentPadding: _padded ? new PaddingValues(12.Dp()) : default,
                verticalArrangement: _spaced ? Arrangement.SpacedBy((_negativeSpace ? -10 : 10).Dp()) : _center ? Arrangement.Center : null,
                horizontalAlignment: _crossEnd ? Alignment.Right : Alignment.Left,
                userScrollEnabled: _enabled.Value, flingBehavior: _fling, overscrollEffect: _overscroll);
        });
    }
    [Composable] private void Item(int id, int baseSize, bool header)
    {
        _composed++;
        var token = Remember(() => new Lifetime(this));
        _remembered[id] = token;
        var local = ItemLocal.Current;
        var size = _variable ? baseSize + Math.Abs(id % 3) * 20 : baseSize;
        var modifier = Modifier.Size((_horizontal ? size : 100).Dp(), (_horizontal ? 90 : size).Dp())
            .Background(header ? Color.Orange : Color.DodgerBlue).TestTag("item-" + id).Clickable(() => _clicks++);
        if (_fillParent) modifier = _horizontal ? modifier.FillParentMaxWidth(.5f) : modifier.FillParentMaxHeight(.5f);
        Box(content: () => Text(local + " " + id), modifier: modifier);
    }
    private sealed class Lifetime(LazyListTests owner) : IRememberObserver
    {
        public void OnRemembered() => owner._active++;
        public void OnForgotten() { owner._active--; owner._forgotten++; }
    }
    private VisualElement E(string tag) => rootVisualElement.Q(tag) ?? throw new AssertionException("Missing " + tag);
    private void Flush(int milliseconds = 0)
    {
        SynchronizationContext.SetSynchronizationContext(_context); _context.Drain(); _adapter.Clock.Advance(milliseconds);
        for (var i = 0; i < 6; i++) { _context.Drain(); _adapter.Invalidator.Tick(); simulate.FrameUpdateMs(16); }
    }
    private IEnumerator Settle(int frames = 8) { for (var i = 0; i < frames; i++) { yield return null; Flush(); } }
    private static void Near(float actual, float expected) => Assert.That(actual, Is.EqualTo(expected).Within(1f));
    private void Wheel(float direction) { simulate.ScrollWheel(_horizontal ? new Vector2(direction, 0) : new Vector2(0, direction), Native.contentViewport.worldBound.center); Flush(); }

    [UnityTest] public IEnumerator ComposesOnlyViewportAndNativeOverscan()
    {
        yield return Mount();
        Assert.That(State.LayoutInfo.TotalItemsCount, Is.EqualTo(10000));
        Assert.That(_active, Is.InRange(1, 40)); Assert.That(_composed, Is.LessThan(120));
        Assert.That(State.FirstVisibleItemIndex, Is.EqualTo(0)); Near(State.FirstVisibleItemScrollOffset, 0);
        Assert.That(State.LayoutInfo.VisibleItemsInfo.Count, Is.InRange(1, 6));
        Assert.That(_horizontal ? E("viewport").Q<ListView>() == null : E("viewport").Q<ListView>() != null, Is.True);
        Assert.That(Native.contentViewport.resolvedStyle.overflow, Is.EqualTo(Overflow.Hidden));
        Assert.That(State.CanScrollForward, Is.True); Assert.That(State.CanScrollBackward, Is.False);
    }
    [UnityTest] public IEnumerator JumpToUncomposedItemMeasuresAndPositionsIt()
    {
        yield return Mount(); State.ScrollToItem(5000, 13); yield return Settle(15);
        Assert.That(State.FirstVisibleItemIndex, Is.EqualTo(5000)); Near(State.FirstVisibleItemScrollOffset, 13);
        Assert.That(E("item-5000"), Is.Not.Null); Assert.That(_active, Is.LessThan(40));
        Assert.That(_forgotten, Is.GreaterThan(0));
    }
    [UnityTest] public IEnumerator InitialIndexAndOffsetSurviveFirstLayout()
    {
        _state.Value = new LazyListState(1234, 17); yield return Mount(); yield return Settle(10);
        Assert.That(State.FirstVisibleItemIndex, Is.EqualTo(1234)); Near(State.FirstVisibleItemScrollOffset, 17);
    }
    [UnityTest] public IEnumerator PositiveOffsetMovesItemFurtherOutOfViewport()
    {
        yield return Mount(); State.ScrollToItem(10, 20); yield return Settle(12);
        Assert.That(State.FirstVisibleItemIndex, Is.EqualTo(10)); Near(State.FirstVisibleItemScrollOffset, 20);
        State.ScrollToItem(10, -20); yield return Settle(12);
        Assert.That(State.FirstVisibleItemIndex, Is.EqualTo(9)); Near(State.FirstVisibleItemScrollOffset, 40);
    }
    [UnityTest] public IEnumerator ReverseLayoutReversesPhysicalOrder()
    {
        yield return Mount();
        var a = E("item-0").worldBound.position; var b = E("item-1").worldBound.position;
        Assert.That(Axis(b - a) > 0, Is.EqualTo(!_initialReverse));
        Near(Physical, _initialReverse ? State.MaxValue : 0);
    }
    [UnityTest] public IEnumerator WheelUsesNativeScrollingAndUpdatesLayoutInfo()
    {
        yield return Mount(); State.ScrollToItem(20); yield return Settle(12); var before = Physical;
        Wheel(2); yield return Settle(); Assert.That(Physical, Is.GreaterThan(before));
        Assert.That(State.FirstVisibleItemIndex, _initialReverse ? Is.LessThanOrEqualTo(20) : Is.GreaterThanOrEqualTo(20));
    }
    [UnityTest] public IEnumerator DisabledUserInputStillAllowsJumpAndClickableItems()
    {
        _enabled.Value = false; yield return Mount(); State.ScrollToItem(20); yield return Settle(12);
        var before = Physical; Wheel(2); yield return Settle(); Near(Physical, before);
        simulate.Click(E("item-20").worldBound.center); Flush(); Assert.That(_clicks, Is.EqualTo(1));
        Assert.That(State.FirstVisibleItemIndex, Is.EqualTo(20));
    }
    [UnityTest] public IEnumerator VariableItemSizesAreMeasuredAtDistantTarget()
    {
        _variable = true; yield return Mount(); State.ScrollToItem(4321, 7); yield return Settle(16);
        Assert.That(State.FirstVisibleItemIndex, Is.EqualTo(4321)); Near(State.FirstVisibleItemScrollOffset, 7);
        foreach (var info in State.LayoutInfo.VisibleItemsInfo) Near(info.Size, 60 + info.Index % 3 * 20);
    }
    [UnityTest] public IEnumerator PaddingAndSpacingAreReportedAndApplied()
    {
        _padded = _spaced = true; yield return Mount();
        Near(State.LayoutInfo.BeforeContentPadding, 12); Near(State.LayoutInfo.AfterContentPadding, 12);
        Near(State.LayoutInfo.MainAxisItemSpacing, 10); Near(State.LayoutInfo.ViewportStartOffset, -12);
        Near(Math.Abs(Axis(E("item-1").worldBound.position - E("item-0").worldBound.position)), 70);
    }
    [UnityTest] public IEnumerator ShortContentRespectsExplicitCenterArrangement()
    {
        _count.Value = 2; _center = true; yield return Mount(); Near(State.MaxValue, 0);
        var bounds = E("item-0").worldBound; var other = E("item-1").worldBound;
        var start = Math.Min(Axis(bounds.position), Axis(other.position)) - Axis(Native.contentViewport.worldBound.position);
        Near(start, 60);
    }
    [UnityTest] public IEnumerator StableKeysPreserveAnchorAndRememberWhenPrepending()
    {
        yield return Mount(); State.ScrollToItem(100, 11); yield return Settle(12);
        var remembered = _remembered[100]; _prefix.Value = 3; _count.Value += 3; yield return Settle(16);
        Assert.That(State.FirstVisibleItemIndex, Is.EqualTo(103)); Near(State.FirstVisibleItemScrollOffset, 11);
        Assert.That(_remembered[100], Is.SameAs(remembered));
    }
    [UnityTest] public IEnumerator RequestPositionOverridesKeyAnchorAfterDataChange()
    {
        yield return Mount(); State.ScrollToItem(100); yield return Settle(12);
        _prefix.Value = 3; _count.Value += 3; State.RequestScrollToItem(20, 9); yield return Settle(16);
        Assert.That(State.FirstVisibleItemIndex, Is.EqualTo(20)); Near(State.FirstVisibleItemScrollOffset, 9);
    }
    [UnityTest] public IEnumerator EmptyShrinkAndRefillRemainUsable()
    {
        yield return Mount(); State.ScrollToItem(9000); yield return Settle(12);
        _count.Value = 0; yield return Settle(); Near(State.MaxValue, 0);
        Assert.That(State.LayoutInfo.TotalItemsCount, Is.EqualTo(0)); Assert.That(_active, Is.EqualTo(0));
        _count.Value = 100; State.RequestScrollToItem(7); yield return Settle(14);
        Assert.That(State.FirstVisibleItemIndex, Is.EqualTo(7));
    }
    [UnityTest] public IEnumerator IndexBeyondEndClampsAndLastItemIsVisible()
    {
        _count.Value = 20; yield return Mount(); State.ScrollToItem(2000); yield return Settle(14);
        Assert.That(State.LayoutInfo.VisibleItemsInfo.Any(i => i.Index == 19), Is.True);
        Assert.That(State.CanScrollForward, Is.False);
    }
    [UnityTest] public IEnumerator CompositionLocalsReachItemsAndRecompositionKeepsRemember()
    {
        yield return Mount(); Assert.That(E("item-0").Q<Label>()?.text ?? ((Label)E("item-0")).text, Does.Contain("inherited"));
        var token = _remembered[0]; _revision.Value++; yield return Settle(); Assert.That(_remembered[0], Is.SameAs(token));
    }
    [UnityTest] public IEnumerator ResizeAndItemSizeChangeRecomputeVisibleWindow()
    {
        yield return Mount(); _size.Value = 120; _itemSize.Value = 80; yield return Settle(12);
        Near(State.ViewportSize, 120); Assert.That(State.LayoutInfo.VisibleItemsInfo.Count, Is.InRange(1, 3));
        foreach (var info in State.LayoutInfo.VisibleItemsInfo) Near(info.Size, 80);
    }
    [UnityTest] public IEnumerator StateReplacementAndRemovalReleaseCompositions()
    {
        yield return Mount(); _state.Value = new LazyListState(50, 5); yield return Settle(14);
        Assert.That(State.FirstVisibleItemIndex, Is.EqualTo(50)); Near(State.FirstVisibleItemScrollOffset, 5);
        _present.Value = false; yield return Settle(); Assert.That(_active, Is.EqualTo(0));
        Assert.That(rootVisualElement.Q("viewport"), Is.Null);
    }
    [UnityTest] public IEnumerator StickyHeaderStaysAtLeadingEdge()
    {
        _sticky = true; yield return Mount(); State.ScrollToItem(4, 10); yield return Settle(14);
        var pos = Axis(E("item-0").worldBound.position) - Axis(Native.contentViewport.worldBound.position);
        Near(pos, _initialReverse ? State.ViewportSize - 60 : 0);
        Assert.That(_active, Is.LessThan(30));
        Assert.That(State.LayoutInfo.VisibleItemsInfo.Any(i => i.Index == 0), Is.True);
    }
    [UnityTest] public IEnumerator EnsureVisibleDoesNotMoveAlreadyVisibleItem()
    {
        yield return Mount(); State.ScrollToItem(20); yield return Settle(12); var before = Physical;
        State.ScrollToItemVisibility(21); yield return Settle(); Near(Physical, before);
    }
    [UnityTest] public IEnumerator AnimateScrollToVisibleTargetCompletesOnFrameClock()
    {
        _count.Value = 40; yield return Mount();
        var task = State.AnimateScrollToItem(2, 0, Tween(100)); Flush(); Flush(50); yield return Settle(2); Flush(60); yield return Settle(10);
        Assert.That(task.IsCompletedSuccessfully, Is.True); Assert.That(State.FirstVisibleItemIndex, Is.EqualTo(2));
    }
    [UnityTest] public IEnumerator AnimateToDistantItemMovesThroughIntermediateOffsets()
    {
        yield return Mount(); var before = State.Value;
        var task = State.AnimateScrollToItem(500, 9, Tween(200)); yield return Settle(2); Flush(90); yield return Settle(2);
        Assert.That(State.Value, Is.GreaterThan(before)); Assert.That(task.IsCompleted, Is.False);
        Flush(130); yield return Settle(18);
        Assert.That(task.IsCompletedSuccessfully, Is.True); Assert.That(State.FirstVisibleItemIndex, Is.EqualTo(500)); Near(State.FirstVisibleItemScrollOffset, 9);
    }
    [UnityTest] public IEnumerator ANewJumpCancelsAnimationAndOwnsFinalPosition()
    {
        yield return Mount(); var task = State.AnimateScrollToItem(500, animationSpec: Tween(300));
        yield return Settle(2); Flush(90); State.ScrollToItem(25, 4); yield return Settle(16); Flush(500);
        Assert.That(task.IsCanceled, Is.True); Assert.That(State.FirstVisibleItemIndex, Is.EqualTo(25)); Near(State.FirstVisibleItemScrollOffset, 4);
    }
    [UnityTest] public IEnumerator UserWheelInterruptsProgrammaticAnimation()
    {
        yield return Mount(); State.ScrollToItem(30); yield return Settle(12);
        var task = State.AnimateScrollToItem(500, animationSpec: Tween(300)); yield return Settle(2); Flush(90);
        Wheel(1); yield return Settle(); Assert.That(task.IsCanceled, Is.True);
    }
    [UnityTest] public IEnumerator FillParentUsesViewportAfterContentPadding()
    {
        _fillParent = _padded = true; yield return Mount(); yield return Settle();
        foreach (var info in State.LayoutInfo.VisibleItemsInfo) Near(info.Size, (State.ViewportSize - 24) / 2);
        _size.Value = 160; yield return Settle(12);
        foreach (var info in State.LayoutInfo.VisibleItemsInfo) Near(info.Size, 68);
    }
    [UnityTest] public IEnumerator CrossAxisAlignmentPreservesIntrinsicItemSize()
    {
        _crossEnd = true; yield return Mount(); var item = E("item-0");
        var far = _horizontal ? item.worldBound.yMax : item.worldBound.xMax;
        var viewportFar = _horizontal ? Native.contentViewport.worldBound.yMax : Native.contentViewport.worldBound.xMax;
        Near(far, viewportFar);
    }
    [UnityTest] public IEnumerator RemovingAndRestoringListRetainsExternalState()
    {
        yield return Mount(); State.ScrollToItem(75, 3); yield return Settle(12);
        _present.Value = false; yield return Settle(); _present.Value = true; yield return Settle(16);
        Assert.That(State.FirstVisibleItemIndex, Is.EqualTo(75)); Near(State.FirstVisibleItemScrollOffset, 3);
    }
    [UnityTest] public IEnumerator TouchUsesNativeCaptureAndDisabledTouchDoesNotScroll()
    {
        yield return Mount(); State.ScrollToItem(20); yield return Settle(12); var before = Physical;
        var origin = Native.contentViewport.worldBound.center; var movement = _horizontal ? new Vector2(50, 0) : new Vector2(0, 50);
        Touch(TouchPhase.Began, origin); Touch(TouchPhase.Moved, origin - movement); Touch(TouchPhase.Ended, origin - movement);
        yield return Settle(); Assert.That(Physical, Is.GreaterThan(before));
        _enabled.Value = false; yield return Settle(); before = Physical;
        Touch(TouchPhase.Began, origin); Touch(TouchPhase.Moved, origin - movement); Touch(TouchPhase.Ended, origin - movement);
        yield return Settle(); Near(Physical, before);
    }
    [UnityTest] public IEnumerator KeyboardNavigationScrollsAndHonorsInputGate()
    {
        yield return Mount(); State.ScrollToItem(20); yield return Settle(12); var before = Physical;
        using (var evt = NavigationMoveEvent.GetPooled(_horizontal ? Vector2.right : Vector2.down, EventModifiers.None))
        { evt.target = Native; Native.SendEvent(evt); } yield return Settle(); Assert.That(Physical, Is.GreaterThan(before));
        _enabled.Value = false; yield return Settle(); before = Physical;
        using (var evt = NavigationMoveEvent.GetPooled(_horizontal ? Vector2.right : Vector2.down, EventModifiers.None))
        { evt.target = Native; Native.SendEvent(evt); } yield return Settle(); Near(Physical, before);
    }
    [UnityTest] public IEnumerator NegativeSpacingOverlapsAdjacentItemsWithoutBreakingJumps()
    {
        _spaced = _negativeSpace = true; yield return Mount();
        Near(Math.Abs(Axis(E("item-1").worldBound.position - E("item-0").worldBound.position)), 50);
        State.ScrollToItem(100, 4); yield return Settle(14);
        Assert.That(State.LayoutInfo.VisibleItemsInfo.Any(i => i.Index == 100), Is.True);
        Near(State.LayoutInfo.MainAxisItemSpacing, -10);
    }
    [UnityTest] public IEnumerator NativePhysicsSettingsAndDisabledGateAreApplied()
    {
        _fling = new FlingBehavior(.25f); _overscroll = new OverscrollEffect(.2f); yield return Mount();
        // Initial positioning stops existing native motion. New input restores configured physics.
        Wheel(_initialReverse ? -1 : 1); yield return Settle();
        Assert.That(Native.scrollDecelerationRate, Is.EqualTo(.25f));
        Assert.That(Native.elasticity, Is.EqualTo(.2f)); Assert.That(Native.touchScrollBehavior, Is.EqualTo(ScrollView.TouchScrollBehavior.Elastic));
        _enabled.Value = false; yield return Settle(); Assert.That(Native.scrollDecelerationRate, Is.Zero);
        Assert.That(Native.touchScrollBehavior, Is.EqualTo(ScrollView.TouchScrollBehavior.Clamped));
    }
    [UnityTest] public IEnumerator NativeDragEmitsPairedInteractionsAndTapDoesNot()
    {
        yield return Mount(); State.ScrollToItem(20); yield return Settle(12);
        var interactions = new List<IInteraction>(); using var cancellation = new CancellationTokenSource();
        _ = State.InteractionSource.Interactions.Collect(interactions.Add, cancellation.Token);
        var origin = Native.contentViewport.worldBound.center;
        Touch(TouchPhase.Began, origin); Touch(TouchPhase.Ended, origin); yield return Settle();
        Assert.That(interactions.OfType<IDragInteraction.Start>().Count(), Is.Zero);
        var movement = _horizontal ? new Vector2(50, 0) : new Vector2(0, 50);
        Touch(TouchPhase.Began, origin); Touch(TouchPhase.Moved, origin - movement); Touch(TouchPhase.Ended, origin - movement);
        var deadline = DateTime.UtcNow.AddSeconds(2);
        while (!interactions.OfType<IDragInteraction.Stop>().Any() && DateTime.UtcNow < deadline) { yield return null; Flush(); }
        Assert.That(interactions.OfType<IDragInteraction.Start>().Count(), Is.EqualTo(1));
        Assert.That(interactions.OfType<IDragInteraction.Stop>().Count(), Is.EqualTo(1));
    }
    [UnityTest] public IEnumerator UnspecifiedCrossAxisWrapsVisibleContent()
    {
        _autoCross = true; yield return Mount(); yield return Settle();
        Near(_horizontal ? E("viewport").layout.height : E("viewport").layout.width, _horizontal ? 90 : 100);
        Assert.That(State.LayoutInfo.VisibleItemsInfo.Count, Is.GreaterThan(0));
    }
    [UnityTest] public IEnumerator InitialIndexWaitsForAsynchronouslyLoadedData()
    {
        _count.Value = 0; _state.Value = new LazyListState(33, 7); yield return Mount();
        Assert.That(State.FirstVisibleItemIndex, Is.EqualTo(33)); Assert.That(_active, Is.Zero);
        _count.Value = 100; yield return Settle(18);
        Assert.That(State.FirstVisibleItemIndex, Is.EqualTo(33)); Near(State.FirstVisibleItemScrollOffset, 7);
    }
    [UnityTest] public IEnumerator RawPixelJumpSupersedesAnUnmeasuredItemRequest()
    {
        yield return Mount(); State.ScrollToItem(5000); State.ScrollTo(0); yield return Settle(16);
        Near(State.Value, 0); Assert.That(State.FirstVisibleItemIndex, Is.EqualTo(0));
    }
    [UnityTest] public IEnumerator AnimatedStickyHeaderTargetCompletes()
    {
        _sticky = true; yield return Mount();
        var task = State.AnimateScrollToItem(9, animationSpec: Tween(100)); yield return Settle(2); Flush(120); yield return Settle(20);
        Assert.That(task.IsCompletedSuccessfully, Is.True); Assert.That(State.FirstVisibleItemIndex, Is.EqualTo(9), $"value={State.Value} physical={Physical}; " + string.Join(",", State.LayoutInfo.VisibleItemsInfo.Select(i => $"{i.Index}@{i.Offset}:{i.Size}")));
    }
    private void Touch(TouchPhase phase, Vector2 position)
    {
        var touch = new Touch { fingerId = 0, position = position, phase = phase, pressure = 1 };
        EventBase evt = phase == TouchPhase.Began ? PointerDownEvent.GetPooled(touch, EventModifiers.None)
            : phase == TouchPhase.Moved ? PointerMoveEvent.GetPooled(touch, EventModifiers.None)
            : PointerUpEvent.GetPooled(touch, EventModifiers.None);
        using (evt)
        {
            var target = Native.contentContainer.HasPointerCapture(UnityEngine.UIElements.PointerId.touchPointerIdBase)
                ? Native.contentContainer : rootVisualElement.panel.Pick(position);
            evt.target = target; target.SendEvent(evt);
        }
        Flush();
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
