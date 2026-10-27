// ReSharper disable CheckNamespace

using Compose.Net;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

public interface IForegroundShadowModifier : IAppearanceModifier
{
}

public abstract record ForegroundShadowUnityModifier<TNode> : ModifierNodeElement<TNode>, IForegroundShadowModifier
    where TNode : ForegroundShadowUnityModifier<TNode>.ForegroundShadowNodeBase
{
    public override object GetTarget(IReusableComposeNode node) => node;

    public abstract class ForegroundShadowNodeBase : IModifier.Node
    {
        protected UnityReusableComposeNode? UnityNode;
        protected VisualElement? Element;
        protected VisualElement? Shadow;

        public override void OnAttach()
        {
            UnityNode = (UnityReusableComposeNode)Target;
            Element = UnityNode.VisualElement;
            Shadow = UnityNode.AcquireForegroundShadow();
            ApplyToShadow(
                node: UnityNode,
                element: Element,
                shadow: Shadow
            );
        }

        public override void OnDetach()
        {
            if (UnityNode != null && Element != null && Shadow != null)
            {
                RevertFromShadow(
                    node: UnityNode,
                    element: Element,
                    shadow: Shadow
                );
                UnityNode.ReleaseForegroundShadow();
            }

            Shadow = null;
            Element = null;
            UnityNode = null;
        }

        protected abstract void ApplyToShadow(
            UnityReusableComposeNode node,
            VisualElement element,
            VisualElement shadow
        );

        protected abstract void RevertFromShadow(
            UnityReusableComposeNode node,
            VisualElement element,
            VisualElement shadow
        );
    }
}
