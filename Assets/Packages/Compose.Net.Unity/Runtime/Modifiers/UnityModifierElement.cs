using System;
using System.Collections.Generic;
using Compose.Net;
using StableCollections;

namespace Compose.Net.Unity;

/// <summary>Collects Unity modifiers after composable expansion using the ordinary modifier contract.</summary>
public abstract record UnityModifierElement<TNode> : ModifierNodeElement<TNode> where TNode : IModifier.Node
{
    public void Flatten(IMutableStableCollection<IModifier> modifiers)
    {
        var marked = false;
        foreach (var modifier in modifiers)
            if (modifier is UnityModifierMarker) { marked = true; break; }
        if (!marked)
        {
            if (modifiers is IMutableStableList<IModifier> list)
                list.Add(0, UnityModifierMarker.Instance);
            else modifiers.Add(UnityModifierMarker.Instance);
        }
        modifiers.Add(this);
    }
}

#if UNITY_7000_0_OR_NEWER
[global::Unity.Scripting.LifecycleManagement.NoAutoStaticsCleanup]
#endif
internal sealed class UnityModifierMarker : IModifier
{
    internal static readonly UnityModifierMarker Instance = new();
    public IModifier Fill(IStableList<IModifier> modifiers)
    {
        var mutable = (IMutableStableList<IModifier>)modifiers;
        var elements = MutableStableListOf<IModifier>();
        var sourceIndices = new List<int>();
        var marker = -1;
        for (var i = 0; i < modifiers.Size; i++)
        {
            if (modifiers[i] is UnityModifierMarker)
            {
                if (marker < 0) marker = i;
                continue;
            }
            elements.Add(modifiers[i]);
            sourceIndices.Add(i);
        }
        // Earlier entries have already been filled by the composer. Fill the rest
        // exactly once, with the marker-free chain as their context.
        var descriptions = new IModifier[elements.Size];
        for (var i = 0; i < descriptions.Length; i++)
        {
            if (sourceIndices[i] > marker)
                elements[i] = elements[i].Fill(elements);
            descriptions[i] = elements[i];
        }
        for (var i = 0; i < mutable.Size; i++)
            mutable[i] = UnityEmptyModifier.Instance;
        return new UnityModifierBatch(descriptions);
    }
    public void Apply(IReusableComposeNode node, IStableList<IModifier> modifiers) =>
        throw new InvalidOperationException("Unity modifier marker must be filled before application");
    public void Revert(IReusableComposeNode node, IStableList<IModifier> modifiers) { }
}

#if UNITY_7000_0_OR_NEWER
[global::Unity.Scripting.LifecycleManagement.NoAutoStaticsCleanup]
#endif
internal sealed class UnityEmptyModifier : IModifier
{
    internal static readonly UnityEmptyModifier Instance = new();
    public void Apply(IReusableComposeNode node, IStableList<IModifier> modifiers) { }
    public void Revert(IReusableComposeNode node, IStableList<IModifier> modifiers) { }
}

internal sealed record UnityModifierBatch(IModifier[] Elements) : IModifier
{
    public bool Equals(UnityModifierBatch? other)
    {
        if (other == null || Elements.Length != other.Elements.Length) return false;
        for (var i = 0; i < Elements.Length; i++)
            if (!Equals(Elements[i], other.Elements[i])) return false;
        return true;
    }
    public override int GetHashCode()
    {
        var hash = 0;
        foreach (var element in Elements) hash = HashCode.Combine(hash, element);
        return hash;
    }
    public void Apply(IReusableComposeNode node, IStableList<IModifier> modifiers) =>
        ((UnityReusableComposeNode)node).ApplyUnityModifiers(Elements);
    public void Revert(IReusableComposeNode node, IStableList<IModifier> modifiers)
    {
        foreach (var modifier in modifiers)
            if (modifier is UnityModifierBatch) return;
        ((UnityReusableComposeNode)node).ClearUnityModifiers();
    }
}
