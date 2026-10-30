// ReSharper disable CheckNamespace

using Compose.Net;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record WeightModifierImpl(float Weight, bool FillSpace = true) : ModifierNodeElement<WeightModifierImpl.WeightNode>
{
    public override WeightNode Create() => new(Weight, FillSpace);
    public override void Update(WeightNode node) => node.Update(Weight, FillSpace);
    public override object GetTarget(IReusableComposeNode node) => node.VisualElement();

    internal sealed class WeightNode(float weight, bool fill) : IModifier.Node
    {
        private float _weight = weight;
        private bool _fill = fill;
        private StyleFloat _original;
        private VisualElement? _element;

        public override void OnAttach()
        {
            _element = (VisualElement)Target;
            _original = _element.style.flexGrow;
            _element.style.flexGrow = _fill ? _weight : 0;
        }

        internal void Update(float next, bool fill)
        {
            if (_weight == next && _fill == fill) return;
            _weight = next;
            _fill = fill;
            if (_element != null)
                _element.style.flexGrow = fill ? next : 0;
        }

        public override void OnDetach()
        {
            if (_element != null)
                _element.style.flexGrow = _original;
            _element = null;
        }
    }
}
