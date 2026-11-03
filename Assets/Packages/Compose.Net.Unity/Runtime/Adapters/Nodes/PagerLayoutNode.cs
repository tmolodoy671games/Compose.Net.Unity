using System;
using System.Threading.Tasks;
using Compose.Net;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Runtime.Adapters.Nodes;

internal sealed class PagerLayoutNode : UnityReusableComposeNode, IPagerLayoutNode, IReusableComposeNode
{
    internal PagerLayoutNode(bool horizontal) : base(new PagerView(horizontal)) { }
    public void ConfigurePager(IMutablePagerState state, PagerSettings settings) => ((PagerView)VisualElement).Configure(state, settings);
    public new void Dispose() { ((PagerView)VisualElement).Dispose(); base.Dispose(); }
}

internal sealed class PagerItemNode : UnityReusableComposeNode, IPagerItemNode
{
    internal PagerItemNode() : base(new Box { name = "PagerItem" }) { }
    public void ConfigurePage(IMutablePagerState state, int index, PagerSettings settings)
    {
        var horizontal = settings.Orientation == PagerOrientation.Horizontal;
        var viewport = state.LayoutInfo.ViewportSize;
        var padding = settings.ContentPadding;
        var d = settings.Density;
        var start = state.BeforePadding + index * state.PageStride;
        if (settings.ReverseLayout) start = state.ContentSize - start - state.PageExtent;
        var style = VisualElement.style;
        style.position = Position.Absolute;
        style.flexShrink = 0;
        style.left = horizontal ? start : d.ToPx(padding.Left);
        style.top = horizontal ? d.ToPx(padding.Top) : start;
        style.width = horizontal ? state.PageExtent : Math.Max(0, viewport.Width - d.ToPx(padding.Left) - d.ToPx(padding.Right));
        style.height = horizontal ? Math.Max(0, viewport.Height - d.ToPx(padding.Top) - d.ToPx(padding.Bottom)) : state.PageExtent;
        ((Box)VisualElement).ContentAlignment = horizontal
            ? settings.CrossAxisAlignment == Alignment.Top ? Alignment.TopLeft : settings.CrossAxisAlignment == Alignment.Bottom ? Alignment.BottomLeft : Alignment.CenterLeft
            : settings.CrossAxisAlignment == Alignment.Left ? Alignment.TopLeft : settings.CrossAxisAlignment == Alignment.Right ? Alignment.TopRight : Alignment.TopCenter;
    }
}

/// <summary>UI Toolkit handles wheel, touch slop/capture, nested scrolling and clipping.
/// Only page selection and settling are added here; no parallel gesture recognizer.</summary>
internal sealed class PagerView : ScrollView, IDisposable
{
    private IMutablePagerState? _state;
    private PagerSettings? _settings;
    private readonly bool _horizontal;
    private bool _syncing, _touching, _disposed;
    private IVisualElementScheduledItem? _settle;
    private float _velocity, _lastValue;
    private double _lastTime;

    internal PagerView(bool horizontal) : base(horizontal ? ScrollViewMode.Horizontal : ScrollViewMode.Vertical)
    {
        _horizontal = horizontal;
        name = horizontal ? "HorizontalPager" : "VerticalPager";
        horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        verticalScrollerVisibility = ScrollerVisibility.Hidden;
        touchScrollBehavior = TouchScrollBehavior.Clamped;
        scrollDecelerationRate = 0; // Pager owns the final snap, ScrollView owns the drag.
        nestedInteractionKind = NestedInteractionKind.ForwardScrolling;
        style.overflow = Overflow.Hidden;
        style.minWidth = 0;
        style.minHeight = 0;
        contentViewport.style.minWidth = 0;
        contentViewport.style.minHeight = 0;
        contentContainer.style.flexShrink = 0;
        contentContainer.style.flexGrow = 0;
        contentViewport.RegisterCallback<GeometryChangedEvent>(_ => Measure());
        contentContainer.RegisterCallback<GeometryChangedEvent>(_ => SyncOffset());
        horizontalScroller.valueChanged += OnScroll;
        verticalScroller.valueChanged += OnScroll;
        RegisterCallback<WheelEvent>(OnWheel, TrickleDown.TrickleDown);
        RegisterCallback<PointerDownEvent>(OnDown, TrickleDown.TrickleDown);
        RegisterCallback<PointerMoveEvent>(OnMove, TrickleDown.TrickleDown);
        RegisterCallback<PointerUpEvent>(OnUp, TrickleDown.TrickleDown);
        RegisterCallback<PointerCancelEvent>(_ => Release(), TrickleDown.TrickleDown);
        RegisterCallback<PointerCaptureOutEvent>(_ => { if (_touching) Release(); });
        RegisterCallback<NavigationMoveEvent>(OnNavigate, TrickleDown.TrickleDown);
        RegisterCallback<DetachFromPanelEvent>(_ => { _settle?.Pause(); _touching = false; _state?.CancelScroll(); });
    }

    internal void Configure(IMutablePagerState state, PagerSettings settings)
    {
        if (_disposed) return;
        if (!ReferenceEquals(_state, state))
        {
            if (_state != null) _state.UnsubscribeToValueChange(StateChanged);
            _state = state;
            state.SubscribeToValueChange(StateChanged);
        }
        _settings = settings;
        pickingMode = UnityEngine.UIElements.PickingMode.Position;
        focusable = settings.UserScrollEnabled;
        if (!settings.UserScrollEnabled) { _settle?.Pause(); _touching = false; state.CancelScroll(); }
        Measure();
    }

