// ReSharper disable CheckNamespace

using Compose.Net;
using Compose.Net.Unity.Runtime.Adapters.Extensions;
using Compose.Net.Unity.Runtime.Adapters.Extensions.Values;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record DropShadowModifierImpl(
    Shape Shape,
    Compose.Net.Shadow Shadow
) : ModifierNodeElement<DropShadowModifierImpl.DropShadowNode>, IAppearanceModifier
{
    public override DropShadowNode Create() => new(Shadow);
    public override void Update(DropShadowNode node) => node.Update(Shadow);
    public override object GetTarget(IReusableComposeNode node) => node.VisualElement();

    internal sealed class DropShadowNode(Compose.Net.Shadow shadow) : IModifier.Node
    {
        private Compose.Net.Shadow _shadow = shadow;
        private VisualElement? _element;

        public override void OnAttach()
        {
            _element = (VisualElement)Target;
            if (_shadow.Color.Alpha > 0f)
                Init(_element, _shadow);
        }

        internal void Update(Compose.Net.Shadow next)
        {
            if (_element == null)
            {
                _shadow = next;
                return;
            }

            if (_shadow.Color.Alpha > 0f)
                Revert(_element, _shadow);
            _shadow = next;
            if (_shadow.Color.Alpha > 0f)
                Init(_element, _shadow);
        }

        public override void OnDetach()
        {
            if (_element != null && _shadow.Color.Alpha > 0f)
                Revert(_element, _shadow);
            _element = null;
        }

        private static void Init(
            VisualElement shadowElement,
            Compose.Net.Shadow shadow
        )
        {
            var shadowFilter = new FilterFunction(FilterFunctionType.DropShadow);
            shadowFilter.AddParameter(new FilterParameter(shadow.Offset.X));
            shadowFilter.AddParameter(new FilterParameter(shadow.Offset.Y));
            shadowFilter.AddParameter(new FilterParameter(shadow.Radius.ToPx(UnityDensity.Current)));
            shadowFilter.AddParameter(new FilterParameter(shadow.Color.ToUnityColor()));
            shadowElement.style.AddFilters(shadowFilter);
        }

        private static void Revert(
            VisualElement shadowElement,
            Compose.Net.Shadow shadow
        )
        {
            var shadowFilter = new FilterFunction(FilterFunctionType.DropShadow);
            shadowFilter.AddParameter(new FilterParameter(shadow.Offset.X));
            shadowFilter.AddParameter(new FilterParameter(shadow.Offset.Y));
            shadowFilter.AddParameter(new FilterParameter(shadow.Radius.ToPx(UnityDensity.Current)));
            shadowFilter.AddParameter(new FilterParameter(shadow.Color.ToUnityColor()));
            shadowElement.style.RemoveFilters(shadowFilter);
        }
    }
}
