using System;
using Compose.Net;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

// ScrollView owns measurement, clipping, wheel/touch input, inertia and nested scrolling.
// This node only bridges Compose state and preserves the content's Row/Column/Box layout.
internal record ScrollModifierImpl(IScrollState State, bool Enabled, FlingBehavior? FlingBehavior,
    bool ReverseScrolling, OverscrollEffect? OverscrollEffect, bool Horizontal)
    : ContentContainerUnityModifier<ScrollModifierImpl.ScrollNode>
{
    public override ScrollNode Create() => new(this);
    public override void Update(ScrollNode node) => node.Update(this);

    internal sealed class ScrollNode(ScrollModifierImpl options) : ContentContainerNodeBase
    {
        private ScrollModifierImpl _options = options;
        private IScrollStateAdapter Bridge => _options.State as IScrollStateAdapter ??
            throw new ArgumentException("Scroll state must implement IScrollStateAdapter.");
        private ScrollView? _view;
        private bool _syncing, _measured, _nativeStopped;
        private IVisualElementScheduledItem? _measure, _idle;
        private IDragInteraction.Start? _drag;
        private int _pointer = -1;
        private StyleEnum<FlexDirection> _direction;
        private StyleEnum<Align> _alignItems, _alignSelf;
        private StyleEnum<Justify> _justify;
        private StyleFloat _shrink, _grow;
        private Scroller Scroller => _options.Horizontal ? _view!.horizontalScroller : _view!.verticalScroller;
        private float Axis(Vector2 size) => _options.Horizontal ? size.x : size.y;
        private float Physical(float logical) => _options.ReverseScrolling ? _options.State.MaxValue - logical : logical;

        internal void Update(ScrollModifierImpl next)
        {
            var previousBridge = Bridge;
            var newState = !ReferenceEquals(_options.State, next.State);
            if (newState)
            {
                StopNativeScroll();
                previousBridge.SetNativeScrollInProgress(false);
                previousBridge.ValueChanged -= OnStateValue;
                previousBridge.ScrollRequested -= StopNativeScroll;
            }
            _options = next;
            if (_view == null) return;
            if (newState) { Bridge.ValueChanged += OnStateValue; Bridge.ScrollRequested += StopNativeScroll; }
            if (!next.Enabled) StopNativeScroll();
            Configure();
            SyncLayout();
        }

        protected override void ApplyToContainer(UnityReusableComposeNode node, VisualElement element, VisualElement content)
        {
            _ = Bridge;
            _direction = content.style.flexDirection; _alignItems = content.style.alignItems;
            _alignSelf = content.style.alignSelf; _justify = content.style.justifyContent;
            _shrink = content.style.flexShrink; _grow = content.style.flexGrow;
            // A scroll axis does not change how the content lays out its children.
            content.style.flexDirection = element is Row ? FlexDirection.Row : element.style.flexDirection;
            content.style.alignItems = element.style.alignItems;
            content.style.justifyContent = element.style.justifyContent;
            content.style.flexShrink = 0;
            content.style.flexGrow = 1;
            content.style.alignSelf = Align.Stretch;
            _view = new ScrollView(_options.Horizontal ? ScrollViewMode.Horizontal : ScrollViewMode.Vertical)
            {
                name = "compose-scroll-view",
                horizontalScrollerVisibility = ScrollerVisibility.Hidden,
                verticalScrollerVisibility = ScrollerVisibility.Hidden,
                nestedInteractionKind = ScrollView.NestedInteractionKind.ForwardScrolling,
            };
            _view.style.flexGrow = 1;
            _view.style.flexShrink = 1;
            _view.style.minWidth = 0;
            _view.style.minHeight = 0;
            _view.style.alignSelf = Align.Stretch;
            (element as ILayout)?.Untrack(content);
            var index = element.IndexOf(content);
            content.RemoveFromHierarchy();
            element.Insert(Math.Max(0, index), _view);
            // The viewport follows flex constraints, not Box's stacking/align rules.
            // Only the original content layout inside it participates in Box stacking.
            _view.Add(content);
            Configure();
            Scroller.valueChanged += OnNativeValue;
            _view.contentViewport.RegisterCallback<GeometryChangedEvent>(OnGeometry);
            _view.contentContainer.RegisterCallback<GeometryChangedEvent>(OnGeometry);
            content.RegisterCallback<GeometryChangedEvent>(OnGeometry);
            // Observe real native capture; a tap is not a drag interaction.
            _view.contentContainer.RegisterCallback<PointerCaptureEvent>(OnCapture);
            _view.contentContainer.RegisterCallback<PointerUpEvent>(OnUp, TrickleDown.TrickleDown);
            _view.contentContainer.RegisterCallback<PointerCancelEvent>(OnCancel, TrickleDown.TrickleDown);
            _view.contentContainer.RegisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
            _view.RegisterCallback<DetachFromPanelEvent>(OnPanelDetach);
            // Gate only scroll input, leaving child controls enabled. A child scroll view
            // consumes its event before this bubble callback, preserving nested scrolling.
            content.RegisterCallback<WheelEvent>(OnWheel);
            content.RegisterCallback<PointerMoveEvent>(OnMove);
            content.RegisterCallback<NavigationMoveEvent>(OnNavigation);
            Bridge.ValueChanged += OnStateValue;
            Bridge.ScrollRequested += StopNativeScroll;
            _view.RegisterCallback<PointerDownEvent>(OnDown, TrickleDown.TrickleDown);
            _view.RegisterCallback<WheelEvent>(OnWheelStart, TrickleDown.TrickleDown);
            _view.RegisterCallback<NavigationMoveEvent>(OnSelfNavigation, TrickleDown.TrickleDown);
            QueueMeasure();
        }

        private void Configure()
        {
            _view!.scrollDecelerationRate = _options.Enabled && !_nativeStopped ? _options.FlingBehavior?.DecelerationRate ?? 0.135f : 0;
            _view.touchScrollBehavior = _options.OverscrollEffect == null || !_options.Enabled || _nativeStopped
                ? ScrollView.TouchScrollBehavior.Clamped : ScrollView.TouchScrollBehavior.Elastic;
            if (_options.OverscrollEffect != null) _view.elasticity = _options.OverscrollEffect.Elasticity;
            _view.focusable = _options.Enabled;
        }

        private void OnGeometry(GeometryChangedEvent evt) => QueueMeasure();
        private void QueueMeasure()
        {
            if (_view == null) return;
            _measure?.Pause();
            _measure = _view.schedule.Execute(SyncLayout);
        }

        private void SyncLayout()
        {
            if (_view?.panel == null || ContentContainer == null || Element == null) return;
            // Node factories apply layout after modifier attachment. Never clamp a pending
            // initial offset using geometry from the preceding flex direction.
            var direction = Element is Row ? (StyleEnum<FlexDirection>)FlexDirection.Row : Element.style.flexDirection;
            if (ContentContainer.style.flexDirection != direction ||
                ContentContainer.style.alignItems != Element.style.alignItems ||
                ContentContainer.style.justifyContent != Element.style.justifyContent)
            {
                ContentContainer.style.flexDirection = direction;
                ContentContainer.style.alignItems = Element.style.alignItems;
                ContentContainer.style.justifyContent = Element.style.justifyContent;
                QueueMeasure();
                return;
            }
            var viewport = Axis(_view.contentViewport.layout.size);
            var content = Axis(_view.contentContainer.layout.size);
            if (float.IsNaN(viewport) || float.IsNaN(content) || viewport <= 0) return;
            _syncing = true;
            try
            {
                Bridge.UpdateLayout(viewport, Math.Max(content, viewport + Math.Max(0, Scroller.highValue)));
                _measured = true;
                SetOffset(_options.State.Value);
            }
            finally { _syncing = false; }
        }

        private void SetOffset(float value)
        {
            var offset = Physical(value);
            _view!.scrollOffset = _options.Horizontal ? new Vector2(offset, 0) : new Vector2(0, offset);
        }

        private void OnStateValue(float value)
        {
            if (_view == null || !_measured || _syncing) return;
            _syncing = true;
            try { SetOffset(value); }
            finally { _syncing = false; }
        }

        private void OnNativeValue(float physical)
        {
            if (_view == null || !_measured || _syncing) return;
            // Native geometry changes update scrollbars before their GeometryChangedEvent.
            // Publish the new range first, preserving the logical offset in reverse mode.
            if (Math.Abs(Math.Max(0, Scroller.highValue) - _options.State.MaxValue) > 0.5f)
            { QueueMeasure(); return; }
            _syncing = true;
            try
            {
                Bridge.SetNativeValue(_options.ReverseScrolling ? _options.State.MaxValue - physical : physical);
                Bridge.SetNativeScrollInProgress(true);
                _idle?.Pause();
                _idle = _view.schedule.Execute(() =>
                {
                    if (_drag == null) Bridge.SetNativeScrollInProgress(false);
                }).StartingIn(80);
            }
            finally { _syncing = false; }
        }

        private void StopNativeScroll()
        {
            if (_view == null) return;
            // Public native settings stop inertia and spring-back on the next toolkit tick.
            // Restore configured physics when the next user gesture starts.
            _nativeStopped = true;
            _view.scrollDecelerationRate = 0;
            _view.touchScrollBehavior = ScrollView.TouchScrollBehavior.Clamped;
            if (_pointer >= 0) _view.contentContainer.ReleasePointer(_pointer);
            EndDrag(true);
            _idle?.Pause();
            Bridge.SetNativeScrollInProgress(false);
        }
        private void OnDown(PointerDownEvent evt) { if (_options.Enabled) { _nativeStopped = false; Configure(); } }
        private void OnWheelStart(WheelEvent evt)
        {
            if (_options.Enabled) { _nativeStopped = false; Configure(); }
            else if (evt.target == _view || evt.target == _view!.contentViewport || evt.target == _view.contentContainer)
                OnWheel(evt);
        }
        private void OnSelfNavigation(NavigationMoveEvent evt) { if (evt.target == _view) OnNavigation(evt); }

        private void OnWheel(WheelEvent evt)
        {
            if (_options.Enabled) return;
            evt.StopPropagation();
            // Continue at the containing layout, bypassing this disabled ScrollView only.
            var parent = Element?.parent;
            if (parent == null) return;
            using var forwarded = WheelEvent.GetPooled(evt);
            forwarded.target = parent;
            parent.SendEvent(forwarded);
        }
        private void OnMove(PointerMoveEvent evt)
        {
            if (!_options.Enabled && evt.pointerType != UnityEngine.UIElements.PointerType.mouse)
                {
                evt.StopPropagation();
                var parent = Element?.parent;
                if (parent == null) return;
                using var forwarded = PointerMoveEvent.GetPooled(evt);
                forwarded.target = parent;
                parent.SendEvent(forwarded);
            }
        }
        private void OnNavigation(NavigationMoveEvent evt)
        {
            if (_options.Enabled) return;
            evt.StopPropagation();
            var parent = Element?.parent;
            if (parent == null) return;
            using var forwarded = NavigationMoveEvent.GetPooled(evt.move, evt.modifiers);
            forwarded.target = parent; parent.SendEvent(forwarded);
        }
        private void OnCapture(PointerCaptureEvent evt)
        {
            if (!_options.Enabled || evt.target != _view!.contentContainer || evt.pointerId == UnityEngine.UIElements.PointerId.mousePointerId) return;
            _pointer = evt.pointerId;
            _drag = new IDragInteraction.Start();
            Bridge.EmitDragInteraction(_drag);
            Bridge.SetNativeScrollInProgress(true);
        }
        private void OnUp(PointerUpEvent evt) { if (evt.pointerId == _pointer) EndDrag(false); }
        private void OnCancel(PointerCancelEvent evt) { if (evt.pointerId == _pointer) EndDrag(true); }
        private void OnCaptureOut(PointerCaptureOutEvent evt) { if (evt.pointerId == _pointer) EndDrag(true); }
        private void OnPanelDetach(DetachFromPanelEvent evt)
        {
            if (evt.target != _view) return;
            EndDrag(true);
            Bridge.SetNativeScrollInProgress(false);
        }
        private void EndDrag(bool cancelled)
        {
            if (_drag == null) return;
            Bridge.EmitDragInteraction(cancelled ? new IDragInteraction.Cancel(_drag) : new IDragInteraction.Stop(_drag));
            _drag = null; _pointer = -1;
            Bridge.SetNativeScrollInProgress(false);
        }

        protected override void RevertFromContainer(UnityReusableComposeNode node, VisualElement element, VisualElement content)
        {
            EndDrag(true);
            Bridge.ValueChanged -= OnStateValue;
            Bridge.ScrollRequested -= StopNativeScroll;
            Bridge.SetNativeScrollInProgress(false);
            _measure?.Pause(); _idle?.Pause();
            if (_view == null) return;
            Scroller.valueChanged -= OnNativeValue;
            _view.UnregisterCallback<PointerDownEvent>(OnDown, TrickleDown.TrickleDown);
            _view.UnregisterCallback<WheelEvent>(OnWheelStart, TrickleDown.TrickleDown);
            _view.UnregisterCallback<NavigationMoveEvent>(OnSelfNavigation, TrickleDown.TrickleDown);
            _view.contentViewport.UnregisterCallback<GeometryChangedEvent>(OnGeometry);
            _view.contentContainer.UnregisterCallback<GeometryChangedEvent>(OnGeometry);
            content.UnregisterCallback<GeometryChangedEvent>(OnGeometry);
            content.UnregisterCallback<WheelEvent>(OnWheel);
            content.UnregisterCallback<PointerMoveEvent>(OnMove);
            content.UnregisterCallback<NavigationMoveEvent>(OnNavigation);
            _view.contentContainer.UnregisterCallback<PointerCaptureEvent>(OnCapture);
            _view.contentContainer.UnregisterCallback<PointerUpEvent>(OnUp, TrickleDown.TrickleDown);
            _view.contentContainer.UnregisterCallback<PointerCancelEvent>(OnCancel, TrickleDown.TrickleDown);
            _view.contentContainer.UnregisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
            _view.UnregisterCallback<DetachFromPanelEvent>(OnPanelDetach);
            var index = element.IndexOf(_view);
            content.RemoveFromHierarchy();
            (element as ILayout)?.Untrack(_view);
            _view.RemoveFromHierarchy();
            element.Insert(Math.Max(0, index), content);
            (element as ILayout)?.Track(content);
            content.style.flexDirection = _direction; content.style.alignItems = _alignItems;
            content.style.alignSelf = _alignSelf; content.style.justifyContent = _justify;
            content.style.flexShrink = _shrink; content.style.flexGrow = _grow;
            _view = null; _measured = false;
        }
    }
}
