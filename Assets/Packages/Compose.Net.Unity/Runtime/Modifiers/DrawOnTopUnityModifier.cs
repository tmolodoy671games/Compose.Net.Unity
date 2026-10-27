// ReSharper disable CheckNamespace

using System;
using Compose.Net;
using StableCollections;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

public interface IDrawOnModifier : IAppearanceModifier
{
}

public abstract record DrawOnTopUnityModifier : UnityModifier,
    IDrawOnModifier
{
    protected sealed override void OnAttach(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    )
    {
        var index = 0;
        for (var i = 0; i < newModifiers.Size; i++)
        {
            var newModifier = newModifiers[i];
            if (ReferenceEquals(newModifier, this))
                break;
            if (newModifier is IDrawOnModifier)
                index++;
        }
        node.InsertDrawOnTop(index, GenerateVisualContent);
        OnDrawAttach(
            node: node,
            element: element,
            newModifiers: newModifiers
        );
    }

    protected sealed override void OnDetach(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    )
    {
        var keepHost = false;
        foreach (var newModifier in newModifiers)
        {
            if (newModifier is IDrawOnModifier)
            {
                keepHost = true;
                break;
            }
        }
        node.RemoveDrawOnTop(GenerateVisualContent, keepHost);
        OnDrawDetach(
            node: node,
            element: element,
            newModifiers: newModifiers
        );
    }

    // Drawn by the element itself while it has no children, and by a DrawOn view on top of them otherwise.
    protected abstract Action<MeshGenerationContext> GenerateVisualContent { get; }

    protected virtual void OnDrawAttach(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    ) {}

    protected virtual void OnDrawDetach(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    ) {}
}
