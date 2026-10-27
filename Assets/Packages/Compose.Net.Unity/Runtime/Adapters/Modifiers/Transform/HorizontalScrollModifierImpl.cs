// ReSharper disable CheckNamespace

using System;
using Compose.Net;
using SharpExtensions;
using Compose.Net.Unity.Runtime.Adapters.Extensions.Values;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record HorizontalScrollModifierImpl(
    IScrollState State,
    bool Enabled,
    float ScrollMultiplier,
    bool ReverseScrolling,
    bool UserScrollEnabled,
    IMutableInteractionSource? InteractionSource
) : ContentContainerUnityModifier<HorizontalScrollModifierImpl.HorizontalScrollNode>
{
    public override HorizontalScrollNode Create() => new(
        State,
        Enabled,
        ScrollMultiplier,
        ReverseScrolling,
        UserScrollEnabled,
        InteractionSource
    );

    public override void Update(HorizontalScrollNode node) => node.Update(
        State,
        Enabled,
        ScrollMultiplier,
        ReverseScrolling,
        UserScrollEnabled,
        InteractionSource
    );

    internal sealed class HorizontalScrollNode(
        IScrollState state,
        bool enabled,
        float scrollMultiplier,
        bool reverseScrolling,
        bool userScrollEnabled,
        IMutableInteractionSource? interactionSource
    ) : ContentContainerNodeBase
    {
        private IScrollState _state = state;
        private bool _enabled = enabled;
        private float _scrollMultiplier = scrollMultiplier;
        private bool _reverseScrolling = reverseScrolling;
        private bool _userScrollEnabled = userScrollEnabled;
        private IMutableInteractionSource? _interactionSource = interactionSource;

        private EventCallback<WheelEvent>? _wheelCallback;
        private EventCallback<GeometryChangedEvent>? _onElementGeometryChanged;
        private EventCallback<GeometryChangedEvent>? _onGeometryChanged;

        internal void Update(
            IScrollState nextState,
            bool nextEnabled,
            float nextScrollMultiplier,
            bool nextReverseScrolling,
            bool nextUserScrollEnabled,
            IMutableInteractionSource? nextInteractionSource
        )
        {
            if (Element != null && ContentContainer != null && _enabled)
                RevertFromContainer(UnityNode!, Element, ContentContainer);
            _state = nextState;
            _enabled = nextEnabled;
            _scrollMultiplier = nextScrollMultiplier;
            _reverseScrolling = nextReverseScrolling;
            _userScrollEnabled = nextUserScrollEnabled;
            _interactionSource = nextInteractionSource;
            if (Element != null && ContentContainer != null && _enabled)
                ApplyToContainer(UnityNode!, Element, ContentContainer);
        }

        protected override void ApplyToContainer(
            UnityReusableComposeNode node,
            VisualElement element,
            VisualElement contentContainer
        )
        {
            if (!_enabled)
                return;
            _onGeometryChanged = evt =>
            {
                var el = evt.VisualElement();
                _state.ContentSize = el.contentRect.width;
            };
            _onElementGeometryChanged = evt =>
            {
                var el = evt.VisualElement();
                _state.ViewportSize = el.contentRect.width;
            };
            _wheelCallback = evt =>
            {
                evt.StopPropagation();
                var offset = -evt.delta.x - evt.delta.y;
                if (offset.AlmostEquals(0f))
                    return;
                var multiplier = _reverseScrolling ? -1 : 1;
                offset *= -multiplier * _scrollMultiplier;
                _state.SmoothScrollBy(offset);
                _interactionSource?.Emit(new IScrollInteraction.Scroll(offset));
            };
            var containerStyle = contentContainer.style;
            contentContainer.RegisterCallback(_onGeometryChanged);
            containerStyle.translate = new Vector2(-_state.Value, 0);
            containerStyle.flexShrink = 0;
            containerStyle.flexDirection = FlexDirection.Row;
            element.Clip().Increment();
            element.RegisterCallback(_onElementGeometryChanged);
            element.PickingMode().Increment();
            if (_userScrollEnabled)
                element.RegisterCallback(_wheelCallback, TrickleDown.TrickleDown);
        }

        protected override void RevertFromContainer(
            UnityReusableComposeNode node,
            VisualElement element,
            VisualElement contentContainer
        )
        {
            if (!_enabled)
                return;
            var containerStyle = contentContainer.style;
            contentContainer.UnregisterCallback(_onGeometryChanged!);
            containerStyle.translate = containerStyle.translate.CompareAndSetNull(new Vector2(-_state.Value, 0));
            containerStyle.flexShrink = containerStyle.flexShrink.CompareAndSetNull(0);
            containerStyle.flexDirection = containerStyle.flexDirection.CompareAndSetNull(FlexDirection.Row);
            element.Clip().Decrement();
            element.UnregisterCallback(_onElementGeometryChanged!);
            element.PickingMode().Decrement();
            if (_userScrollEnabled)
                element.UnregisterCallback(_wheelCallback!, TrickleDown.TrickleDown);
            _wheelCallback = null;
            _onElementGeometryChanged = null;
            _onGeometryChanged = null;
        }
    }
}
