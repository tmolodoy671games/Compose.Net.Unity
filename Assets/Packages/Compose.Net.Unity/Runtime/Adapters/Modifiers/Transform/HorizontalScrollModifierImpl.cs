// ReSharper disable CheckNamespace

using System;
using System.Threading;
using Compose.Net;
using SharpExtensions;
using StableCollections;
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
) : ContentContainerUnityModifier
{
    private readonly EventCallback<WheelEvent> _callback = evt =>
    {
        evt.StopPropagation();
        var offset = -evt.delta.x - evt.delta.y;
        if (offset.AlmostEquals(0f))
            return;
        var multiplier = ReverseScrolling ? -1 : 1;
        offset *= -multiplier * ScrollMultiplier;
        State.SmoothScrollBy(offset);
        InteractionSource?.Emit(new IScrollInteraction.Scroll(offset));
    };

    private readonly EventCallback<GeometryChangedEvent> _onElementGeometryChanged = evt =>
    {
        var element = evt.VisualElement();
        State.ViewportSize = element.contentRect.width;
    };

    private readonly EventCallback<GeometryChangedEvent> _onGeometryChanged = evt =>
    {
        var element = evt.VisualElement();
        State.ContentSize = element.contentRect.width;
    };

    protected override void OnAttach(
        UnityReusableComposeNode node,
        VisualElement element,
        VisualElement contentContainer,
        IStableList<IModifier> newModifiers
    )
    {
        if (!Enabled)
            return;
        var containerStyle = contentContainer.style;
        contentContainer.RegisterCallback(_onGeometryChanged);
        containerStyle.translate = new Vector2(-State.Value, 0);
        containerStyle.flexShrink = 0;
        containerStyle.flexDirection = FlexDirection.Row;
        
        var style = element.style;
        element.Clip().Increment();
        element.RegisterCallback(_onElementGeometryChanged);
        element.PickingMode().Increment();
        if (UserScrollEnabled)
            element.RegisterCallback(_callback, TrickleDown.TrickleDown);
    }

    protected override void OnDetach(
        UnityReusableComposeNode node,
        VisualElement element,
        VisualElement contentContainer,
        IStableList<IModifier> newModifiers
    )
    {
        if (!Enabled)
            return;
        var containerStyle = contentContainer.style;
        contentContainer.UnregisterCallback(_onGeometryChanged);
        containerStyle.translate = containerStyle.translate.CompareAndSetNull(new Vector2(-State.Value, 0));
        containerStyle.flexShrink = containerStyle.flexShrink.CompareAndSetNull(0);
        containerStyle.flexDirection = containerStyle.flexDirection.CompareAndSetNull(FlexDirection.Row);

        var style = element.style;
        element.Clip().Decrement();
        element.UnregisterCallback(_onElementGeometryChanged);
        element.PickingMode().Decrement();
        if (UserScrollEnabled)
            element.UnregisterCallback(_callback, TrickleDown.TrickleDown);
    }
}
