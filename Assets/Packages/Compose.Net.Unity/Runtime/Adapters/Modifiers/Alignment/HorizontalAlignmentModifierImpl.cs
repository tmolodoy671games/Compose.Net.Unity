using Compose.Net;
using SharpExtensions;
using Compose.Net.Unity.Runtime.Adapters.Extensions;
using UnityEngine.UIElements;

// ReSharper disable CheckNamespace

namespace Compose.Net.Unity;

internal record HorizontalAlignModifierImpl(Alignment.Horizontal Align)
    : UnityModifierElement<HorizontalAlignModifierImpl.HorizontalAlignNode>
{
    public override HorizontalAlignNode Create() => new(Align);
    public override void Update(HorizontalAlignNode node) => node.Update(Align);
    public override object GetTarget(IReusableComposeNode node) => node.VisualElement();

    internal sealed class HorizontalAlignNode(Alignment.Horizontal align) : IModifier.Node
    {
        private Alignment.Horizontal _align = align;
        private StyleEnum<Align> _original;
        private bool _applied;
        private VisualElement? _element;

        public override void OnAttach()
        {
            _element = (VisualElement)Target;
            Apply();
        }

        internal void Update(Alignment.Horizontal next)
        {
            if (_align == next) return;
            RevertIfApplied();
            _align = next;
            Apply();
        }

        public override void OnDetach()
        {
            RevertIfApplied();
            _element = null;
        }

        private void Apply()
        {
            if (_element == null) return;
            if (_element.parent.NotNull().style.flexDirection.value is not (FlexDirection.Column
                or FlexDirection.ColumnReverse))
                return;
            _original = _element.style.alignSelf;
            _element.style.alignSelf = _align.ToAlign();
            _applied = true;
        }

        private void RevertIfApplied()
        {
            if (_element == null || !_applied) return;
            _element.style.alignSelf = _original;
            _applied = false;
        }
    }
}
