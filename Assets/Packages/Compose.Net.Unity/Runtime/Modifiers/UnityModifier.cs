// ReSharper disable CheckNamespace

using Compose.Net;
using StableCollections;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

public abstract record UnityModifier : IModifier
{
    public void Apply(IReusableComposeNode node, IStableList<IModifier> newModifiers)
    {
        var unityNode = (UnityReusableComposeNode)node;
        Apply(
            node: unityNode,
            element: unityNode.VisualElement,
            newModifiers: newModifiers
        );
    }

    public void Revert(IReusableComposeNode node, IStableList<IModifier> newModifiers)
    {
        var unityNode = (UnityReusableComposeNode)node;
        Revert(
            node: unityNode,
            element: unityNode.VisualElement,
            newModifiers: newModifiers
        );
    }
    
    protected abstract void Apply(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    );

    protected abstract void Revert(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    );
}

public interface IAppearanceModifier : IModifier
{
}