// ReSharper disable CheckNamespace

using Compose.Net;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record VisibleModifierImpl(bool Visible)
    : UnityModifierElement<VisibleModifierImpl.VisibleNode>, IAppearanceModifier
{
    public override VisibleNode Create() => new(Visible);
    public override void Update(VisibleNode node) => node.Update(Visible);
    public override object GetTarget(IReusableComposeNode node) => node.VisualElement();

    internal sealed class VisibleNode(bool visible) : IModifier.Node
    {
        private bool _visible = visible;
        private bool _original;
        private VisualElement? _element;

        public override void OnAttach()
        {
            _element = (VisualElement)Target;
            _original = _element.visible;
            _element.visible = _visible;
        }

        internal void Update(bool next)
        {
            if (_visible == next) return;
            _visible = next;
            if (_element != null)
                _element.visible = next;
        }

        public override void OnDetach()
        {
            if (_element != null)
                _element.visible = _original;
            _element = null;
        }
    }
}
