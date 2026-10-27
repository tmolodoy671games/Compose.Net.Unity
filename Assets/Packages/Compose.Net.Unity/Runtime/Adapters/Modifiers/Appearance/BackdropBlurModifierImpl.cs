// ReSharper disable CheckNamespace

using Compose.Net;
using Compose.Net.Unity.Runtime.Adapters.Extensions;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record BackdropBlurModifierImpl(Dp Radius)
    : ModifierNodeElement<BackdropBlurModifierImpl.BackdropBlurNode>, IAppearanceModifier
{
    public override BackdropBlurNode Create() => new(Radius.ToPx(UnityDensity.Current));
    public override void Update(BackdropBlurNode node) => node.Update(Radius.ToPx(UnityDensity.Current));
    public override object GetTarget(IReusableComposeNode node) => node.VisualElement();

    internal sealed class BackdropBlurNode(float radius) : IModifier.Node
    {
        private float _radius = radius;
        private VisualElement? _element;

        public override void OnAttach()
        {
            _element = (VisualElement)Target;
            ApplyFilter();
        }

        internal void Update(float next)
        {
            if (_radius == next) return;
            RemoveFilter();
            _radius = next;
            ApplyFilter();
        }

        public override void OnDetach()
        {
            RemoveFilter();
            _element = null;
        }

        private void ApplyFilter()
        {
            if (_element == null || _radius <= 0f) return;
            var filter = new FilterFunction(FilterFunctionType.Blur);
            filter.AddParameter(new FilterParameter(_radius));
            _element.style.AddFilter(filter);
        }

        private void RemoveFilter()
        {
            if (_element == null || _radius <= 0f) return;
            var filter = new FilterFunction(FilterFunctionType.Blur);
            filter.AddParameter(new FilterParameter(_radius));
            _element.style.RemoveFilter(filter);
        }
    }
}
