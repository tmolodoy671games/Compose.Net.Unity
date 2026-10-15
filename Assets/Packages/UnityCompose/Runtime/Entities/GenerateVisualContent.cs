// ReSharper disable CheckNamespace

using System;
using SharpExtensions;
using StableCollections;
using UnityEngine.UIElements;

namespace UnityCompose;

internal class GenerateVisualContent
{
    private readonly IMutableStableList<Action<MeshGenerationContext>> _generateVisualContent =
        MutableStableListOf<Action<MeshGenerationContext>>();

    private readonly VisualElement _element;

    public GenerateVisualContent(VisualElement element)
    {
        _element = element;
    }

    public void Insert(int index, Action<MeshGenerationContext> generateVisualContent)
    {
        index = Math.Min(index, _generateVisualContent.Size);
        _generateVisualContent.Add(index, generateVisualContent);
        _element.MarkDirtyRepaint();
    }

    public void Remove(Action<MeshGenerationContext> generateVisualContent)
    {
        _generateVisualContent.Remove(generateVisualContent);
        _element.MarkDirtyRepaint();
    }

    internal void Invoke(MeshGenerationContext context)
    {
        foreach (var generateVisualContent in _generateVisualContent)
            generateVisualContent(context);
    }
}

public static partial class VisualElementExtensions
{
    internal static GenerateVisualContent GenerateVisualContent(this VisualElement visualElement)
    {
        const string key = "UnityCompose_GenerateVisualElement";
        if (visualElement.UserData().GetOrDefault(key, null) is { } cached)
            return (GenerateVisualContent)cached.NotNull();
        var newInstance = new GenerateVisualContent(visualElement);
        visualElement.UserData()[key] = newInstance;
        visualElement.generateVisualContent = newInstance.Invoke;
        return newInstance;
    }
}