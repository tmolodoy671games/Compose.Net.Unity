using System.Runtime.CompilerServices;
using Compose.Net;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record ZIndexModifierImpl(float ZIndex) : UnityModifierNodeElement<ZIndexModifierImpl.ZIndexNode>, IAppearanceModifier
{
    public override ZIndexNode Create() => new(ZIndex);
    public override void Update(ZIndexNode node) => node.Update(ZIndex);
    public override object GetTarget(IReusableComposeNode node) => node.VisualElement();

    internal sealed class ZIndexNode(float value) : IModifier.Node
    {
        private static readonly ConditionalWeakTable<VisualElement, Total> Totals = new();
        private Total? _total;

        public override void OnAttach()
        {
            var element = (VisualElement)Target;
            _total = Totals.GetValue(element, static view => new Total(view.style.zIndex));
            _total.Count++;
            Add(value);
        }

        internal void Update(float next)
        {
            Add((double)next - value);
            value = next;
        }

        public override void OnDetach()
        {
            if (_total == null) return;
            var element = (VisualElement)Target;
            if (--_total.Count == 0)
            {
                element.style.zIndex = _total.Original;
                Totals.Remove(element);
            }
            else Add(-value);
            _total = null;
        }

        private void Add(double delta)
        {
            if (_total == null) return;
            _total.Value += delta;
            ((VisualElement)Target).style.zIndex = Mathf.RoundToInt((float)_total.Value);
        }

        private sealed class Total(StyleInt original)
        {
            internal readonly StyleInt Original = original;
            internal int Count;
            internal double Value = original.value;
        }
    }
}
