// ReSharper disable CheckNamespace

using System;
using Compose.Net;
using StableCollections;
using UnityEngine.UIElements;

namespace UnityCompose;

public interface IDrawOnModifier : IAppearanceModifier
{
}

public abstract record DrawOnTopUnityModifier : UnityModifier,
    IDrawOnModifier
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
        var drawOn = node.SetupDrawOnTop();
        drawOn.GenerateVisualContent().Insert(index, GenerateVisualContent);
        Apply(
            node: node,
            element: element,
            drawOn: node.SetupDrawOnTop(),
            newModifiers: newModifiers
        );
    }

    protected sealed override void Revert(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    )
    {
        var drawOn = node.SetupDrawOnTop();
        drawOn.GenerateVisualContent().Remove(GenerateVisualContent);
        Revert(
            node: node,
            element: element,
            drawOn: drawOn,
            newModifiers: newModifiers
        );
        foreach (var newModifier in newModifiers)
        {
            if (newModifier is IDrawOnModifier)
                return;
        }
        node.RemoveDrawOnTop();
    }
    
    protected abstract Action<MeshGenerationContext> GenerateVisualContent { get; }

    protected virtual void Apply(
        UnityReusableComposeNode node,
        VisualElement element,
        VisualElement drawOn,
        IStableList<IModifier> newModifiers
    ) {}

    protected virtual void Revert(
        UnityReusableComposeNode node,
        VisualElement element,
        VisualElement drawOn,
        IStableList<IModifier> newModifiers
    ) {}
}