    private void Measure()
    {
        if (_state == null || _settings == null || _disposed || _syncing) return;
        var size = contentViewport.contentRect.size;
        if (!float.IsFinite(size.x) || !float.IsFinite(size.y)) return;
        _syncing = true;
        try
        {
            _state.UpdateLayout(new Size(Math.Max(0, size.x), Math.Max(0, size.y)));
            contentContainer.style.width = _horizontal ? _state.ContentSize : size.x;
            contentContainer.style.height = _horizontal ? size.y : _state.ContentSize;
            horizontalPageSize = _state.PageStride;
            verticalPageSize = _state.PageStride;
        }
        finally { _syncing = false; }
        SyncOffset();
    }

    private void StateChanged(float value) => SyncOffset();
    private void SyncOffset()
    {
        if (_state == null || _settings == null || _syncing || _disposed) return;
        _syncing = true;
        try
        {
            var scroller = _horizontal ? horizontalScroller : verticalScroller;
            scroller.lowValue = 0;
            scroller.highValue = _state.MaxValue;
            var offset = _settings.ReverseLayout ? _state.MaxValue - _state.Value : _state.Value;
            scrollOffset = _horizontal ? new Vector2(offset, 0) : new Vector2(0, offset);
        }
        finally { _syncing = false; }
    }

    private void OnScroll(float ignored)
    {
        if (_syncing || _state == null || _settings == null || _disposed) return;
        var offset = _horizontal ? horizontalScroller.value : verticalScroller.value;
        var value = _settings.ReverseLayout ? _state.MaxValue - offset : offset;
        if (!_settings.UserScrollEnabled) { SyncOffset(); return; }
        var time = Time.realtimeSinceStartupAsDouble;
        if (time > _lastTime && time - _lastTime < .15) _velocity = (float)((value - _lastValue) / (time - _lastTime));
        _lastTime = time;
        _lastValue = value;
        _state.ScrollFromUser(value);
        if (!_touching) ScheduleSettle();
    }

    private void OnWheel(WheelEvent evt)
    {
        var ancestor = GetFirstAncestorOfType<ScrollView>();
        var nearest = evt.target as VisualElement;
        var inner = nearest as ScrollView ?? nearest?.GetFirstAncestorOfType<ScrollView>();
        // A descendant scroll view gets first refusal on its own gestures.
        if (inner != null && inner != this) return;
        var crossAxisIntent = _horizontal ? Math.Abs(evt.delta.y) > Math.Abs(evt.delta.x) : Math.Abs(evt.delta.x) > Math.Abs(evt.delta.y);
        var ancestorHandlesCrossAxis = ancestor != null && (_horizontal ? ancestor.mode != ScrollViewMode.Horizontal : ancestor.mode != ScrollViewMode.Vertical);
        if (_settings?.UserScrollEnabled != true || crossAxisIntent && ancestorHandlesCrossAxis)
        {
            evt.StopImmediatePropagation();
            if (parent != null)
            {
                // GetPooled(IMouseEvent) drops WheelEvent.delta; preserve the actual wheel data.
                var source = new Event { type = EventType.ScrollWheel, delta = evt.delta, mousePosition = evt.mousePosition, modifiers = evt.modifiers };
                using var forwarded = WheelEvent.GetPooled(source);
                forwarded.target = parent; parent.SendEvent(forwarded);
            }
            return;
        }
        _velocity = 0;
        // The native ScrollView consumes the event only if it can scroll, forwarding at the edge.
        ScheduleSettle();
    }
    private void OnDown(PointerDownEvent evt)
    {
        if (evt.pointerType == UnityEngine.UIElements.PointerType.mouse || !evt.isPrimary) return;
        // Let taps reach page content; blocking move prevents native drag without disabling children.
        if (_settings?.UserScrollEnabled != true) return;
        _settle?.Pause();
        _touching = true;
        _velocity = 0;
        _lastTime = 0;
        _state?.BeginUserScroll();
    }
    private void OnMove(PointerMoveEvent evt)
    {
        if (evt.pointerType != UnityEngine.UIElements.PointerType.mouse && _settings?.UserScrollEnabled != true) evt.StopImmediatePropagation();
    }
    private void OnUp(PointerUpEvent evt) { if (evt.pointerType != UnityEngine.UIElements.PointerType.mouse && evt.isPrimary) Release(); }
    private void Release()
    {
        _touching = false;
        ScheduleSettle(1); // Let ScrollView release capture before applying the snap.
    }
    private void ScheduleSettle(long delay = 120)
    {
        _settle?.Pause();
        _settle = schedule.Execute(() => Observe(_state?.EndUserScroll(_velocity) ?? Task.CompletedTask)).StartingIn(delay);
    }
    private void OnNavigate(NavigationMoveEvent evt)
    {
        var direction = _horizontal
            ? evt.direction == NavigationMoveEvent.Direction.Right ? 1 : evt.direction == NavigationMoveEvent.Direction.Left ? -1 : 0
            : evt.direction == NavigationMoveEvent.Direction.Down ? 1 : evt.direction == NavigationMoveEvent.Direction.Up ? -1 : 0;
        if (direction == 0 || _state == null) return;
        evt.StopImmediatePropagation();
        if (_settings?.UserScrollEnabled != true) return;
        if (_settings.ReverseLayout) direction = -direction;
        Observe(_state.AnimateScrollToPage(_state.CurrentPage + direction, animationSpec: _settings.FlingBehavior.SnapAnimationSpec));
    }
    private static async void Observe(Task task)
    {
        try { await task; }
        catch (OperationCanceledException) { }
        catch (Exception exception) { Debug.LogException(exception); }
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _settle?.Pause();
        if (_state != null) { _state.UnsubscribeToValueChange(StateChanged); _state.CancelScroll(); }
        _state = null;
        horizontalScroller.valueChanged -= OnScroll;
        verticalScroller.valueChanged -= OnScroll;
    }
}
