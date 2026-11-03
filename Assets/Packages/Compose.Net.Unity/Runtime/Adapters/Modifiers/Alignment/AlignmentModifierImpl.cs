using Compose.Net;
using StableCollections;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

// Align and MatchParentSize are one parent-data value in Compose: the first in a chain wins.
internal record AlignModifierImpl(Alignment Align, bool MatchParent = false, bool Active = true)
    : ModifierNodeElement<AlignModifierImpl.AlignNode>
{
    public override IModifier Fill(IStableList<IModifier> modifiers)
    {
        foreach (var modifier in modifiers)
        {
            if (ReferenceEquals(modifier, this)) break;
            if (modifier is AlignModifierImpl) return this with { Active = false };
        }
        // Resolve to a distinct description so repeated source instances still have distinct positions.
        return this with { Active = true };
    }
    public override AlignNode Create() => new(this);
    public override void Update(AlignNode node) => node.Update(this);
    public override object GetTarget(IReusableComposeNode node) => node.VisualElement();

    internal sealed class AlignNode(AlignModifierImpl value) : IModifier.Node
    {
        private AlignModifierImpl _value = value;
        private VisualElement? _element;
        public override void OnAttach() { _element = (VisualElement)Target; Apply(); }
        internal void Update(AlignModifierImpl value)
        {
            if (_value == value) return;
            Clear(); _value = value; Apply();
        }
        public override void OnDetach() { Clear(); _element = null; }
        private void Apply()
        {
            if (_element != null && _value.Active) Box.SetChildData(_element, _value.Align, _value.MatchParent);
        }
        private void Clear()
        {
            if (_element != null && _value.Active) Box.SetChildData(_element, null, false);
        }
    }
}
