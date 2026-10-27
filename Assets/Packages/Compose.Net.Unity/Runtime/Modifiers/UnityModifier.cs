using Compose.Net;
using StableCollections;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

public abstract record UnityModifier : IModifier
{
    public virtual object NodeKey => GetType();
    public virtual bool SupportsUpdate => false;
    public virtual bool DependsOnChain => false;
    public virtual ModifierNode CreateNode() => new UnityStyleModifierNode(this);
    public virtual void UpdateStyle(UnityStyleModifierNode node) { }
    protected virtual void OnAttach(UnityReusableComposeNode node, VisualElement element, IStableList<IModifier> newModifiers) { }
    protected virtual void OnDetach(UnityReusableComposeNode node, VisualElement element, IStableList<IModifier> newModifiers) { }
    internal void AttachResource(UnityStyleModifierNode node) => OnAttach((UnityReusableComposeNode)node.Owner, node.Element, node.Modifiers);
    internal void DetachResource(UnityStyleModifierNode node) => OnDetach((UnityReusableComposeNode)node.Owner, node.Element, node.Modifiers);
    internal void ReorderResource(UnityStyleModifierNode node)
    {
        if (this is not IDrawModifier) return;
        DetachResource(node);
        AttachResource(node);
    }
}

public interface IAppearanceModifier : IModifier { }
