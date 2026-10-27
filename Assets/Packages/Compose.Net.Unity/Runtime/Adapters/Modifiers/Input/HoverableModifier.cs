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
) : ModifierNodeElement<HoverableModiferImpl.HoverableNode>
{
    public override HoverableNode Create() => new(InteractionSource, Enabled);
    public override void Update(HoverableNode node) => node.Update(InteractionSource, Enabled);
    public override object GetTarget(IReusableComposeNode node) => node.VisualElement();

    // Callback fields differ per instance, so default record equality is always false and the
    // modifier would be reverted/reapplied (Exit + Enter) on every recomposition.
    public virtual bool Equals(HoverableModiferImpl? other) =>
        other != null && Equals(InteractionSource, other.InteractionSource) && Enabled == other.Enabled;

    public override int GetHashCode() => HashCode.Combine(InteractionSource, Enabled);

    internal sealed class HoverableNode(IMutableInteractionSource interactionSource, bool enabled) : IModifier.Node
    {
        private IMutableInteractionSource _interactionSource = interactionSource;
        private bool _enabled = enabled;
        private VisualElement? _element;
        private EventCallback<PointerEnterEvent>? _pointerEnterCallback;
        private EventCallback<PointerLeaveEvent>? _pointerLeaveCallback;
        private EventCallback<GeometryChangedEvent>? _firstLayoutCallback;

        public override void OnAttach()
        {
            _element = (VisualElement)Target;
            if (!_enabled) return;
            EnableHover();
        }

        internal void Update(IMutableInteractionSource nextSource, bool nextEnabled)
        {
            if (Equals(_interactionSource, nextSource) && _enabled == nextEnabled) return;
            if (_element != null && _enabled)
                DisableHover();
            _interactionSource = nextSource;
            _enabled = nextEnabled;
            if (_element != null && _enabled)
                EnableHover();
        }

        public override void OnDetach()
        {
            if (_element != null && _enabled)
                DisableHover();
            _element = null;
        }

        private void EnableHover()
        {
            if (_element == null) return;
            _pointerEnterCallback = evt => Enter(evt.VisualElement(), _interactionSource);
            _pointerLeaveCallback = evt => Exit(evt.VisualElement(), _interactionSource);
            _firstLayoutCallback = OnFirstLayout;
            _element.PickingMode().Increment();
            _element.RegisterCallback(_pointerEnterCallback);
            _element.RegisterCallback(_pointerLeaveCallback);
            if (!(_element.layout.width > 0) || !(_element.layout.height > 0))
                _element.RegisterCallback(_firstLayoutCallback);
            else if (_element.IsUnderPointer())
                Enter(_element, _interactionSource, initial: true);
        }

        private void DisableHover()
        {
            if (_element == null) return;
            Exit(_element, _interactionSource);
            _element.PickingMode().Decrement();
            if (_pointerEnterCallback != null)
                _element.UnregisterCallback(_pointerEnterCallback);
            if (_pointerLeaveCallback != null)
                _element.UnregisterCallback(_pointerLeaveCallback);
            if (_firstLayoutCallback != null)
                _element.UnregisterCallback(_firstLayoutCallback);
            _pointerEnterCallback = null;
            _pointerLeaveCallback = null;
            _firstLayoutCallback = null;
        }

        private void OnFirstLayout(GeometryChangedEvent evt)
        {
            if (!(evt.newRect.width > 0) || !(evt.newRect.height > 0))
                return;
            var visualElement = evt.VisualElement();
            if (_firstLayoutCallback != null)
                visualElement.UnregisterCallback(_firstLayoutCallback);
            if (!visualElement.IsUnderPointer())
                return;
            Enter(visualElement, _interactionSource, initial: true);
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
    }
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
