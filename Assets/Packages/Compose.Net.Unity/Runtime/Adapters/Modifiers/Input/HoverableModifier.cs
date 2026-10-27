// ReSharper disable CheckNamespace

using System;
using Compose.Net;
using SharpExtensions;
using StableCollections;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record HoverableModiferImpl(
    IMutableInteractionSource InteractionSource,
    bool Enabled
) : UnityModifier
{
    private readonly EventCallback<PointerEnterEvent> _pointerEnterCallback = evt =>
        Enter(evt.VisualElement(), InteractionSource);

    // A new element under a still pointer gets no PointerEnter: check once after first layout.
    private void OnFirstLayout(GeometryChangedEvent evt)
    {
        if (!(evt.newRect.width > 0) || !(evt.newRect.height > 0))
            return;
        var visualElement = evt.VisualElement();
        visualElement.UnregisterCallback<GeometryChangedEvent>(OnFirstLayout);
        if (!visualElement.IsUnderPointer())
            return;
        Enter(visualElement, InteractionSource, initial: true);
        // Indication reads the initial hover at draw time: repaint this frame, not after recomposition.
        visualElement.MarkDirtyRepaint();
    }

    private static void Enter(VisualElement visualElement, IMutableInteractionSource interactionSource, bool initial = false)
    {
        var enterInteractions = visualElement.EnterInteractions(interactionSource);
        if (!enterInteractions.IsEmpty())
            return;
        var enterInteraction = new IHoverInteraction.Enter(initial);
        enterInteractions.Add(enterInteraction);
        interactionSource.Emit(enterInteraction);
    }

    private static void Exit(VisualElement visualElement, IMutableInteractionSource interactionSource)
    {
        var enterInteractions = visualElement.EnterInteractions(interactionSource);
        if (enterInteractions.IsEmpty())
            return;
        var enterInteraction = enterInteractions[enterInteractions.LastIndex];
        enterInteractions.RemoveAt(enterInteractions.LastIndex);
        interactionSource.Emit(new IHoverInteraction.Exit(enterInteraction));
    }

    private readonly EventCallback<PointerLeaveEvent> _pointerLeaveCallback = evt =>
        Exit(evt.VisualElement(), InteractionSource);

    protected override void OnAttach(
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
        if (!(element.layout.width > 0) || !(element.layout.height > 0))
            element.RegisterCallback<GeometryChangedEvent>(OnFirstLayout);
        else if (element.IsUnderPointer())
            Enter(element, InteractionSource, initial: true);
    }

    protected override void OnDetach(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    )
    {
        if (!Enabled)
            return;
        Exit(element, InteractionSource);
        element.PickingMode().Decrement();
        element.UnregisterCallback(_pointerEnterCallback);
        element.UnregisterCallback(_pointerLeaveCallback);
        element.UnregisterCallback<GeometryChangedEvent>(OnFirstLayout);
    }

    // Callback fields differ per instance, so default record equality is always false and the
    // modifier would be reverted/reapplied (Exit + Enter) on every recomposition.
    public virtual bool Equals(HoverableModiferImpl? other) =>
        other != null && Equals(InteractionSource, other.InteractionSource) && Enabled == other.Enabled;

    public override int GetHashCode() => HashCode.Combine(InteractionSource, Enabled);
}

public static partial class VisualElementExtensions
{
    private const string EnterInteractionsKey = "Compose.Net.Unity_HoverEvents";

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
