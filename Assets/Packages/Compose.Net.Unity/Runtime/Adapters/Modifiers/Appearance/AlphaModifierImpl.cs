// ReSharper disable CheckNamespace

using Compose.Net;
using Compose.Net.Unity.Runtime.Adapters.Extensions.Values;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record AlphaModifierImpl(float Alpha)
    : UnityModifierElement<AlphaModifierImpl.AlphaNode>, IAppearanceModifier
{
    public override AlphaNode Create() => new(Alpha);
    public override void Update(AlphaNode node) => node.Update(Alpha);
    public override object GetTarget(IReusableComposeNode node) => node.VisualElement();

    internal sealed class AlphaNode(float alpha) : IModifier.Node
    {
        private float _alpha = alpha;
        private StyleFloat _original;
        private VisualElement? _element;

        public override void OnAttach()
        {
            _element = (VisualElement)Target;
            _original = _element.style.opacity;
            _element.style.opacity = _alpha;
        }

        internal void Update(float next)
        {
            if (_alpha == next) return;
            _alpha = next;
            if (_element != null)
                _element.style.opacity = next;
        }

        public override void OnDetach()
        {
            if (_element != null)
                _element.style.opacity = _original;
            _element = null;
        }
    }
}
