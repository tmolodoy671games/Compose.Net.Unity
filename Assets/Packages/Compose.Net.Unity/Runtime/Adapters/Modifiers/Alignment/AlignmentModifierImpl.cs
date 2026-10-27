using Compose.Net;
using SharpExtensions;
using Compose.Net.Unity.Runtime.Adapters.Extensions;
using UnityEngine.UIElements;

// ReSharper disable CheckNamespace

namespace Compose.Net.Unity;

internal record AlignModifierImpl(Alignment Align) : ModifierNodeElement<AlignModifierImpl.AlignNode>
{
    public override AlignNode Create() => new(Align);
    public override void Update(AlignNode node) => node.Update(Align);
    public override object GetTarget(IReusableComposeNode node) => node.VisualElement();

    internal sealed class AlignNode(Alignment align) : IModifier.Node
    {
        private Alignment _align = align;
        private StyleEnum<Align> _originalAlignSelf;
        private bool _appliedBox;
        private bool _appliedFlex;
        private VisualElement? _element;

        public override void OnAttach()
        {
            _element = (VisualElement)Target;
            Apply();
        }

        internal void Update(Alignment next)
        {
            if (_align == next) return;
            Revert();
            _align = next;
            Apply();
        }

        public override void OnDetach()
        {
            Revert();
            _element = null;
        }

        private void Apply()
        {
            if (_element == null) return;
            if (_element.parent is Box)
            {
                Box.SetAlign(_element, _align);
                _appliedBox = true;
                return;
            }

            if (_element.parent.NotNull().style.flexDirection.value is not (FlexDirection.Row or FlexDirection.RowReverse))
                return;
            _originalAlignSelf = _element.style.alignSelf;
            _element.style.alignSelf = _align.ToAlign();
            _appliedFlex = true;
        }

        private void Revert()
        {
            if (_element == null) return;
            if (_appliedBox)
            {
                Box.SetAlign(_element, null);
                _appliedBox = false;
                return;
            }

            if (!_appliedFlex) return;
            _element.style.alignSelf = _originalAlignSelf;
            _appliedFlex = false;
        }
    }
}
