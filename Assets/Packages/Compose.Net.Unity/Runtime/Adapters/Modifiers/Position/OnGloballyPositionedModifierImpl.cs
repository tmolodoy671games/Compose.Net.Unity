// ReSharper disable CheckNamespace
using System;
using Compose.Net;
using SharpExtensions;
using UnityEngine.UIElements;
using TimeUtils = Compose.Net.Unity.Runtime.Adapters.Utils.TimeUtils;

namespace Compose.Net.Unity;

internal record OnGloballyPositionedModifierImpl(Action<ILayoutCoordinates> OnGloballyPositioned)
    : ModifierNodeElement<OnGloballyPositionedModifierImpl.PositionNode>
{
    public override PositionNode Create() => new(OnGloballyPositioned);
    public override void Update(PositionNode node) => node.Update(OnGloballyPositioned);
    public override object GetTarget(IReusableComposeNode node) => node.VisualElement();

    internal sealed class PositionNode : IModifier.Node
    {
        private Action<ILayoutCoordinates> _callback;
        private VisualElement? _element;
        private IVisualElementScheduledItem? _scheduled;
        private Optional<RelativeLayoutBounds> _previous;

        internal PositionNode(Action<ILayoutCoordinates> callback) => _callback = callback;

        public override void OnAttach()
        {
            _element = (VisualElement)Target;
            _scheduled = _element.schedule.Execute(Check).Every(TimeUtils.Frametime);
            Check();
        }

        internal void Update(Action<ILayoutCoordinates> callback)
        {
            if (Equals(_callback, callback)) return;
            _callback = callback;
            _previous = default;
            Check();
        }

        private void Check()
        {
            if (_element == null) return;
            var coordinates = _element.LayoutCoordinates();
            var rect = coordinates.ToRelativeLayoutBounds();
            if (_previous.Equals(rect)) return;
            _previous = rect.ToOptional();
            _callback(coordinates);
        }

        public override void OnDetach()
        {
            _scheduled?.Pause();
            _scheduled = null;
            _element = null;
        }
    }
}
