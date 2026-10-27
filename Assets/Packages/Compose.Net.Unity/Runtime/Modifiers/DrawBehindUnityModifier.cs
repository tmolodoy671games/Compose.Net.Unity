// ReSharper disable CheckNamespace

using System;
using Compose.Net;
using StableCollections;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

public interface IDrawBehindModifier : IAppearanceModifier
{
}

public abstract record DrawBehindUnityModifier : UnityModifier,
    IDrawBehindModifier
{
    protected abstract Action<MeshGenerationContext> GenerateVisualContent { get; }

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
            if (newModifier is IDrawBehindModifier)
                index++;
        }

        var drawBehind = node.SetupDrawBehind();
        drawBehind.GenerateVisualContent().Insert(index, GenerateVisualContent);
        OnDrawAttach(
            node: node,
            element: element,
            drawOn: drawBehind,
            newModifiers: newModifiers
        );
    }

    protected sealed override void OnDetach(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    )
    {
        var drawBehind = node.SetupDrawBehind();
        drawBehind.GenerateVisualContent().Remove(GenerateVisualContent);
        OnDrawDetach(
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

    protected virtual void OnDrawAttach(
        UnityReusableComposeNode node,
        VisualElement element,
        VisualElement drawOn,
        IStableList<IModifier> newModifiers
    )
    {
    }

    protected virtual void OnDrawDetach(
        UnityReusableComposeNode node,
        VisualElement element,
        VisualElement drawOn,
        IStableList<IModifier> newModifiers
    )
    {
    }
}
