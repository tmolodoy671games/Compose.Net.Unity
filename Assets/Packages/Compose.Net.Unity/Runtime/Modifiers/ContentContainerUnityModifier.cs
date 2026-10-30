// ReSharper disable CheckNamespace

using Compose.Net;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

public interface IContentContainerUnityModifier : IAppearanceModifier
{
}

public abstract record ContentContainerUnityModifier<TNode> : UnityModifierElement<TNode>, IContentContainerUnityModifier
    where TNode : ContentContainerUnityModifier<TNode>.ContentContainerNodeBase
{
    public override object GetTarget(IReusableComposeNode node) => node;

    public abstract class ContentContainerNodeBase : IModifier.Node
    {
        protected UnityReusableComposeNode? UnityNode;
        protected VisualElement? Element;
        protected VisualElement? ContentContainer;

        public override void OnAttach()
        {
            UnityNode = (UnityReusableComposeNode)Target;
            Element = UnityNode.VisualElement;
            ContentContainer = UnityNode.AcquireContentContainer();
            ApplyToContainer(
                node: UnityNode,
                element: Element,
                contentContainer: ContentContainer
            );
        }

        public override void OnDetach()
        {
            if (UnityNode != null && Element != null && ContentContainer != null)
            {
                RevertFromContainer(
                    node: UnityNode,
                    element: Element,
                    contentContainer: ContentContainer
                );
                UnityNode.ReleaseContentContainer();
            }

            ContentContainer = null;
            Element = null;
            UnityNode = null;
        }

        protected abstract void ApplyToContainer(
            UnityReusableComposeNode node,
            VisualElement element,
            VisualElement contentContainer
        );

        protected abstract void RevertFromContainer(
            UnityReusableComposeNode node,
            VisualElement element,
            VisualElement contentContainer
        );
    }
}
