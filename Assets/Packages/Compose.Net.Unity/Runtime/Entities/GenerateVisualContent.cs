// ReSharper disable CheckNamespace

using System;
using SharpExtensions;
using StableCollections;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal class GenerateVisualContent
{
    private readonly Action<MeshGenerationContext>? _originalGenerateVisualContent;
    private readonly IMutableStableList<Action<MeshGenerationContext>?> _generateVisualContent =
        MutableStableListOf<Action<MeshGenerationContext>?>();

    private readonly VisualElement _element;
    private Action<MeshGenerationContext>? _onTop;

    // Drawn after every inserted callback.
    public Action<MeshGenerationContext>? OnTop
    {
        get => _onTop;
        set
        {
            if (_onTop == value)
                return;
            _onTop = value;
            _element.MarkDirtyRepaint();
        }
    }

    public GenerateVisualContent(VisualElement element)
    {
        _element = element;
        _originalGenerateVisualContent = element.generateVisualContent;
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
        _originalGenerateVisualContent?.Invoke(context);
        foreach (var generateVisualContent in _generateVisualContent)
            generateVisualContent?.Invoke(context);
        _onTop?.Invoke(context);
    }
}

public static partial class VisualElementExtensions
{
    internal static GenerateVisualContent GenerateVisualContent(this VisualElement visualElement)
    {
        const string key = "Compose.Net.Unity_GenerateVisualElement";
        if (visualElement.UserData().GetOrDefault(key, null) is { } cached)
            return (GenerateVisualContent)cached.NotNull();
        var newInstance = new GenerateVisualContent(visualElement);
        visualElement.UserData()[key] = newInstance;
        visualElement.generateVisualContent =
            (Action<MeshGenerationContext>)newInstance.Invoke;
        return newInstance;
    }
}