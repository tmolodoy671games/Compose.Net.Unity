using System;
using System.Threading;
using System.Threading.Tasks;
using Compose.Net;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record VerticalScrollModifierImpl(IScrollState State, bool Enabled, float ScrollMultiplier,
    bool ReverseScrolling, bool UserScrollEnabled, IMutableInteractionSource? InteractionSource,
    IFlingBehavior? FlingBehavior = null, IOverscrollEffect? OverscrollEffect = null)
    : ContentContainerUnityModifier<VerticalScrollModifierImpl.VerticalScrollNode>
{
    public override VerticalScrollNode Create() => new(this);
    public override void Update(VerticalScrollNode node) => node.Update(this);

    internal sealed class VerticalScrollNode(VerticalScrollModifierImpl options) : ContentContainerNodeBase, IScrollScope
    {
        private VerticalScrollModifierImpl _options = options;
        private EventCallback<WheelEvent>? _wheel;
        private EventCallback<GeometryChangedEvent>? _geometry;
        private StyleTranslate _translate;
        private StyleFloat _shrink;
        private StyleEnum<Align> _align;
        private StyleEnum<FlexDirection> _direction;
        private StyleEnum<Align> _alignItems;
        private StyleEnum<Justify> _justify;
        private int _pointer = -1;
        private VisualElement? _capturedChild;
        private float _lastPointer, _startPointer, _velocity, _lastTime;
        private CancellationTokenSource? _fling;
        private IScrollState State => _options.State;
        private IOverscrollDisplacement? VisualEffect => _options.OverscrollEffect as IOverscrollDisplacement;

        internal void Update(VerticalScrollModifierImpl next)
        {
            if (Element != null && ContentContainer != null)
                RevertFromContainer(UnityNode!, Element, ContentContainer);
            _options = next;
            if (Element != null && ContentContainer != null)
                ApplyToContainer(UnityNode!, Element, ContentContainer);
        }

        private void Render()
        {
            if (ContentContainer == null) return;
            var position = _options.ReverseScrolling ? State.MaxValue - State.Value : State.Value;
            if (float.IsNaN(position) || float.IsInfinity(position)) position = 0;
            var displacement = VisualEffect?.Displacement.Y ?? 0;
            position -= displacement * (_options.ReverseScrolling ? -1 : 1);
            ContentContainer.style.translate = new Vector2(0, -position);
        }

        private void Measure()
        {
            if (Element == null || ContentContainer == null) return;
            if (float.IsNaN(Element.contentRect.height) || Element.contentRect.height <= 0) return;
            State.ViewportSize = Element.contentRect.height;
            State.ContentSize = ContentContainer.layout.height;
            Render();
        }

        protected override void ApplyToContainer(UnityReusableComposeNode node, VisualElement element, VisualElement contentContainer)
        {
            if (!_options.Enabled) return;
            _translate = contentContainer.style.translate;
            _shrink = contentContainer.style.flexShrink;
            _align = contentContainer.style.alignSelf;
            _direction = contentContainer.style.flexDirection;
            _alignItems = contentContainer.style.alignItems;
            _justify = contentContainer.style.justifyContent;
            contentContainer.style.flexShrink = 0;
            contentContainer.style.alignSelf = Align.Stretch;
            contentContainer.style.flexDirection = element is Row ? FlexDirection.Row : FlexDirection.Column;
            contentContainer.style.alignItems = element.style.alignItems;
            contentContainer.style.justifyContent = element.style.justifyContent;
            State.Changed += Render;
            if (VisualEffect != null) VisualEffect.Changed += Render;
            _geometry = _ => Measure();
            element.RegisterCallback(_geometry);
            contentContainer.RegisterCallback(_geometry);
            _wheel = evt =>
            {
                if (!_options.UserScrollEnabled) return;
                var delta = (evt.delta.y) * _options.ScrollMultiplier * (_options.ReverseScrolling ? -1 : 1);
                if (Math.Abs(delta) < .001f || (delta > 0 ? !State.CanScrollForward : !State.CanScrollBackward)) return;
                _fling?.Cancel();
                State.SmoothScrollBy(delta);
                _options.InteractionSource?.TryEmit(new IScrollInteraction.Scroll(delta));
                evt.StopPropagation();
            };
            element.RegisterCallback(_wheel);
            element.RegisterCallback<PointerDownEvent>(OnDown, TrickleDown.TrickleDown);
            element.RegisterCallback<PointerMoveEvent>(OnMove, TrickleDown.TrickleDown);
            element.RegisterCallback<PointerUpEvent>(OnUp, TrickleDown.TrickleDown);
            element.RegisterCallback<PointerCancelEvent>(OnCancel, TrickleDown.TrickleDown);
            element.RegisterCallback<PointerCaptureEvent>(OnCapture);
            element.RegisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
            element.PickingMode().Increment();
            element.Clip().Increment();
            Measure();
        }

        private void OnDown(PointerDownEvent evt)
        {
            if (!_options.UserScrollEnabled || evt.button != 0 || _pointer >= 0 || State.MaxValue <= 0) return;
            _fling?.Cancel();
            State.ScrollTo(State.Value);
            _pointer = evt.pointerId;
            _startPointer = _lastPointer = evt.position.y;
            _lastTime = Time.realtimeSinceStartup;
            _velocity = 0;
        }

        private void OnMove(PointerMoveEvent evt)
        {
            if (evt.pointerId != _pointer || Element == null) return;
            var position = evt.position.y;
            if (!Element.HasPointerCapture(_pointer) && Math.Abs(position - _startPointer) < 5) return;
            var delta = (_lastPointer - position) * (_options.ReverseScrolling ? -1 : 1);
            if (Math.Abs(delta) < .001f) return;
            var now = Time.realtimeSinceStartup;
            _velocity = delta / Math.Max(.001f, now - _lastTime);
            _lastTime = now;
            _lastPointer = position;
            if (ScrollWithOverscroll(delta) == 0) return;
            Element.CapturePointer(_pointer);
            evt.StopPropagation();
        }

        private void OnUp(PointerUpEvent evt)
        {
            if (evt.pointerId != _pointer) return;
            var dragged = Element != null && Element.HasPointerCapture(_pointer);
            ReleasePointer();
            if (!dragged) return;
            evt.StopPropagation();
            if (Time.realtimeSinceStartup - _lastTime > .1f) _velocity = 0;
            StartFling(_velocity);
        }

        private void OnCancel(PointerCancelEvent evt)
        {
            if (evt.pointerId != _pointer) return;
            ReleasePointer();
            if (_options.OverscrollEffect?.IsInProgress == true) StartFling(0);
        }

        private void OnCapture(PointerCaptureEvent evt)
        {
            if ((_pointer >= 0 && evt.pointerId != _pointer) || evt.target is not VisualElement target || target == Element) return;
            UntrackCapturedChild();
            _capturedChild = target;
            // Captured pointer events skip ancestors in UI Toolkit. Listen on the
            // capturing child until the scroll gesture takes over its capture.
            target.RegisterCallback<PointerMoveEvent>(OnMove, TrickleDown.TrickleDown);
            target.RegisterCallback<PointerUpEvent>(OnUp, TrickleDown.TrickleDown);
            target.RegisterCallback<PointerCancelEvent>(OnCancel, TrickleDown.TrickleDown);
        }

        private void UntrackCapturedChild()
        {
            if (_capturedChild == null) return;
            _capturedChild.UnregisterCallback<PointerMoveEvent>(OnMove, TrickleDown.TrickleDown);
            _capturedChild.UnregisterCallback<PointerUpEvent>(OnUp, TrickleDown.TrickleDown);
            _capturedChild.UnregisterCallback<PointerCancelEvent>(OnCancel, TrickleDown.TrickleDown);
            _capturedChild = null;
        }

        private void OnCaptureOut(PointerCaptureOutEvent evt)
        {

            if (evt.target == _capturedChild) UntrackCapturedChild();
            if (evt.target != Element || evt.pointerId != _pointer) return;
            _pointer = -1;
            if (_options.OverscrollEffect?.IsInProgress == true) StartFling(0);
        }

        private void StartFling(float velocity)
        {
            _fling?.Cancel();
            _velocity = velocity;
            var cancellation = _fling = new CancellationTokenSource();
            _ = Fling(cancellation);
        }
        private void ReleasePointer()
        {
            var pointer = _pointer;
            _pointer = -1;
            if (pointer >= 0) Element?.ReleasePointer(pointer);
        }

        private float ScrollWithOverscroll(float pixels)
        {
            var effect = _options.OverscrollEffect;
            if (effect == null) return ScrollBy(pixels);
            return effect.ApplyToScroll(new Offset(0, pixels), NestedScrollSource.UserInput,
                delta => new Offset(0, ScrollBy(delta.Y))).Y;
        }

        public float ScrollBy(float pixels)
        {
            var consumed = State.DispatchRawDelta(pixels);
            if (consumed != 0) _options.InteractionSource?.TryEmit(new IScrollInteraction.Scroll(consumed));
            return consumed;
        }

        private async Task Fling(CancellationTokenSource cancellation)
        {
            try
            {
                if (_options.OverscrollEffect is { } effect)
                    await effect.ApplyToFling(new Velocity(0, _velocity), async velocity => velocity - await PerformFling(velocity, cancellation.Token), cancellation.Token);
                else
                    await PerformFling(new Velocity(0, _velocity), cancellation.Token);
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            finally
            {
                if (ReferenceEquals(_fling, cancellation)) _fling = null;
                cancellation.Dispose();
            }
        }

        private async Task<Velocity> PerformFling(Velocity initialVelocity, CancellationToken token)
        {
            if (_options.FlingBehavior != null)
            {
                var remaining = await _options.FlingBehavior.PerformFling(this, initialVelocity.Y, token);
                return new Velocity(0, remaining);
            }

            var last = Time.realtimeSinceStartup;
            var velocity = initialVelocity.Y;
            while (Math.Abs(velocity) > 1)
            {
                await ComposeFunctions.NextFrame(token);
                var now = Time.realtimeSinceStartup;
                token.ThrowIfCancellationRequested();
                var dt = now - last;
                last = now;
                var delta = velocity * (1 - (float)Math.Exp(-6 * dt)) / 6;
                if (Math.Abs(ScrollBy(delta) - delta) > .01f) return new Velocity(0, velocity);
                velocity *= (float)Math.Exp(-6 * dt);
            }
            return Velocity.Zero;
        }
        protected override void RevertFromContainer(UnityReusableComposeNode node, VisualElement element, VisualElement contentContainer)
        {
            if (!_options.Enabled) return;
            _fling?.Cancel();
            ReleasePointer();
            State.Changed -= Render;
            if (VisualEffect != null) VisualEffect.Changed -= Render;
            element.UnregisterCallback(_geometry!);
            contentContainer.UnregisterCallback(_geometry!);
            element.UnregisterCallback(_wheel!);
            element.UnregisterCallback<PointerDownEvent>(OnDown, TrickleDown.TrickleDown);
            element.UnregisterCallback<PointerMoveEvent>(OnMove, TrickleDown.TrickleDown);
            element.UnregisterCallback<PointerUpEvent>(OnUp, TrickleDown.TrickleDown);
            element.UnregisterCallback<PointerCancelEvent>(OnCancel, TrickleDown.TrickleDown);
            element.UnregisterCallback<PointerCaptureEvent>(OnCapture);
            element.UnregisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
            UntrackCapturedChild();
            element.PickingMode().Decrement();
            element.Clip().Decrement();
            contentContainer.style.translate = _translate;
            contentContainer.style.flexShrink = _shrink;
            contentContainer.style.alignSelf = _align;
            contentContainer.style.flexDirection = _direction;
            contentContainer.style.alignItems = _alignItems;
            contentContainer.style.justifyContent = _justify;
        }
    }
}
