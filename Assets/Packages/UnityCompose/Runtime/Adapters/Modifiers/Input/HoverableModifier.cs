// ReSharper disable CheckNamespace

using System;
using Compose.Net;
using SharpExtensions;
using StableCollections;
using UnityEngine.UIElements;

namespace UnityCompose;

internal record HoverableModiferImpl(
    IMutableInteractionSource InteractionSource,
    bool Enabled
) : UnityModifier
{
    private readonly EventCallback<PointerEnterEvent> _pointerEnterCallback = evt =>
        Enter(evt.VisualElement(), InteractionSource);

    private readonly EventCallback<GeometryChangedEvent> _geometryChangedCallback = evt =>
    {
        var appeared = !(evt.oldRect.width > 0 && evt.oldRect.height > 0) && evt.newRect.width > 0 && evt.newRect.height > 0;
        if (appeared && evt.VisualElement().IsUnderPointer())
            Enter(evt.VisualElement(), InteractionSource);
    };

    private static void Enter(VisualElement visualElement, IMutableInteractionSource interactionSource)
    {
        var enterInteractions = visualElement.EnterInteractions(interactionSource);
        if (!enterInteractions.IsEmpty())
            return;
        var enterInteraction = new IHoverInteraction.Enter();
        enterInteractions.Add(enterInteraction);
        interactionSource.Emit(enterInteraction);
    }

    private readonly EventCallback<PointerLeaveEvent> _pointerLeaveCallback = evt =>
    {
        var visualElement = evt.VisualElement();
        var enterInteractions = visualElement.EnterInteractions(InteractionSource);
        if (enterInteractions.IsEmpty())
            return;
        var enterInteraction = enterInteractions[enterInteractions.LastIndex];
        enterInteractions.RemoveAt(enterInteractions.LastIndex);
        InteractionSource.Emit(new IHoverInteraction.Exit(enterInteraction));
    };

    protected override void Apply(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    )
    {
        if (!Enabled)
            return;
        element.PickingMode().Increment();
        element.RegisterCallback(_pointerEnterCallback);
        element.RegisterCallback(_pointerLeaveCallback);
        element.RegisterCallback(_geometryChangedCallback);
        if (element.IsUnderPointer())
            Enter(element, InteractionSource);
    }

    protected override void Revert(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    )
    {
        if (!Enabled)
            return;
        element.PickingMode().Decrement();
        element.UnregisterCallback(_pointerEnterCallback);
        element.UnregisterCallback(_pointerLeaveCallback);
        element.UnregisterCallback(_geometryChangedCallback);
    }
}

public static partial class VisualElementExtensions
{
    private const string EnterInteractionsKey = "UnityCompose_HoverEvents";

    internal static IMutableStableList<IHoverInteraction.Enter> EnterInteractions(
        this VisualElement element,
        IInteractionSource interactionSource
    )
    {
        return (IMutableStableList<IHoverInteraction.Enter>)element.UserData().GetOrPut(
            (EnterInteractionsKey, interactionSource),
            static () => MutableStableListOf<IHoverInteraction.Enter>()
        ).NotNull();
    }

    internal static bool IsUnderPointer(this VisualElement element)
    {
        var pointer = UnityEngine.InputSystem.Pointer.current;
        if (pointer == null || element.panel == null)
            return false;
        var position = pointer.position.ReadValue();
        position.y = UnityEngine.Screen.height - position.y;
        return element.worldBound.Contains(RuntimePanelUtils.ScreenToPanel(element.panel, position));
    }
}