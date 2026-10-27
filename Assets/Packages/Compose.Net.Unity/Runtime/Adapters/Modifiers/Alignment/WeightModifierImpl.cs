// ReSharper disable CheckNamespace

using Compose.Net;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record WeightModifierImpl(float Weight) : ModifierNodeElement<WeightModifierImpl.WeightNode>
{
    public override WeightNode Create() => new(Weight);
    public override void Update(WeightNode node) => node.Update(Weight);
    public override object GetTarget(IReusableComposeNode node) => node.VisualElement();

    internal sealed class WeightNode(float weight) : IModifier.Node
    {
        private float _weight = weight;
        private StyleFloat _original;
        private VisualElement? _element;

        public override void OnAttach()
        {
            _element = (VisualElement)Target;
            _original = _element.style.flexGrow;
            _element.style.flexGrow = _weight;
        }

        internal void Update(float next)
        {
            if (_weight == next) return;
            _weight = next;
            if (_element != null)
                _element.style.flexGrow = next;
        }

        public override void OnDetach()
        {
            if (_element != null)
                _element.style.flexGrow = _original;
            _element = null;
        }
    }
}
