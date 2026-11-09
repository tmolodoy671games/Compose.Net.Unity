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

    internal sealed class PositionNode(Action<ILayoutCoordinates> callback) : IModifier.Node
    {
        private Action<ILayoutCoordinates> _callback = callback;
        private VisualElement? _element;
        private IVisualElementScheduledItem? _scheduled;
        private Optional<RelativeLayoutBounds> _previous;

        public override void OnAttach()
        {
            _element = (VisualElement)Target;
            _element.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            // Scheduled checks also detect movement inherited from ancestors. Never
            // dispatch synchronously from modifier attachment or recomposition.
            _scheduled = _element.schedule.Execute(Check).Every(TimeUtils.Frametime);
        }

        internal void Update(Action<ILayoutCoordinates> next)
        {
            if (Equals(_callback, next)) return;
            _callback = next;
            _previous = default;
        }

        private void OnGeometryChanged(GeometryChangedEvent evt) => Check();

        private void Check()
        {
            if (_element?.panel == null) return;
            var coordinates = _element.LayoutCoordinates();
            var rect = coordinates.ToRelativeLayoutBounds();
            // Before the first layout pass UI Toolkit can expose NaN bounds.
            // Do not let these poison state (in particular animated size targets).
            if (!float.IsFinite(rect.TopLeft.X) || !float.IsFinite(rect.TopLeft.Y) ||
                !float.IsFinite(rect.BottomRight.X) || !float.IsFinite(rect.BottomRight.Y)) return;
            if (_previous.Equals(rect)) return;
            _previous = rect.ToOptional();
            _callback(coordinates);
        }

        public override void OnDetach()
        {
            _scheduled?.Pause();
            _scheduled = null;
            _element?.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            _element = null;
            _previous = default;
        }
    }
}
