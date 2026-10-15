// ReSharper disable CheckNamespace

using System;
using Compose.Net;
using StableCollections;
using UnityEngine.UIElements;

namespace UnityCompose;

public interface IDrawBehindModifier : IAppearanceModifier
{
}

public abstract record DrawBehindUnityModifier : UnityModifier,
    IDrawBehindModifier
{
    protected abstract Action<MeshGenerationContext> GenerateVisualContent { get; }

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
            if (newModifier is IDrawBehindModifier)
                index++;
        }

        var drawBehind = node.SetupDrawBehind();
        drawBehind.GenerateVisualContent().Insert(index, GenerateVisualContent);
        OnApply(
            node: node,
            element: element,
            drawOn: drawBehind,
            newModifiers: newModifiers
        );
    }

    protected sealed override void Revert(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    )
    {
        var drawBehind = node.SetupDrawBehind();
        drawBehind.GenerateVisualContent().Remove(GenerateVisualContent);
        OnRevert(
            node: node,
            element: element,
            drawOn: drawBehind,
            newModifiers: newModifiers
        );
        foreach (var newModifier in newModifiers)
        {
            if (newModifier is IDrawBehindModifier)
                return;
        }

        node.RemoveDrawBehind();
    }

    protected virtual void OnApply(
        UnityReusableComposeNode node,
        VisualElement element,
        VisualElement drawOn,
        IStableList<IModifier> newModifiers
    )
    {
    }

    protected virtual void OnRevert(
        UnityReusableComposeNode node,
        VisualElement element,
        VisualElement drawOn,
        IStableList<IModifier> newModifiers
    )
    {
    }
}