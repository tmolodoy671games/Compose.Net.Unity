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
using Color = Compose.Net.Color;

namespace Compose.Net.Unity.Tests;

[TestFixture(false, false), TestFixture(false, true), TestFixture(true, false), TestFixture(true, true)]
public sealed partial class ScrollModifierTests : UITestFixture
{
    private readonly bool _horizontal, _reverseInitially;
    private IComposer _composer = null!;
    private DemoAdapter _adapter = null!;
    private IMutableState<IScrollState> _state = null!;
    private IMutableState<bool> _enabled = null!, _reverse = null!, _present = null!;
    private IMutableState<int> _count = null!, _size = null!, _revision = null!;
    private IMutableState<bool> _row = null!;
    private IScrollState State => _state.Value;
    private bool _nested, _cross, _box, _padded, _compact;
    private IMutableState<bool> _align = null!;
    private IScrollState? _inner;
    private int _clicks, _compositions;
    private FlingBehavior? _fling;
    private OverscrollEffect? _overscroll;
    private SynchronizationContext? _previousContext;
    private readonly UiContext _context = new();
    private readonly List<IInteraction> _interactions = new();
    private CancellationTokenSource? _collect;
    private ScrollView Native => E("viewport").Q<ScrollView>();
    private float Physical => _horizontal ? Native.scrollOffset.x : Native.scrollOffset.y;
    private float Expected => _reverse.Value ? State.MaxValue - State.Value : State.Value;

