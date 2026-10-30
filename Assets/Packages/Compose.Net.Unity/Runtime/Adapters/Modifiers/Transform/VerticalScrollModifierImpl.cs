using System;
using System.Threading;
using System.Threading.Tasks;
using Compose.Net;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record VerticalScrollModifierImpl(IScrollState State, bool Enabled, float ScrollMultiplier,
    bool ReverseScrolling, bool UserScrollEnabled, IMutableInteractionSource? InteractionSource,
    IFlingBehavior? FlingBehavior = null)
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
        private float _lastPointer, _velocity, _lastTime;
        private CancellationTokenSource? _fling;
        private IScrollState State => _options.State;

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
            element.RegisterCallback<PointerDownEvent>(OnDown);
            element.RegisterCallback<PointerMoveEvent>(OnMove);
            element.RegisterCallback<PointerUpEvent>(OnUp);
            element.RegisterCallback<PointerCancelEvent>(OnCancel);
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
            _lastPointer = evt.position.y;
            _lastTime = Time.realtimeSinceStartup;
            _velocity = 0;
        }

        private void OnMove(PointerMoveEvent evt)
        {
            if (evt.pointerId != _pointer || Element == null) return;
            var position = evt.position.y;
            var delta = (_lastPointer - position) * (_options.ReverseScrolling ? -1 : 1);
            if (Math.Abs(delta) < .001f) return;
            var now = Time.realtimeSinceStartup;
            _velocity = delta / Math.Max(.001f, now - _lastTime);
            _lastTime = now;
            _lastPointer = position;
            if (ScrollBy(delta) == 0) return;
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
            var cancellation = _fling = new CancellationTokenSource();
            _ = Fling(cancellation);
        }

        private void OnCancel(PointerCancelEvent evt) { if (evt.pointerId == _pointer) ReleasePointer(); }
        private void OnCaptureOut(PointerCaptureOutEvent evt) { if (evt.pointerId == _pointer) _pointer = -1; }
        private void ReleasePointer()
        {
            var pointer = _pointer;
            _pointer = -1;
            if (pointer >= 0) Element?.ReleasePointer(pointer);
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
                if (_options.FlingBehavior != null)
                    await _options.FlingBehavior.PerformFling(this, _velocity, cancellation.Token);
                else
                {

                    var last = Time.realtimeSinceStartup;
                    var velocity = _velocity;
                    while (Math.Abs(velocity) > 1)
                    {
                        await ComposeFunctions.NextFrame(cancellation.Token);
                        var now = Time.realtimeSinceStartup;
                        cancellation.Token.ThrowIfCancellationRequested();
                        var dt = now - last;
                        last = now;
                        var delta = velocity * (1 - (float)Math.Exp(-6 * dt)) / 6;
                        if (Math.Abs(ScrollBy(delta) - delta) > .01f) break;
                        velocity *= (float)Math.Exp(-6 * dt);
                    }
                }
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            finally
            {
                if (ReferenceEquals(_fling, cancellation)) _fling = null;
                cancellation.Dispose();
            }
        }

        protected override void RevertFromContainer(UnityReusableComposeNode node, VisualElement element, VisualElement contentContainer)
        {
            if (!_options.Enabled) return;
            _fling?.Cancel();
            ReleasePointer();
            State.Changed -= Render;
            element.UnregisterCallback(_geometry!);
            contentContainer.UnregisterCallback(_geometry!);
            element.UnregisterCallback(_wheel!);
            element.UnregisterCallback<PointerDownEvent>(OnDown);
            element.UnregisterCallback<PointerMoveEvent>(OnMove);
            element.UnregisterCallback<PointerUpEvent>(OnUp);
            element.UnregisterCallback<PointerCancelEvent>(OnCancel);
            element.UnregisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
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
