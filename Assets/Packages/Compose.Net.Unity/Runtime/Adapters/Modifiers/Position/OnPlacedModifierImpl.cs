// ReSharper disable CheckNamespace

using System;
using Compose.Net;
using SharpExtensions;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record OnPlacedModifierImpl(Action<ILayoutCoordinates> OnPlaced)
    : UnityModifierElement<OnPlacedModifierImpl.PlacedNode>
{
    public override PlacedNode Create() => new(OnPlaced);
    public override void Update(PlacedNode node) => node.Update(OnPlaced);
    public override object GetTarget(IReusableComposeNode node) => node.VisualElement();

    internal sealed class PlacedNode(Action<ILayoutCoordinates> onPlaced) : IModifier.Node
    {
        private Action<ILayoutCoordinates> _onPlaced = onPlaced;
        private Optional<RelativeLayoutBounds> _previousBounds;
        private VisualElement? _element;
        private EventCallback<GeometryChangedEvent>? _callback;

        public override void OnAttach()
        {
            _element = (VisualElement)Target;
            _callback = OnGeometryChanged;
            _element.RegisterCallback(_callback);
        }

        internal void Update(Action<ILayoutCoordinates> next)
        {
            if (Equals(_onPlaced, next)) return;
            _onPlaced = next;
            _previousBounds = default;
        }

        public override void OnDetach()
        {
            if (_element != null && _callback != null)
                _element.UnregisterCallback(_callback);
            _element = null;
            _callback = null;
        }

        private void OnGeometryChanged(GeometryChangedEvent it)
        {
            var coordinates = it.VisualElement().LayoutCoordinates();
            var newBounds = coordinates.ToRelativeLayoutBounds();
            if (_previousBounds.Equals(newBounds))
                return;
            _previousBounds = newBounds;
            _onPlaced(coordinates);
        }
    }
}
