// ReSharper disable CheckNamespace

using Compose.Net;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record ZIndexModifierImpl(float ZIndex)
    : ModifierNodeElement<ZIndexModifierImpl.ZIndexNode>, IAppearanceModifier
{
    public override ZIndexNode Create() => new(ZIndex);
    public override void Update(ZIndexNode node) => node.Update(ZIndex);
    public override object GetTarget(IReusableComposeNode node) => node.VisualElement();

    internal sealed class ZIndexNode(float zIndex) : IModifier.Node
    {
        private float _zIndex = zIndex;
        private StyleInt _original;
        private VisualElement? _element;

        public override void OnAttach()
        {
            _element = (VisualElement)Target;
            _original = _element.style.zIndex;
            _element.style.zIndex = Mathf.RoundToInt(_zIndex);
        }

        internal void Update(float next)
        {
            if (_zIndex == next) return;
            _zIndex = next;
            if (_element != null)
                _element.style.zIndex = Mathf.RoundToInt(next);
        }

        public override void OnDetach()
        {
            if (_element != null)
                _element.style.zIndex = _original;
            _element = null;
        }
    }
}
