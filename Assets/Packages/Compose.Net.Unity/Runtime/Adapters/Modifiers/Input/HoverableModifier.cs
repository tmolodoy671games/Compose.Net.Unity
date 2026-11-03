using System;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record HoverableModiferImpl(IMutableInteractionSource InteractionSource, bool Enabled)
    : ModifierNodeElement<HoverableModiferImpl.HoverableNode>
{
    public override HoverableNode Create() => new(InteractionSource, Enabled);
    public override void Update(HoverableNode node) => node.Update(InteractionSource, Enabled);
    public override object GetTarget(IReusableComposeNode node) => node.VisualElement();

    internal sealed class HoverableNode(IMutableInteractionSource source, bool enabled) : IModifier.Node
    {
        private VisualElement? _element;
        private IHoverInteraction.Enter? _enter;
        public override void OnAttach()
        {
            _element = (VisualElement)Target;
            if (enabled) _element.PickingMode().Increment();
            _element.RegisterCallback<PointerEnterEvent>(OnEnter);
            _element.RegisterCallback<PointerLeaveEvent>(OnLeave);
            _element.RegisterCallback<DetachFromPanelEvent>(OnPanelDetach);
            _element.RegisterCallback<GeometryChangedEvent>(OnGeometry);
        }
        internal void Update(IMutableInteractionSource next, bool nextEnabled)
        {
            if (!ReferenceEquals(source, next) || enabled != nextEnabled) Exit();
            if (_element != null && enabled != nextEnabled)
            {
                if (nextEnabled) _element.PickingMode().Increment(); else _element.PickingMode().Decrement();
            }
            source = next; enabled = nextEnabled;
        }
        public override void OnDetach()
        {
            Exit();
            if (_element == null) return;
            _element.UnregisterCallback<PointerEnterEvent>(OnEnter);
            _element.UnregisterCallback<PointerLeaveEvent>(OnLeave);
            _element.UnregisterCallback<DetachFromPanelEvent>(OnPanelDetach);
            _element.UnregisterCallback<GeometryChangedEvent>(OnGeometry);
            if (enabled) _element.PickingMode().Decrement();
            _element = null;
        }
        private void OnEnter(PointerEnterEvent evt)
        {
            // Enter/Leave also trickle through ancestors. Each modifier owns one pair.
            if (!ReferenceEquals(evt.target, _element) || !enabled || !_element!.enabledInHierarchy ||
                evt.pointerType == UnityEngine.UIElements.PointerType.touch || _enter != null) return;
            _enter = new IHoverInteraction.Enter();
            source.TryEmit(_enter);
        }
        private void OnLeave(PointerLeaveEvent evt) { if (ReferenceEquals(evt.target, _element)) Exit(); }
        private void OnPanelDetach(DetachFromPanelEvent evt) => Exit();
        private void OnGeometry(GeometryChangedEvent evt) { if (evt.newRect.width <= 0 || evt.newRect.height <= 0) Exit(); }
        private void Exit()
        {
            var enter = _enter; _enter = null;
            if (enter != null) source.TryEmit(new IHoverInteraction.Exit(enter));
        }
    }
}
