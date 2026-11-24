using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

public partial class UnityReusableComposeNode
{
    private List<ModifierLayer>? _modifierLayers;

    internal VisualElement ModifierLayoutElement =>
        _modifierLayers is { Count: > 0 } layers ? layers[^1].VisualElement : VisualElement;

    // Depth is counted from the content. Existing inner layers survive prefix insertions.
    internal UnityReusableComposeNode AcquireModifierLayer(int depth)
    {
        if (depth == 0) return this;
        var layers = _modifierLayers ??= new();
        var index = 0;
        while (index < layers.Count && layers[index].Depth < depth) index++;
        if (index < layers.Count && layers[index].Depth == depth)
        {
            layers[index].Users++;
            return layers[index];
        }

        var inner = index == 0 ? VisualElement : layers[index - 1].VisualElement;
        var layer = new ModifierLayer(depth);
        layer.VisualElement.SetReusableComposeNode(this);
        var shared = index == layers.Count ? SharedVisual : null;
        shared?.DetachLayoutElement();
        var parent = inner.parent;
        var position = parent?.IndexOf(inner) ?? 0;
        (parent as ILayout)?.Untrack(inner);
        inner.RemoveFromHierarchy();
        layers.Insert(index, layer);
        if (parent != null)
        {
            parent.Insert(position, layer.VisualElement);
            (parent as ILayout)?.Track(layer.VisualElement);
        }
        layer.VisualElement.Add(inner);
        ((ILayout)layer.VisualElement).Track(inner);
        shared?.AttachLayoutElement();
        return layer;
    }

    internal void ReleaseModifierLayer(int depth)
    {
        if (depth == 0) return;
        var layers = _modifierLayers!;
        var index = layers.FindIndex(layer => layer.Depth == depth);
        var layer = layers[index];
        if (--layer.Users != 0) return;
        var shared = index == layers.Count - 1 ? SharedVisual : null;
        shared?.DetachLayoutElement();

        var inner = index == 0 ? VisualElement : layers[index - 1].VisualElement;
        var parent = layer.VisualElement.parent;
        var position = parent?.IndexOf(layer.VisualElement) ?? 0;
        (inner.parent as ILayout)?.Untrack(inner);
        inner.RemoveFromHierarchy();
        (parent as ILayout)?.Untrack(layer.VisualElement);
        layer.VisualElement.RemoveFromHierarchy();
        layers.RemoveAt(index);
        if (parent != null)
        {
            parent.Insert(position, inner);
            (parent as ILayout)?.Track(inner);
        }
        layer.Dispose();
        shared?.AttachLayoutElement();
    }

    private sealed class ModifierLayer(int depth) : UnityReusableComposeNode(new Box { PropagateMinConstraints = true })
    {
        internal readonly int Depth = depth;
        internal int Users = 1;
    }
}
