// ReSharper disable CheckNamespace
using Compose.Net;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record FloatModifierImpl(bool Enabled) : ModifierNodeElement<FloatModifierImpl.FloatNode>
{
    public override FloatNode Create() => new(Enabled);
    public override void Update(FloatNode node) => node.Update(Enabled);
    public override object GetTarget(IReusableComposeNode node) => node.VisualElement();

    internal sealed class FloatNode(bool enabled) : IModifier.Node
    {
        private bool _enabled = enabled;
        private StyleEnum<Position> _original;
        private VisualElement? _element;

        internal void Update(bool enabled)
        {
            if (_enabled == enabled) return;
            _enabled = enabled;
            if (_element != null)
                _element.style.position = enabled ? Position.Absolute : _original;
        }

        public override void OnAttach()
        {
            _element = (VisualElement)Target;
            _original = _element.style.position;
            if (_enabled) _element.style.position = Position.Absolute;
        }

        public override void OnDetach()
        {
            if (_element != null)
                _element.style.position = _original;
            _element = null;
        }
    }
}
