// ReSharper disable CheckNamespace

using Compose.Net;
using StableCollections;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

public interface IContentContainerUnityModifier : IAppearanceModifier
{
}

public abstract record ContentContainerUnityModifier : UnityModifier, IContentContainerUnityModifier
{
    protected sealed override void OnAttach(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    )
    {
        OnAttach(
            node: node,
            element: element,
            contentContainer: node.SetupContentContainer(),
            newModifiers: newModifiers
        );
    }

    protected sealed override void OnDetach(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    )
    {
        OnDetach(
            node: node,
            element: element,
            contentContainer: node.SetupContentContainer(),
            newModifiers: newModifiers
        );
        foreach (var newModifier in newModifiers)
        {
            if (newModifier is IContentContainerUnityModifier)
                return;
        }
        node.RemoveContentContainer();
    }

    protected abstract void OnAttach(
        UnityReusableComposeNode node,
        VisualElement element,
        VisualElement contentContainer,
        IStableList<IModifier> newModifiers
    );

    protected abstract void OnDetach(
        UnityReusableComposeNode node,
        VisualElement element,
        VisualElement contentContainer,
        IStableList<IModifier> newModifiers
    );
}
