// ReSharper disable CheckNamespace

using System;
using Compose.Net;
using SharpExtensions;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record OnSizeChangedImpl(Action<Size> OnSizeChanged)
    : ModifierNodeElement<OnSizeChangedImpl.SizeChangedNode>
{
    public override SizeChangedNode Create() => new(OnSizeChanged);
    public override void Update(SizeChangedNode node) => node.Update(OnSizeChanged);
    public override object GetTarget(IReusableComposeNode node) => node.VisualElement();

    internal sealed class SizeChangedNode(Action<Size> onSizeChanged) : IModifier.Node
    {
        private Action<Size> _onSizeChanged = onSizeChanged;
        private Optional<Size> _previousSize;
        private VisualElement? _element;
        private EventCallback<GeometryChangedEvent>? _callback;

        public override void OnAttach()
        {
            _element = (VisualElement)Target;
            _callback = OnGeometryChanged;
            _element.RegisterCallback(_callback);
        }

        internal void Update(Action<Size> next)
        {
            if (Equals(_onSizeChanged, next)) return;
            _onSizeChanged = next;
            _previousSize = default;
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
            var newSize = coordinates.Size;
            if (_previousSize.Equals(newSize))
                return;
            _previousSize = newSize;
            _onSizeChanged(newSize);
        }
    }
}