    public ScrollModifierTests(bool horizontal, bool reverse) { _horizontal = horizontal; _reverseInitially = reverse; }
    [SetUp] public void Setup()
    {
        _previousContext = SynchronizationContext.Current; SynchronizationContext.SetSynchronizationContext(_context);
        panelSize = new Vector2(700, 500); simulate.timePerSimulatedFrameMs = 0;
        _adapter = new DemoAdapter(); _state = MutableStateOf<IScrollState>(new ScrollState());
        _enabled = MutableStateOf(true); _reverse = MutableStateOf(_reverseInitially); _present = MutableStateOf(true);
        _count = MutableStateOf(10); _size = MutableStateOf(120); _revision = MutableStateOf(0);
        _row = MutableStateOf(_horizontal); _clicks = _compositions = 0;
        _align = MutableStateOf(false); _box = _padded = _compact = false;
        _nested = _cross = false; _inner = null; _fling = null; _overscroll = null;
        _interactions.Clear();
    }
    [TearDown] public void Teardown()
    {
        _composer?.Dispose(); _collect?.Cancel(); _collect?.Dispose(); _collect = null;
        _context.Drain(); SynchronizationContext.SetSynchronizationContext(_previousContext);
        simulate.ResetTimePerSimulatedFrameToDefault();
    }
    private IEnumerator Mount()
    {
        _composer = IComposer.Create();
        Bootstrap(_composer, _adapter, false, new ComposeConfig(Logging: false), new RootNode(rootVisualElement), Content);
        _collect = new CancellationTokenSource();
        _ = State.InteractionSource.Interactions.Collect(_interactions.Add, _collect.Token);
        yield return Settle();
        Assert.That(Native, Is.Not.Null);
        Near(State.ViewportSize, _size.Value - (_padded ? 20 : 0));
    }
    [Composable] private void Content()
    {
        _compositions++;
        var unused = _revision.Value;
        var state = Remember(_state.Value, () => _state.Value);
        var modifier = Modifier.Size((_horizontal ? _size.Value : 200).Dp(), (_horizontal ? 200 : _size.Value).Dp()).TestTag("viewport");
        if (_padded) modifier = modifier.Padding(10.Dp());
        if (_present.Value)
            modifier = _horizontal
                ? modifier.HorizontalScroll(state, _overscroll, _enabled.Value, _fling, _reverse.Value)
                : modifier.VerticalScroll(state, _overscroll, _enabled.Value, _fling, _reverse.Value);
        if (_box) Box(modifier: modifier, content: () => Box(modifier: Modifier
            .Size((_horizontal ? 600 : 200).Dp(), (_horizontal ? 200 : 600).Dp()).TestTag("box-child"), content: () => { }));
        else if (_row.Value) Row(modifier: modifier, verticalAlignment: _align.Value ? Alignment.Bottom : Alignment.Top, content: Items);
        else Column(modifier: modifier, horizontalAlignment: _align.Value ? Alignment.Right : Alignment.Left, content: Items);
    }
    [Composable] private void Items()
    {
        if (_nested)
        {
            _inner = RememberScrollState();
            var innerHorizontal = _cross ? !_horizontal : _horizontal;
            var modifier = Modifier.Size((_horizontal ? 100 : 180).Dp(), (_horizontal ? 180 : 100).Dp()).TestTag("inner");
            modifier = innerHorizontal ? modifier.HorizontalScroll(_inner, overscrollEffect: null) : modifier.VerticalScroll(_inner, overscrollEffect: null);
            if (innerHorizontal) Row(modifier: modifier, content: () => InnerItems(true));
            else Column(modifier: modifier, content: () => InnerItems(false));
        }
        for (var i = 0; i < _count.Value; i++)
        {
            var index = i;
            Box(modifier: Modifier.Size((_horizontal ? 60 : _compact ? 80 : 200).Dp(), (_horizontal ? _compact ? 80 : 200 : 60).Dp())
                .Background(index % 2 == 0 ? Color.DodgerBlue : Color.Gray).TestTag("item-" + index)
                .Clickable(() => _clicks++), content: () => { });
        }
    }
    [Composable] private static void InnerItems(bool horizontal)
    {
        for (var i = 0; i < 8; i++)
            Box(modifier: Modifier.Size((horizontal ? 60 : 180).Dp(), (horizontal ? 180 : 60).Dp())
                .Background(Color.Orange).TestTag("inner-" + i), content: () => { });
    }
    private VisualElement E(string tag) => rootVisualElement.Q(tag) ?? throw new AssertionException("Missing " + tag);
    private void Flush(int milliseconds = 0)
    {
        SynchronizationContext.SetSynchronizationContext(_context); _context.Drain(); _adapter.Clock.Advance(milliseconds);
        for (var i = 0; i < 6; i++) { _context.Drain(); _adapter.Invalidator.Tick(); simulate.FrameUpdateMs(16); }
    }
    private IEnumerator Settle()
    {
        for (var i = 0; i < 5; i++) { yield return null; Flush(); }
    }
    private static void Near(float actual, float expected) => Assert.That(actual, Is.EqualTo(expected).Within(.6f));
    private void Wheel(float direction, VisualElement? target = null)
    {
        simulate.ScrollWheel(_horizontal ? new Vector2(direction, 0) : new Vector2(0, direction),
            (target ?? E("viewport")).worldBound.center);
        Flush();
    }
    [UnityTest] public IEnumerator MeasuresNativeRangeAndStartsAtCorrectEdge()
    {
        yield return Mount();
        Near(State.MaxValue, 480); Near(State.ContentSize, 600); Near(State.Value, 0); Near(Physical, Expected);
        Assert.That(Native.horizontalScrollerVisibility, Is.EqualTo(ScrollerVisibility.Hidden));
        Assert.That(Native.verticalScrollerVisibility, Is.EqualTo(ScrollerVisibility.Hidden));
        Assert.That(Native.contentViewport.resolvedStyle.overflow, Is.EqualTo(Overflow.Hidden));
    }
    [UnityTest] public IEnumerator InitialOffsetSurvivesFirstMeasure()
    {
        _state.Value = new ScrollState(135);
        yield return Mount(); Near(State.Value, 135); Near(Physical, Expected);
    }
    [UnityTest] public IEnumerator InitialOffsetBeyondEndClamps()
    {
        _state.Value = new ScrollState(10000);
        yield return Mount(); Near(State.Value, State.MaxValue); Near(Physical, Expected);
    }
    [UnityTest] public IEnumerator ProgrammaticScrollUpdatesNativeWithoutRecomposition()
    {
        yield return Mount(); var compositions = _compositions;
        State.ScrollTo(80); Near(Physical, Expected); Flush();
        Near(State.Value, 80); Assert.That(_compositions, Is.EqualTo(compositions));
        State.ScrollBy(40); Near(State.Value, 120); Near(Physical, Expected);
        State.ScrollTo(-1); Near(State.Value, 0); Near(Physical, Expected);
        State.ScrollTo(10000); Near(State.Value, State.MaxValue); Near(Physical, Expected);
    }
    [UnityTest] public IEnumerator WheelMovesNativeAndLogicalStateTogether()
    {
        yield return Mount(); State.ScrollTo(200); var before = Physical;
        Wheel(1); Assert.That(Physical, Is.GreaterThan(before)); Near(Physical, Expected);
        Assert.That(_reverse.Value ? State.LastScrolledBackward : State.LastScrolledForward, Is.True);
        Wheel(-1); Near(Physical, before);
    }
    [UnityTest] public IEnumerator DisabledInputPreservesLayoutAndProgrammaticScrolling()
    {
        _enabled.Value = false; yield return Mount(); State.ScrollTo(150);
        Wheel(2); Near(State.Value, 150); Near(State.MaxValue, 480); Near(Physical, Expected);
        var animation = State.AnimateScrollTo(200, Tween(100)); Flush(100);
        Assert.That(animation.IsCompletedSuccessfully, Is.True); Near(State.Value, 200); Near(Physical, Expected);
    }
    [UnityTest] public IEnumerator DisabledChildrenRemainClickable()
    {
        _enabled.Value = false; _reverse.Value = false; yield return Mount();
        simulate.Click(E("item-0")); yield return Settle(); Assert.That(_clicks, Is.EqualTo(1));
    }
    [UnityTest] public IEnumerator ToggleEnabledReusesNativeViewAndCurrentPosition()
    {
        yield return Mount(); State.ScrollTo(150); var view = Native;
        for (var i = 0; i < 3; i++)
        {
            _enabled.Value = false; yield return Settle(); Wheel(1); Near(State.Value, 150);
            _enabled.Value = true; yield return Settle(); Assert.That(Native, Is.SameAs(view));
            Wheel(1); Assert.That(State.Value, Is.Not.EqualTo(150)); State.ScrollTo(150);
        }
    }
    [UnityTest] public IEnumerator ReverseToggleChangesOriginWithoutReorderingChildren()
    {
        yield return Mount(); State.ScrollTo(75); var first = E("item-0"); var second = E("item-1");
        _reverse.Value = !_reverse.Value; yield return Settle(); Near(State.Value, 75); Near(Physical, Expected);
        Assert.That(E("item-0"), Is.SameAs(first));
        Assert.That(_horizontal ? first.layout.x < second.layout.x : first.layout.y < second.layout.y, Is.True);
    }
    [UnityTest] public IEnumerator ContentShrinkGrowAndEmptyUpdateBounds()
    {
        yield return Mount(); State.ScrollTo(State.MaxValue);
        _count.Value = 4; yield return Settle(); Near(State.MaxValue, 120); Near(State.Value, 120); Near(Physical, Expected);
        _count.Value = 10; yield return Settle(); Near(State.MaxValue, 480); Near(State.Value, 120); Near(Physical, Expected);
        _count.Value = 0; yield return Settle(); Near(State.MaxValue, 0); Near(State.Value, 0);
        _count.Value = 10; yield return Settle(); Near(State.MaxValue, 480); Near(Physical, Expected);
    }
    [UnityTest] public IEnumerator ResizeRecomputesViewportAndClampsPosition()
    {
        yield return Mount(); State.ScrollTo(State.MaxValue);
        _size.Value = 300; yield return Settle(); Near(State.ViewportSize, 300); Near(State.MaxValue, 300);
        Near(State.Value, 300); Near(Physical, Expected);
    }
    [UnityTest] public IEnumerator ShortContentDoesNotConsumeWheel()
    {
        _count.Value = 1; yield return Mount(); var bubbled = 0;
        rootVisualElement.RegisterCallback<WheelEvent>(_ => bubbled++);
        Wheel(1); Near(State.MaxValue, 0); Near(State.Value, 0); Assert.That(bubbled, Is.EqualTo(1));
    }
    [UnityTest] public IEnumerator AnimationAndWheelInterruptEachOther()
    {
        yield return Mount(); var task = State.AnimateScrollTo(400, Tween(1000, easing: LinearEasing));
        Flush(250); Near(State.Value, 100); Near(Physical, Expected); Assert.That(State.IsScrollInProgress, Is.True);
        Wheel(_reverse.Value ? -1 : 1); Flush(); Assert.That(task.IsCanceled, Is.True);
        var value = State.Value; Flush(1000); Near(State.Value, value);
    }
    [UnityTest] public IEnumerator ReplacingStateUnsubscribesOldState()
    {
        yield return Mount(); var old = State; var native = Native;
        _state.Value = new ScrollState(30); yield return Settle(); Near(State.Value, 30); Near(Physical, Expected);
        old.ScrollTo(350); Flush(); Near(State.Value, 30); Near(Physical, Expected); Assert.That(Native, Is.SameAs(native));
    }
    [UnityTest] public IEnumerator RemoveAndReaddRestoresHierarchyWithoutDuplicates()
    {
        yield return Mount(); var child = E("item-0"); State.ScrollTo(75);
        for (var i = 0; i < 3; i++)
        {
            _present.Value = false; yield return Settle(); Assert.That(E("viewport").Q<ScrollView>(), Is.Null);
            Assert.That(E("item-0"), Is.SameAs(child));
            _present.Value = true; yield return Settle(); Assert.That(E("viewport").Query<ScrollView>().ToList().Count, Is.EqualTo(1));
            Near(State.Value, 75); Near(Physical, Expected);
        }
    }
    [UnityTest] public IEnumerator ContentDirectionIsIndependentOfScrollAxis()
    {
        _row.Value = !_horizontal; yield return Mount();
        var first = E("item-0"); var second = E("item-1");
        Assert.That(_row.Value ? second.layout.x > first.layout.x : second.layout.y > first.layout.y, Is.True);
    }
    [UnityTest] public IEnumerator ClippingPreventsClickingOffscreenItems()
    {
        _reverse.Value = false; yield return Mount();
        var offscreen = E("item-4"); var picked = rootVisualElement.panel.Pick(offscreen.worldBound.center);
        Assert.That(picked == offscreen || (picked != null && offscreen.Contains(picked)), Is.False);
        simulate.Click(offscreen.worldBound.center); yield return Settle(); Assert.That(_clicks, Is.Zero);
    }
    [UnityTest] public IEnumerator NestedScrollMovesInnerBeforeOuterAndForwardsAtBoundary()
    {
        _nested = true; _reverse.Value = false; yield return Mount();
        var inner = E("inner"); Wheel(1, inner); Assert.That(_inner!.Value, Is.GreaterThan(0)); Near(State.Value, 0);
        _inner.ScrollTo(_inner.MaxValue); Wheel(1, inner); Assert.That(State.Value, Is.GreaterThan(0));
    }
    [UnityTest] public IEnumerator CrossAxisNestedScrollDoesNotStealOuterAxis()
    {
        _nested = _cross = true; _reverse.Value = false; yield return Mount();
        Wheel(1, E("inner")); Assert.That(State.Value, Is.GreaterThan(0)); Near(_inner!.Value, 0);
    }
    [UnityTest] public IEnumerator NativePhysicsOptionsAreApplied()
    {
        _fling = new FlingBehavior(.25f); _overscroll = new OverscrollEffect(.2f); yield return Mount();
        Near(Native.scrollDecelerationRate, .25f); Near(Native.elasticity, .2f);
        Assert.That(Native.touchScrollBehavior, Is.EqualTo(ScrollView.TouchScrollBehavior.Elastic));
        Assert.That(Native.nestedInteractionKind, Is.EqualTo(ScrollView.NestedInteractionKind.ForwardScrolling));
    }
    [UnityTest] public IEnumerator TouchDragUsesNativeCaptureAndPairsInteractions()
    {
        _reverse.Value = false; _fling = new FlingBehavior(0); yield return Mount();
        var origin = Native.contentViewport.worldBound.center;
        Touch(TouchPhase.Began, origin);
        Touch(TouchPhase.Moved, origin - (_horizontal ? new Vector2(35, 0) : new Vector2(0, 35)));
        yield return Settle(); Assert.That(State.Value, Is.GreaterThan(0));
        yield return WaitFor(() => _interactions.OfType<IDragInteraction.Start>().Count() == 1);
        Touch(TouchPhase.Ended, origin - (_horizontal ? new Vector2(35, 0) : new Vector2(0, 35)));
        yield return WaitFor(() => _interactions.OfType<IDragInteraction.Stop>().Count() == 1);
        Assert.That(_clicks, Is.Zero);
    }
    [UnityTest] public IEnumerator DisabledTouchDoesNotMoveOrDisableChildTap()
    {
        _enabled.Value = false; _reverse.Value = false; yield return Mount();
        var origin = Native.contentViewport.worldBound.center;
        Touch(TouchPhase.Began, origin); Touch(TouchPhase.Moved, origin - new Vector2(35, 35)); Touch(TouchPhase.Ended, origin - new Vector2(35, 35));
        yield return Settle(); Near(State.Value, 0); Assert.That(_interactions.OfType<IDragInteraction.Start>(), Is.Empty);
    }
    [UnityTest] public IEnumerator DisposalCancelsAnimationAndUnhooksView()
    {
        yield return Mount(); var state = State; var view = Native;
        var animation = State.AnimateScrollTo(400, Tween(1000)); Flush(100);
        _composer.Dispose(); Flush(); Assert.That(animation.IsCanceled, Is.True);
        var offset = view.scrollOffset; state.ScrollTo(100); Assert.That(view.scrollOffset, Is.EqualTo(offset));
    }
    [UnityTest] public IEnumerator DisabledNestedScrollForwardsWheelToParent()
    {
        _nested = true; _reverse.Value = false; yield return Mount();
        _enabled.Value = false; yield return Settle();
        // The inner native scroller remains enabled although its outer modifier is disabled.
        Wheel(1, E("inner")); Assert.That(_inner!.Value, Is.GreaterThan(0)); Near(State.Value, 0);
    }
    [UnityTest] public IEnumerator KeyboardNavigationUsesNativeScrollView()
    {
        _reverse.Value = false; yield return Mount();
        Navigate(); Assert.That(State.Value, Is.GreaterThan(0)); Near(Physical, Expected);
        _enabled.Value = false; yield return Settle(); var value = State.Value;
        Navigate(); Near(State.Value, value);
    }
    private void Navigate()
    {
        Native.Focus();
        using var evt = NavigationMoveEvent.GetPooled(_horizontal ? Vector2.right : Vector2.down, EventModifiers.None);
        evt.target = Native; Native.SendEvent(evt); Flush();
    }
    [UnityTest] public IEnumerator ProgrammaticJumpInterruptsNativeFling()
    {
        _reverse.Value = false; yield return Mount();
        var origin = Native.contentViewport.worldBound.center;
        var movement = _horizontal ? new Vector2(45, 0) : new Vector2(0, 45);
        Touch(TouchPhase.Began, origin); Touch(TouchPhase.Moved, origin - movement);
        Touch(TouchPhase.Ended, origin - movement);
        State.ScrollTo(125); yield return Settle(); Flush(500); Near(State.Value, 125); Near(Physical, Expected);
        Assert.That(State.IsScrollInProgress, Is.False);
    }
    [UnityTest] public IEnumerator DisablingDuringDragCancelsCaptureAndStopsMotion()
    {
        _reverse.Value = false; yield return Mount();
        var origin = Native.contentViewport.worldBound.center;
        var movement = _horizontal ? new Vector2(30, 0) : new Vector2(0, 30);
        Touch(TouchPhase.Began, origin); Touch(TouchPhase.Moved, origin - movement);
        _enabled.Value = false; yield return Settle(); var value = State.Value;
        Touch(TouchPhase.Moved, origin - movement * 2); Touch(TouchPhase.Ended, origin - movement * 2);
        yield return Settle(); Near(State.Value, value);
        yield return WaitFor(() => _interactions.OfType<IDragInteraction.Cancel>().Count() == 1);
    }
    [UnityTest] public IEnumerator FractionalScrollIsNotRoundedAway()
    {
        yield return Mount(); State.ScrollTo(100.25f); Near(Physical, Expected);
        Assert.That(State.ScrollBy(.125f), Is.EqualTo(.125f));
        Assert.That(State.Value, Is.EqualTo(100.375f)); Near(Physical, Expected);
    }
    [UnityTest] public IEnumerator BoxContentUsesNativeViewportAndPreservesStacking()
    {
        _box = true; yield return Mount(); Near(State.MaxValue, 480);
        State.ScrollTo(250); Near(Physical, Expected);
        Assert.That(E("box-child").layout.size, Is.EqualTo(_horizontal ? new Vector2(600,200) : new Vector2(200,600)));
    }
    [UnityTest] public IEnumerator PaddingReducesViewportAndClippingRemainsCorrect()
    {
        _padded = true; yield return Mount(); Near(State.ViewportSize, 100); Near(State.MaxValue, 500);
        State.ScrollTo(100); Near(Physical, Expected);
    }
    [UnityTest] public IEnumerator CrossAxisAlignmentChangesWithoutResettingScroll()
    {
        _compact = true; yield return Mount(); State.ScrollTo(100);
        var child = E("item-0"); var before = _horizontal ? child.layout.y : child.layout.x;
        _align.Value = true; yield return Settle();
        Assert.That(_horizontal ? child.layout.y : child.layout.x, Is.GreaterThan(before + 100));
        Near(State.Value, 100); Near(Physical, Expected);
    }
    private IEnumerator WaitFor(Func<bool> ready)
    {
        var deadline = DateTime.UtcNow.AddSeconds(2);
        while (!ready() && DateTime.UtcNow < deadline) { yield return null; Flush(); }
        Assert.That(ready(), Is.True, "Interaction flow did not deliver the expected event: " + string.Join(", ", _interactions));
    }
    [UnityTest] public IEnumerator ReplacingStateDuringNativeFlingStopsOldMotion()
    {
        _reverse.Value = false; yield return Mount();
        var origin = Native.contentViewport.worldBound.center;
        var movement = _horizontal ? new Vector2(45, 0) : new Vector2(0, 45);
        Touch(TouchPhase.Began, origin); Touch(TouchPhase.Moved, origin - movement); Touch(TouchPhase.Ended, origin - movement);
        _state.Value = new ScrollState(125); yield return Settle(); Flush(500);
        Near(State.Value, 125); Near(Physical, Expected);
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
