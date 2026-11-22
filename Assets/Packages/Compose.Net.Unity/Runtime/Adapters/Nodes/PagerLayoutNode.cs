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

/// <summary>UI Toolkit handles touch slop/capture, nested scrolling and clipping.
/// Only page selection and settling are added here; no parallel gesture recognizer.</summary>
internal sealed class PagerView : ScrollView, IDisposable
{
    private IMutablePagerState? _state;
    private PagerSettings? _settings;
    private readonly bool _horizontal;
    private bool _syncing, _disposed;
    private int? _touchPointer;
    private readonly SmoothWheelScroll _wheel;
    private bool _scrollingWithWheel;
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
        horizontalPageSize = 0;
        verticalPageSize = 0;
        // Handle the two wheel axes explicitly. ScrollView otherwise maps vertical input to X.
        mouseWheelScrollSize = 0;
        focusable = false;
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
        _wheel = new SmoothWheelScroll(this, () => _settings?.UserScrollEnabled == true, BeginWheel, useDefaultWheelInput: false);
        contentViewport.RegisterCallback<WheelEvent>(OnWheel);
        RegisterCallback<WheelEvent>(evt => { if (evt.target == this) OnWheel(evt); }, TrickleDown.TrickleDown);
        RegisterCallback<KeyDownEvent>(OnKeyDown);
        RegisterCallback<PointerDownEvent>(OnDown, TrickleDown.TrickleDown);
        RegisterCallback<PointerMoveEvent>(OnMove, TrickleDown.TrickleDown);
        RegisterCallback<PointerUpEvent>(evt => Release(evt.pointerId), TrickleDown.TrickleDown);
        RegisterCallback<PointerCancelEvent>(evt => Release(evt.pointerId), TrickleDown.TrickleDown);
        RegisterCallback<PointerCaptureOutEvent>(evt => Release(evt.pointerId));
        RegisterCallback<DetachFromPanelEvent>(evt =>
        {
            if (evt.target != this) return;
            _settle?.Pause();
            _wheel.Cancel();
            _scrollingWithWheel = false;
            _touchPointer = null;
            _state?.CancelScroll();
        });
    }

    internal void Configure(IMutablePagerState state, PagerSettings settings)
    {
        if (_disposed) return;
        if (!ReferenceEquals(_state, state))
        {
            if (_state != null)
            {
                _state.UnsubscribeToValueChange(StateChanged);
                _state.CancelScroll();
                _settle?.Pause();
                _wheel.Cancel();
                _scrollingWithWheel = false;
                _touchPointer = null;
            }
            _state = state;
            state.SubscribeToValueChange(StateChanged);
        }
        if (_settings?.UserScrollEnabled == true && !settings.UserScrollEnabled)
        {
            _settle?.Pause();
            _wheel.Cancel();
            _scrollingWithWheel = false;
            _touchPointer = null;
            state.CancelScroll();
        }
        _settings = settings;
        pickingMode = UnityEngine.UIElements.PickingMode.Position;
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
        }
        finally { _syncing = false; }
        SyncOffset();
    }

    private void StateChanged(float value)
    {
        if (_scrollingWithWheel && (_state?.IsScrollInProgress != true || !Mathf.Approximately(value, _lastValue)))
        {
            _wheel.Cancel();
            _settle?.Pause();
            _scrollingWithWheel = false;
        }
        SyncOffset();
    }
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
        // UI Toolkit can deliver a scroller change after SyncOffset has returned.
        if (Mathf.Approximately(value, _state.Value)) return;
        var time = Time.realtimeSinceStartupAsDouble;
        if (_scrollingWithWheel) _velocity = _settings.ReverseLayout ? -_wheel.Velocity : _wheel.Velocity;
        else if (time > _lastTime && time - _lastTime < .15) _velocity = (float)((value - _lastValue) / (time - _lastTime));
        _lastTime = time;
        _lastValue = value;
        _state.ScrollFromUser(value);
        if (_touchPointer == null) ScheduleSettle();
    }

    private void OnKeyDown(KeyDownEvent evt)
    {
        if (evt.ctrlKey || evt.keyCode is not (KeyCode.PageUp or KeyCode.PageDown) ||
            _settings?.UserScrollEnabled != true || _state == null) return;

        var size = _state.LayoutInfo.ViewportSize;
        var delta = _horizontal ? size.Width : size.Height;
        _settle?.Pause();
        _wheel.Cancel();
        _scrollingWithWheel = false;
        if ((evt.keyCode == KeyCode.PageUp) != _settings.ReverseLayout) delta = -delta;
        _state.ScrollFromUser(Mathf.Clamp(_state.Value + delta, 0, _state.MaxValue));
        // A page key scrolls by a viewport, even if that leaves a fractional page offset.
        _state.CancelScroll();
        evt.StopPropagation();
    }

    private void OnWheel(WheelEvent evt)
    {
        if (evt.isPropagationStopped || _settings?.UserScrollEnabled != true || _state == null) return;
        var ticks = (_horizontal ? evt.delta.x : evt.delta.y) / WheelEvent.scrollDeltaPerTick;
        if (ticks == 0) return;

        // Android's fallback is 64 dp per detent. Unity reports deltas in units per tick.
        var delta = ticks * _settings.Density.ToPx(64.Dp());
        var remaining = _wheel.AddDelta(delta);
        if (Mathf.Approximately(remaining, delta)) return;
        evt.StopPropagation();
    }

    private void BeginWheel()
    {
        _settle?.Pause();
        _scrollingWithWheel = true;
        _lastValue = _state!.Value;
        _lastTime = 0;
        _velocity = 0;
        _state?.BeginUserScroll();
    }

    private void OnDown(PointerDownEvent evt)
    {
        if (evt.pointerType == UnityEngine.UIElements.PointerType.mouse || !evt.isPrimary) return;
        // Let taps reach page content; blocking move prevents native drag without disabling children.
        if (_settings?.UserScrollEnabled != true) return;
        _settle?.Pause();
        _scrollingWithWheel = false;
        _touchPointer = evt.pointerId;
        _velocity = 0;
        _lastTime = 0;
        if (_state?.IsScrollInProgress == true) _state.BeginUserScroll();
    }
    private void OnMove(PointerMoveEvent evt)
    {
        if (evt.pointerType != UnityEngine.UIElements.PointerType.mouse && _settings?.UserScrollEnabled != true) evt.StopImmediatePropagation();
    }
    private void Release(int pointerId)
    {
        if (_touchPointer != pointerId) return;
        _touchPointer = null;
        if (_state?.IsScrollInProgress == true) ScheduleSettle(1);
    }
    private void ScheduleSettle(long delay = 120)
    {
        _settle?.Pause();
        _settle = schedule.Execute(Settle).StartingIn(delay);
    }

    private void Settle()
    {
        if (_state?.IsScrollInProgress != true || _settings == null) return;
        if (!_scrollingWithWheel)
        {
            Observe(_state.EndUserScroll(_velocity));
            return;
        }
        // A wheel burst can travel through many pages before the final snap begins.
        // Settle from the last visible position, including when the direction changed.
        _scrollingWithWheel = false;
        var fraction = _state.CurrentPageOffsetFraction;
        var target = _state.CurrentPage;
        var policy = _settings.FlingBehavior;
        if (Mathf.Abs(fraction) > .001f && _velocity != 0 && policy.PagerSnapDistance.MaxPages > 0)
        {
            var page = target + fraction;
            var from = _velocity > 0 ? Mathf.FloorToInt(page) : Mathf.CeilToInt(page);
            var advance = Mathf.Abs(_velocity) > 400 || Mathf.Abs(page - from) >= policy.SnapPositionalThreshold;
            target = advance ? from + Math.Sign(_velocity) : from;
        }
        Observe(_state.AnimateScrollToPage(target, animationSpec: policy.SnapAnimationSpec));
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
        _wheel.Cancel();
        _settle?.Pause();
        if (_state != null) { _state.UnsubscribeToValueChange(StateChanged); _state.CancelScroll(); }
        _state = null;
        horizontalScroller.valueChanged -= OnScroll;
        verticalScroller.valueChanged -= OnScroll;
    }
}
