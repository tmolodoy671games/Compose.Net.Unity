// ReSharper disable CheckNamespace

using System;
using System.Threading;
using Compose.Net;
using SharpExtensions;
using StableCollections;
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
) : ContentContainerUnityModifier
{
    private readonly EventCallback<WheelEvent> _callback = evt =>
    {
        evt.StopPropagation();
        var offset = -evt.delta.y;
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
        State.ViewportSize = element.contentRect.height;
    };

    private readonly EventCallback<GeometryChangedEvent> _onContentContainerGeometryChanged = evt =>
    {
        var element = evt.VisualElement();
        State.ContentSize = element.contentRect.height;
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
        contentContainer.RegisterCallback(_onContentContainerGeometryChanged);
        contentContainer.style.translate = new Vector2(0, -State.Value);
        contentContainer.style.flexShrink = 0;
        // As in Compose, scrolled content gets the viewport width as its max width,
        // so FillMaxWidth / Weight inside it resolve against a definite width.
        contentContainer.style.alignSelf = Align.Stretch;
        element.RegisterCallback(_onElementGeometryChanged);
        if (UserScrollEnabled)
            element.RegisterCallback(_callback, TrickleDown.TrickleDown);
        element.PickingMode().Increment();
        element.Clip().Increment();
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
        element.UnregisterCallback(_onElementGeometryChanged);
        if (UserScrollEnabled)
            element.UnregisterCallback(_callback, TrickleDown.TrickleDown);
        element.PickingMode().Decrement();
        element.Clip().Decrement();
        contentContainer.style.translate = StyleKeyword.Null;
        contentContainer.style.flexShrink = StyleKeyword.Null;
        contentContainer.style.alignSelf = StyleKeyword.Null;
        contentContainer.UnregisterCallback(_onContentContainerGeometryChanged);
    }
}
