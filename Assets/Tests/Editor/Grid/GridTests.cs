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
using Compose.Net.Unity.Samples.Behaviors.Content;

namespace Compose.Net.Unity.Tests;

[TestFixture(false, false), TestFixture(false, true), TestFixture(true, false), TestFixture(true, true)]
public sealed partial class GridTests : UITestFixture
{
    private readonly bool _horizontal, _initialReverse;
    private IComposer _composer = null!;
    private DemoAdapter _adapter = null!;
    private IMutableState<ILazyGridState> _state = null!;
    private IMutableState<bool> _enabled = null!, _present = null!;
    private IMutableState<int> _count = null!, _revision = null!, _prefix = null!, _size = null!, _cross = null!;
    private GridCells _cells = null!;
    private GridDemoState? _gallery;
    private bool _nested;
    private IScrollState? _outer;
    private Arrangement? _mainArrangement, _crossArrangement;
    private bool _variable, _padded, _spaced, _spans, _sticky, _center;
    private int _clicks, _active, _composed;
    private readonly Dictionary<int, object> _remembered = new();
    private SynchronizationContext? _previousContext;
    private readonly UiContext _context = new();
    private ILazyGridState State => _state.Value;
    private ScrollView Native => E("viewport").Q<ScrollView>();
    private float Axis(Vector2 v) => _horizontal ? v.x : v.y;
    private float Cross(Vector2 v) => _horizontal ? v.y : v.x;
    private float Physical => Axis(Native.scrollOffset);
    private static readonly ICompositionLocal<string> ItemLocal = CompositionLocalOf(() => "missing");
    public GridTests(bool horizontal, bool reverse) { _horizontal = horizontal; _initialReverse = reverse; }
    [SetUp] public void Setup()
    {
        _previousContext = SynchronizationContext.Current; SynchronizationContext.SetSynchronizationContext(_context);
        panelSize = new Vector2(700, 500); simulate.timePerSimulatedFrameMs = 0;
        _adapter = new DemoAdapter(); _state = MutableStateOf<ILazyGridState>(new LazyGridState());
        _enabled = MutableStateOf(true); _present = MutableStateOf(true);
        _count = MutableStateOf(10000); _revision = MutableStateOf(0); _prefix = MutableStateOf(0);
        _size = MutableStateOf(60); _cross = MutableStateOf(300); _cells = GridCells.Fixed(3);
        _variable = _padded = _spaced = _spans = _sticky = _center = false;
        _nested=false; _outer=null; _gallery = null; _mainArrangement = _crossArrangement = null;
        _clicks = _active = _composed = 0; _remembered.Clear();
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
        yield return Settle(16);
    }
    [Composable] private void Content()
    {
        if (!_nested) { GridContent(); return; }
        _outer ??= new ScrollState();
        if (_horizontal) Row(modifier:Modifier.Size(180.Dp(),300.Dp()).HorizontalScroll(_outer), content:()=> { GridContent(); Spacer(Modifier.Size(600.Dp(),100.Dp())); });
        else Column(modifier:Modifier.Size(300.Dp(),180.Dp()).VerticalScroll(_outer), content:()=> { GridContent(); Spacer(Modifier.Size(100.Dp(),600.Dp())); });
    }
    [Composable] private void GridContent()
    {
        if (_gallery != null) { GridDemo.Screen(_gallery); return; }
        if (!_present.Value) return;
        var revision = _revision.Value; var count = _count.Value; var prefix = _prefix.Value; var size = _size.Value;
        var modifier = Modifier.Size((_horizontal ? 240 : _cross.Value).Dp(), (_horizontal ? _cross.Value : 240).Dp()).TestTag("viewport");
        CompositionLocalProvider(ItemLocal.Provides("inherited"), () =>
        {
            Action<ILazyGridScope> content = scope =>
            {
                if (_sticky)
                {
                    for (var section = 0; section < 4; section++)
                    {
                        var part = section;
                        scope.StickyHeader(i => Item(i, size), "header-" + section);
                        scope.Items(12, i => Item(part * 13 + i + 1, size), i => part * 13 + i + 1);
                    }
                }
                else scope.Items(count, i => Item(i - prefix, size), i => i - prefix,
                    span: _spans ? (s, i) => new GridItemSpan(i % 7 == 0 ? s.MaxLineSpan : i % 7 == 1 ? 2 : 1) : null,
                    contentType: i => i % 2);
            };
            if (_horizontal) LazyHorizontalGrid(_cells, content, state: State, modifier: modifier, reverseLayout: _initialReverse,
                contentPadding: _padded ? new PaddingValues(12.Dp()) : default,
                horizontalArrangement: _mainArrangement as Arrangement.Horizontal ?? (_spaced ? Arrangement.SpacedBy(10.Dp()) : _center ? Arrangement.Center : null),
                verticalArrangement: _crossArrangement as Arrangement.Vertical ?? (_spaced ? Arrangement.SpacedBy(6.Dp()) : null),
                userScrollEnabled: _enabled.Value, overscrollEffect: (OverscrollEffect?)null);
            else LazyVerticalGrid(_cells, content, state: State, modifier: modifier, reverseLayout: _initialReverse,
                contentPadding: _padded ? new PaddingValues(12.Dp()) : default,
                verticalArrangement: _mainArrangement as Arrangement.Vertical ?? (_spaced ? Arrangement.SpacedBy(10.Dp()) : _center ? Arrangement.Center : null),
                horizontalArrangement: _crossArrangement as Arrangement.Horizontal ?? (_spaced ? Arrangement.SpacedBy(6.Dp()) : null),
                userScrollEnabled: _enabled.Value, overscrollEffect: (OverscrollEffect?)null);
        });
    }
    [Composable] private void Item(int id, int size)
    {
        Assert.That(ItemLocal.Current, Is.EqualTo("inherited"));
        var token = Remember(() => new object()); _remembered[id] = token;
        DisposableEffect(id, () => { _active++; _composed++; return OnDispose(() => _active--); });
        var length = _variable ? size + Math.Abs(id) % 3 * 20 : size;
        var modifier = _horizontal ? Modifier.Width(length.Dp()).FillMaxHeight() : Modifier.Height(length.Dp()).FillMaxWidth();
        Box(content: () => Text("Cell " + id, color: Color.White), modifier: modifier.Background(new Color(.1f, .3f, .5f))
            .TestTag("item-" + id).Clickable(() => _clicks++));
    }
    private VisualElement E(string name) => rootVisualElement.Q(name) ?? throw new AssertionException("Missing " + name);
    private static void Near(float actual, float expected) => Assert.That(actual, Is.EqualTo(expected).Within(1f));
    private void Flush(int milliseconds = 0)
    {
        SynchronizationContext.SetSynchronizationContext(_context); _context.Drain(); _adapter.Clock.Advance(milliseconds);
        for (var i = 0; i < 6; i++) { _context.Drain(); _adapter.Invalidator.Tick(); simulate.FrameUpdateMs(16); }
    }
    private IEnumerator Settle(int frames = 8)
    { for (var i = 0; i < frames; i++) { Flush(); yield return null; } Flush(); }

