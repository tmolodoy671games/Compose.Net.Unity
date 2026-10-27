// ReSharper disable CheckNamespace

using System;
using Compose.Net;
using SharpExtensions;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record VerticalScrollModifierImpl(
    IScrollState State,
    bool Enabled,
    float ScrollMultiplier,
    bool ReverseScrolling,
    bool UserScrollEnabled,
    IMutableInteractionSource? InteractionSource
) : ContentContainerUnityModifier<VerticalScrollModifierImpl.VerticalScrollNode>
{
    public override VerticalScrollNode Create() => new(
        State,
        Enabled,
        ScrollMultiplier,
        ReverseScrolling,
        UserScrollEnabled,
        InteractionSource
    );

    public override void Update(VerticalScrollNode node) => node.Update(
        State,
        Enabled,
        ScrollMultiplier,
        ReverseScrolling,
        UserScrollEnabled,
        InteractionSource
    );

    internal sealed class VerticalScrollNode(
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
        private EventCallback<GeometryChangedEvent>? _onContentContainerGeometryChanged;

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
            _onContentContainerGeometryChanged = evt =>
            {
                var el = evt.VisualElement();
                _state.ContentSize = el.contentRect.height;
            };
            _onElementGeometryChanged = evt =>
            {
                var el = evt.VisualElement();
                _state.ViewportSize = el.contentRect.height;
            };
            _wheelCallback = evt =>
            {
                evt.StopPropagation();
                var offset = -evt.delta.y;
                if (offset.AlmostEquals(0f))
                    return;
                var multiplier = _reverseScrolling ? -1 : 1;
                offset *= -multiplier * _scrollMultiplier;
                _state.SmoothScrollBy(offset);
                _interactionSource?.Emit(new IScrollInteraction.Scroll(offset));
            };
            contentContainer.RegisterCallback(_onContentContainerGeometryChanged);
            contentContainer.style.translate = new Vector2(0, -_state.Value);
            contentContainer.style.flexShrink = 0;
            contentContainer.style.alignSelf = Align.Stretch;
            element.RegisterCallback(_onElementGeometryChanged);
            if (_userScrollEnabled)
                element.RegisterCallback(_wheelCallback, TrickleDown.TrickleDown);
            element.PickingMode().Increment();
            element.Clip().Increment();
        }

        protected override void RevertFromContainer(
            UnityReusableComposeNode node,
            VisualElement element,
            VisualElement contentContainer
        )
        {
            if (!_enabled)
                return;
            element.UnregisterCallback(_onElementGeometryChanged!);
            if (_userScrollEnabled)
                element.UnregisterCallback(_wheelCallback!, TrickleDown.TrickleDown);
            element.PickingMode().Decrement();
            element.Clip().Decrement();
            contentContainer.style.translate = StyleKeyword.Null;
            contentContainer.style.flexShrink = StyleKeyword.Null;
            contentContainer.style.alignSelf = StyleKeyword.Null;
            contentContainer.UnregisterCallback(_onContentContainerGeometryChanged!);
            _wheelCallback = null;
            _onElementGeometryChanged = null;
            _onContentContainerGeometryChanged = null;
        }
    }
}
