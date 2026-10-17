// ReSharper disable CheckNamespace

using System;
using Compose.Net;
using StableCollections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

public interface IDrawModifier : IAppearanceModifier
{
}

public abstract record DrawUnityModifier : UnityModifier, IDrawModifier
{
    protected sealed override void Apply(
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
            if (newModifier is IDrawModifier)
                index++;
        }
        element.GenerateVisualContent().Insert(index, GenerateVisualContent);
        OnApply(
            node: node,
            element: element,
            newModifiers: newModifiers
        );
    }

    protected sealed override void Revert(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    )
    {
        element.GenerateVisualContent().Remove(GenerateVisualContent);
        OnRevert(
            node: node,
            element: element,
            newModifiers: newModifiers
        );
    }
    
    protected abstract Action<MeshGenerationContext> GenerateVisualContent { get; }
    
    protected virtual void OnApply(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    ) {}

    protected virtual void OnRevert(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    ) {}
}