    [UnityTest] public IEnumerator NativeLayoutHasThreeTracksAndBoundedCompositions()
    {
        yield return Mount(); TestContext.WriteLine($"grid H={_horizontal} R={_initialReverse} first={State.FirstVisibleItemIndex} off={State.FirstVisibleItemScrollOffset} physical={Physical} max={State.MaxValue} nativeHigh={(_horizontal ? Native.horizontalScroller.highValue : Native.verticalScroller.highValue)} visible=" + string.Join(";", State.LayoutInfo.VisibleItemsInfo.Select(i => $"{i.Index}:{i.Offset}:{i.Size}"))); Assert.That(Native, Is.Not.Null);
        Assert.That(E("viewport").Q<ListView>() != null, Is.EqualTo(!_horizontal));
        Near(Cross(E("item-0").worldBound.size), 100);
        Near(Cross(E("item-1").worldBound.position - E("item-0").worldBound.position), 100);
        Near(Math.Abs(Axis(E("item-3").worldBound.position - E("item-0").worldBound.position)), 60);
        Assert.That(State.LayoutInfo.MaxSpan, Is.EqualTo(3)); Assert.That(State.LayoutInfo.TotalItemsCount, Is.EqualTo(10000));
        Assert.That(_active, Is.InRange(9, 90)); Assert.That(_composed, Is.LessThan(100));
        Assert.That(State.FirstVisibleItemIndex, Is.Zero);
    }
    [UnityTest] public IEnumerator DistantRequestUsesItemIndexAndLineOffset()
    {
        yield return Mount(); State.ScrollToItem(4321, 7); yield return Settle(25);
        Assert.That(State.FirstVisibleItemIndex, Is.EqualTo(4320)); Near(State.FirstVisibleItemScrollOffset, 7);
        Assert.That(State.LayoutInfo.VisibleItemsInfo.Any(i => i.Index == 4321), Is.True);
        Assert.That(_active, Is.LessThan(100));
    }
    [UnityTest] public IEnumerator AdaptiveReflowsOnResize()
    {
        _cells = GridCells.Adaptive(90.Dp()); yield return Mount(); Assert.That(State.LayoutInfo.MaxSpan, Is.EqualTo(3));
        _cross.Value = 200; yield return Settle(20); Assert.That(State.LayoutInfo.MaxSpan, Is.EqualTo(2)); Near(Cross(E("item-0").layout.size), 100);
    }
    [UnityTest] public IEnumerator FullAndPartialSpansUseTrackGeometry()
    {
        _spans = true; yield return Mount(); Near(Cross(E("item-0").layout.size), 300); Near(Cross(E("item-1").layout.size), 200);
        Near(Cross(E("item-2").layout.size), 100);
        var info = State.LayoutInfo.VisibleItemsInfo.First(i => i.Index == 1); Assert.That(info.Span, Is.EqualTo(2));
        Assert.That(_horizontal ? info.Column : info.Row, Is.EqualTo(1));
    }
    [UnityTest] public IEnumerator PaddingAndSpacingApplyOncePerLine()
    {
        _padded = _spaced = true; yield return Mount(); Near(State.LayoutInfo.BeforeContentPadding, 12);
        Near(Cross(E("item-0").layout.size), 88); Near(Cross(E("item-1").worldBound.position - E("item-0").worldBound.position), 94);
        Near(Math.Abs(Axis(E("item-3").worldBound.position - E("item-0").worldBound.position)), 70);
    }
    [UnityTest] public IEnumerator VariableLineExtentIsMaximumCellSize()
    {
        _variable = true; yield return Mount(); Near(Axis(E("item-0").layout.size),60); Near(Axis(E("item-2").layout.size),100);
        Near(Math.Abs(Axis(E("item-5").worldBound.position - E("item-2").worldBound.position)),100);
    }
    [UnityTest] public IEnumerator KeyedAnchorSurvivesPrepend()
    {
        yield return Mount(); State.ScrollToItem(99, 11); yield return Settle(20);
        TestContext.WriteLine($"before anchor first={State.FirstVisibleItemIndex} off={State.FirstVisibleItemScrollOffset} pending={((ILazyGridStateAdapter)State).PendingScroll?.Index}");
        var token = _remembered[99]; _prefix.Value = 3; _count.Value += 3; yield return Settle(20);
        TestContext.WriteLine($"after anchor first={State.FirstVisibleItemIndex} off={State.FirstVisibleItemScrollOffset} pending={((ILazyGridStateAdapter)State).PendingScroll?.Index}");
        Assert.That(State.FirstVisibleItemIndex, Is.EqualTo(102)); Near(State.FirstVisibleItemScrollOffset, 11);
        Assert.That(_remembered[99], Is.SameAs(token));
    }
    [UnityTest] public IEnumerator EmptyAndShrinkingDataClampPosition()
    {
        yield return Mount(); State.ScrollToItem(9000); yield return Settle(20); _count.Value = 2; yield return Settle(20);
        Assert.That(State.FirstVisibleItemIndex, Is.Zero); Near(State.MaxValue,0);
        _count.Value = 0; yield return Settle(20); Assert.That(_active, Is.Zero); Assert.That(State.LayoutInfo.VisibleItemsInfo, Is.Empty);
        _count.Value=90; yield return Settle(25); Assert.That(State.LayoutInfo.VisibleItemsInfo.Any(i=>i.Index==0),Is.True);
    }
    [UnityTest] public IEnumerator ReplacementStateAndRemovalReleaseItems()
    {
        yield return Mount(); _state.Value = new LazyGridState(60, 5); yield return Settle(20);
        Assert.That(State.FirstVisibleItemIndex, Is.EqualTo(60)); Near(State.FirstVisibleItemScrollOffset, 5);
        _present.Value = false; yield return Settle(); Assert.That(_active, Is.Zero);
    }
    [UnityTest] public IEnumerator FixedSizeLeavesFreeSpaceForCrossArrangement()
    {
        _cells = GridCells.FixedSize(80.Dp()); _crossArrangement = Arrangement.Center;
        yield return Mount(); Near(Cross(E("item-0").layout.size),80);
        Near(Cross(E("item-0").worldBound.position - Native.contentViewport.worldBound.position),30);
        _cross.Value = 60; yield return Settle(20); Near(Cross(E("item-0").layout.size),60);
    }
    [UnityTest] public IEnumerator DistributedTracksAndShortLinesRespectArrangements()
    {
        _cells = GridCells.FixedSize(80.Dp()); _crossArrangement = Arrangement.SpaceBetween;
        _count.Value = 6; _mainArrangement = Arrangement.SpaceEvenly; yield return Mount();
        Near(Cross(E("item-1").worldBound.position - E("item-0").worldBound.position),110);
        Near(Math.Abs(Axis(E("item-3").worldBound.position - E("item-0").worldBound.position)),100);
    }
    [UnityTest] public IEnumerator ExplicitCenterArrangementIsIndependentOfReverse()
    {
        _count.Value = 2; _center = true; yield return Mount(); Near(State.MaxValue,0);
        Near(Axis(E("item-0").worldBound.position - Native.contentViewport.worldBound.position),90);
    }
    [UnityTest] public IEnumerator WheelUsesNativeInputAndDisabledInputKeepsChildrenClickable()
    {
        yield return Mount(); Wheel(_initialReverse ? -5 : 5); yield return Settle(14);
        TestContext.WriteLine($"wheel first={State.FirstVisibleItemIndex} physical={Physical} value={State.Value} max={State.MaxValue}"); Assert.That(State.FirstVisibleItemIndex, Is.GreaterThan(0));
        _enabled.Value = false; yield return Settle(); var offset = State.Value;
        Wheel(_initialReverse ? -5 : 5); yield return Settle(); Near(State.Value,offset);
        State.ScrollToItem(60,3); yield return Settle(20); Assert.That(State.FirstVisibleItemIndex,Is.EqualTo(60));
        var cell = E("item-63"); simulate.MouseDown(cell); yield return null; simulate.MouseUp(cell); yield return Settle(12);
        Assert.That(_clicks,Is.EqualTo(1));
    }
    [UnityTest] public IEnumerator LayoutInfoMatchesRenderedCells()
    {
        _padded = true; yield return Mount(); State.ScrollToItem(60,17); yield return Settle(20);
        foreach (var info in State.LayoutInfo.VisibleItemsInfo)
        {
            var e = E("item-"+info.Index); var pos = e.worldBound.position-Native.contentViewport.worldBound.position;
            Near(info.Size.Width,e.layout.width); Near(info.Size.Height,e.layout.height);
            Near(info.Offset.X,pos.x-(_horizontal ? 12 : 0)); Near(info.Offset.Y,pos.y-(_horizontal ? 0 : 12));
            Assert.That(info.Span,Is.EqualTo(1));
            Assert.That(_horizontal ? info.Row : info.Column,Is.EqualTo(info.Index%3));
        }
        Near(State.LayoutInfo.ViewportStartOffset,-12); Near(State.LayoutInfo.ViewportEndOffset,228);
    }
    [UnityTest] public IEnumerator ExplicitRequestOverridesKeyAnchor()
    {
        yield return Mount(); State.ScrollToItem(99); yield return Settle(18);
        _prefix.Value=3; _count.Value+=3; State.RequestScrollToItem(30,9); yield return Settle(25);
        Assert.That(State.FirstVisibleItemIndex,Is.EqualTo(30)); Near(State.FirstVisibleItemScrollOffset,9);
    }
    [UnityTest] public IEnumerator MillionItemsDoNotCreateMillionCompositions()
    {
        _count.Value=1000000; yield return Mount(); Assert.That(_active,Is.LessThan(100)); Assert.That(_composed,Is.LessThan(1000));
        State.ScrollToItem(900000); yield return Settle(25); Assert.That(_active,Is.LessThan(100));
        TestContext.WriteLine($"million first={State.FirstVisibleItemIndex} off={State.FirstVisibleItemScrollOffset} physical={Physical} max={State.MaxValue} active={_active} visible="+string.Join(",",State.LayoutInfo.VisibleItemsInfo.Select(i=>$"{i.Index}:{i.Offset}")));
        Assert.That(State.LayoutInfo.VisibleItemsInfo.Any(i=>i.Index==900000),Is.True);
    }
    [UnityTest] public IEnumerator StickyHeaderPinsAndNextHeaderPushesIt()
    {
        _sticky=true; yield return Mount(); State.ScrollToItem(7,10); yield return Settle(25);
        Near(Axis(E("item-0").worldBound.position-Native.contentViewport.worldBound.position),_initialReverse ? 180 : 0);
        State.ScrollToItem(20,10); yield return Settle(25);
        Near(Axis(E("item-13").worldBound.position-Native.contentViewport.worldBound.position),_initialReverse ? 180 : 0);
        Assert.That(_active,Is.LessThan(60));
    }
    [UnityTest] public IEnumerator AnimationMovesThroughIntermediatePositionAndCanBeReplaced()
    {
        yield return Mount(); var task=State.AnimateScrollToItem(30,4,Tween(200)); yield return Settle(2);
        Flush(60); yield return Settle(2); Assert.That(State.IsScrollInProgress,Is.True); Assert.That(State.Value,Is.GreaterThan(0));
        State.ScrollToItem(60,7); yield return Settle(25); Assert.That(task.IsCanceled,Is.True);
        Assert.That(State.FirstVisibleItemIndex,Is.EqualTo(60)); Near(State.FirstVisibleItemScrollOffset,7);
    }
    [UnityTest] public IEnumerator AnimationCompletesAtMeasuredDistantTarget()
    {
        yield return Mount(); var task=State.AnimateScrollToItem(4321,6,Tween(100)); yield return Settle(2);
        for(var i=0;i<8;i++) { Flush(40); yield return Settle(2); }
        yield return Settle(25); Assert.That(task.IsCompletedSuccessfully,Is.True);
        Assert.That(State.FirstVisibleItemIndex,Is.EqualTo(4320)); Near(State.FirstVisibleItemScrollOffset,6);
    }
    [UnityTest] public IEnumerator CellSizeChangesRemeasureNativeLines()
    {
        yield return Mount(); _size.Value=82; yield return Settle(20); TestContext.WriteLine($"cellSize {_size.Value} item0={Axis(E("item-0").layout.size)} native0={Axis(E("item-0").parent.layout.size)} first={State.FirstVisibleItemIndex} value={State.Value} max={State.MaxValue} phys={Physical}");
        Near(Math.Abs(Axis(E("item-3").worldBound.position-E("item-0").worldBound.position)),82);
    }
    private void Wheel(float direction)
    {
        var bounds=Native.contentViewport.worldBound;
        for(var ancestor=Native.parent;ancestor!=null;ancestor=ancestor.parent)
            if(ancestor is ScrollView outer)
            { var clip=outer.contentViewport.worldBound; bounds=UnityEngine.Rect.MinMaxRect(Math.Max(bounds.xMin,clip.xMin),Math.Max(bounds.yMin,clip.yMin),Math.Min(bounds.xMax,clip.xMax),Math.Min(bounds.yMax,clip.yMax)); }
        Assert.That(bounds.width>0 && bounds.height>0,Is.True,"A visible portion of the grid must receive the wheel");
        simulate.ScrollWheel(_horizontal ? new Vector2(direction,0) : new Vector2(0,direction),bounds.center); Flush();
    }
    [UnityTest] public IEnumerator DemoRendersAllTwentyFourCasesAndControls()
    {
        if (_horizontal || _initialReverse) Assert.Ignore("The gallery is independent of the parameterized grid fixture.");
        _gallery = new GridDemoState(); panelSize = new Vector2(1200,3600); yield return Mount(); yield return Settle(20);
        Assert.That(_gallery.Cases.Count,Is.EqualTo(24));
        foreach (var item in _gallery.Cases)
        {
            var prefix="grid."+item.Id;
            Assert.That(E(prefix+".viewport").Q<ScrollView>(),Is.Not.Null,item.Id);
            foreach (var control in new[]{"start","jump","end","animate","resize","input","reverse","size","prepend","count","state","remove"})
                Assert.That(E(prefix+"."+control),Is.Not.Null);
            Assert.That(item.State.Value.LayoutInfo.TotalItemsCount,Is.EqualTo(item.Sticky ? 78 : item.Count.Value),item.Id);
            var originalWidth=item.Width.Value;
            var button=E(prefix+".resize"); simulate.MouseDown(button); yield return null; simulate.MouseUp(button); yield return Settle(4);
            Assert.That(item.Width.Value,Is.Not.EqualTo(originalWidth),item.Id+" resize click");
        }
    }
    [UnityTest] public IEnumerator NativeTouchCaptureIsReleasedWhenInputIsDisabled()
    {
        yield return Mount(); var origin=Native.contentViewport.worldBound.center;
        var delta=(_horizontal ? new Vector2(50,0) : new Vector2(0,50))*(_initialReverse ? -1 : 1);
        Touch(TouchPhase.Began,origin); Touch(TouchPhase.Moved,origin-delta); yield return Settle(4);
        TestContext.WriteLine($"touch value={State.Value} phys={Physical} max={State.MaxValue} captured={Native.contentContainer.HasPointerCapture(UnityEngine.UIElements.PointerId.touchPointerIdBase)}");
        Assert.That(State.Value,Is.GreaterThan(0));
        Assert.That(Native.contentContainer.HasPointerCapture(UnityEngine.UIElements.PointerId.touchPointerIdBase),Is.True);
        _enabled.Value=false; yield return Settle();
        Assert.That(Native.contentContainer.HasPointerCapture(UnityEngine.UIElements.PointerId.touchPointerIdBase),Is.False);
        Assert.That(State.IsScrollInProgress,Is.False);
        var value=State.Value; Touch(TouchPhase.Ended,origin-delta); yield return Settle(); Near(State.Value,value);
    }
    [UnityTest] public IEnumerator RemovingGridCancelsAnimationAndUnsubscribesState()
    {
        yield return Mount(); var view=Native; var task=State.AnimateScrollToItem(90,animationSpec:Tween(1000)); yield return Settle(2); Flush(100);
        _present.Value=false; yield return Settle(); Assert.That(task.IsCanceled,Is.True); Assert.That(State.IsScrollInProgress,Is.False);
        var offset=view.scrollOffset; State.ScrollTo(100); Flush(100); Assert.That(view.scrollOffset,Is.EqualTo(offset));
    }
    [UnityTest] public IEnumerator NestedWheelForwardsAtBoundaryAndWhenDisabled()
    {
        _nested=true; _count.Value=300; yield return Mount(); State.ScrollToItem(_initialReverse ? 0 : 299); yield return Settle(25);
        Wheel(8); yield return Settle(15); TestContext.WriteLine($"nested edge outer={_outer!.Value}/{_outer.MaxValue} inner={State.Value}/{State.MaxValue} phys={Physical} grid={Native.contentViewport.worldBound}"); Assert.That(_outer!.Value,Is.GreaterThan(0));
        _outer.ScrollTo(0); State.ScrollToItem(60); yield return Settle(20); _enabled.Value=false; yield return Settle();
        var before=State.Value; Wheel(5); yield return Settle(15); Near(State.Value,before); TestContext.WriteLine($"nested disabled outer={_outer.Value}/{_outer.MaxValue}"); Assert.That(_outer.Value,Is.GreaterThan(0));
    }
    [UnityTest] public IEnumerator LargeGridCrossesNativeSegmentBoundaries()
    {
        _count.Value=1000000; yield return Mount();
        foreach(var index in new[]{98280,98310,98340,999990,0})
        {
            State.ScrollToItem(index,3); yield return Settle(30);
            TestContext.WriteLine($"segment {index} first={State.FirstVisibleItemIndex} off={State.FirstVisibleItemScrollOffset} value={State.Value} max={State.MaxValue} native={Physical} high={(_horizontal ? Native.horizontalScroller.highValue : Native.verticalScroller.highValue)} visible="+string.Join(",",State.LayoutInfo.VisibleItemsInfo.Select(i=>i.Index)));
            Assert.That(State.LayoutInfo.VisibleItemsInfo.Any(i=>i.Index==index),Is.True,"item "+index);
            Assert.That(_active,Is.LessThan(100));
        }
    }
    [UnityTest] public IEnumerator NativeKeyboardNavigationRespectsInputGate()
    {
        yield return Mount();
        void Navigate()
        {
            Native.Focus();
            using var evt=NavigationMoveEvent.GetPooled((_horizontal ? Vector2.right : Vector2.down)*(_initialReverse ? -1 : 1),EventModifiers.None);
            evt.target=Native; Native.SendEvent(evt); Flush();
        }
        Navigate(); yield return Settle(15); Assert.That(State.Value,Is.GreaterThan(0));
        _enabled.Value=false; yield return Settle(); var value=State.Value; Navigate(); yield return Settle(); Near(State.Value,value);
    }
    [UnityTest] public IEnumerator DisposalBeforeNextNativeLayoutCancelsPendingRequest()
    {
        yield return Mount(); var task=State.AnimateScrollToItem(300);
        _composer.Dispose(); Assert.That(task.IsCanceled,Is.True);
        Assert.That(State.IsScrollInProgress,Is.False);
    }
    private void Touch(TouchPhase phase, Vector2 position)
    {
        var touch=new Touch { fingerId=0,position=position,phase=phase,pressure=1 };
        EventBase evt=phase==TouchPhase.Began ? PointerDownEvent.GetPooled(touch,EventModifiers.None)
            : phase==TouchPhase.Moved ? PointerMoveEvent.GetPooled(touch,EventModifiers.None)
            : PointerUpEvent.GetPooled(touch,EventModifiers.None);
        using(evt)
        {
            var target=Native.contentContainer.HasPointerCapture(UnityEngine.UIElements.PointerId.touchPointerIdBase)
                ? Native.contentContainer : rootVisualElement.panel.Pick(position);
            evt.target=target; target.SendEvent(evt);
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
