using Compose.Net;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Runtime.Adapters.Modifiers.Other;

internal record TestTagModifierImpl(string Tag) : ModifierNodeElement<TestTagModifierImpl.TestTagNode>
{
    public override TestTagNode Create() => new(Tag);
    public override void Update(TestTagNode node) => node.Update(Tag);
    public override object GetTarget(IReusableComposeNode node) => node.VisualElement();

    internal sealed class TestTagNode(string tag) : IModifier.Node
    {
        private string _tag = tag;
        private string? _originalName;
        private VisualElement? _element;

        public override void OnAttach()
        {
            _element = (VisualElement)Target;
            _originalName = _element.name;
            _element.name = _tag;
        }

        internal void Update(string next)
        {
            if (_tag == next) return;
            _tag = next;
            if (_element != null)
                _element.name = next;
        }

        public override void OnDetach()
        {
            if (_element != null)
                _element.name = _originalName;
            _element = null;
        }
    }
}
