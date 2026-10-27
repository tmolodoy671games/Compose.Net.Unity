using System;
using System.Collections.Generic;
using Compose.Net;
using StableCollections;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

public abstract record UnityStyleModifier : UnityModifier, IModifier
{
    public override bool SupportsUpdate => true;
    public abstract override void UpdateStyle(UnityStyleModifierNode node);
}

public sealed class UnityStyleModifierNode : ModifierNode
{
    private UnityModifier _modifier;
    private readonly List<IModifierValueBinding> _bindings = new();
    private int _cursor;
    internal ModifierStyles Styles => ((UnityReusableComposeNode)Owner).Styles;
    public VisualElement Element => ((UnityReusableComposeNode)Owner).VisualElement;
    internal ModifierStyles? ParentStyles => (Element.parent?.GetReusableComposeNode() as UnityReusableComposeNode)?.Styles;
    internal UnityStyleModifierNode(UnityModifier modifier) => _modifier = modifier;
    public override bool RequiresUpdateOnChainChange => _modifier.DependsOnChain;
    public override bool CanUpdate(IModifier modifier)
        => _modifier.SupportsUpdate && modifier is UnityModifier other && Equals(_modifier.NodeKey, other.NodeKey);
    public override void Update(IModifier modifier)
    {
        _modifier = (UnityModifier)modifier;
        Refresh();
    }
    protected override void OnAttach()
    {
        _modifier.AttachResource(this);
        Refresh();
    }
    protected override void OnOrderChanged()
    {
        _modifier.ReorderResource(this);
        foreach (var binding in _bindings) binding.InvalidateOrder();
    }
    internal void Refresh()
    {
        _cursor = 0;
        _modifier.UpdateStyle(this);
        while (_bindings.Count > _cursor)
        {
            var last = _bindings.Count - 1;
            _bindings[last].Dispose();
            _bindings.RemoveAt(last);
        }
    }
    public void Set<T>(ModifierValueSlot<T> slot, T value)
    {
        if (_cursor < _bindings.Count && _bindings[_cursor] is ModifierValueBinding<T> existing && ReferenceEquals(existing.Slot, slot))
            existing.Set(value);
        else
        {
            if (_cursor < _bindings.Count)
            {
                _bindings[_cursor].Dispose();
                _bindings[_cursor] = slot.Add(this, value);
            }
            else _bindings.Add(slot.Add(this, value));
        }
        _cursor++;
    }
    protected override void OnDetach()
    {
        _modifier.DetachResource(this);
        for (var i = _bindings.Count - 1; i >= 0; i--) _bindings[i].Dispose();
        _bindings.Clear();
    }
}
