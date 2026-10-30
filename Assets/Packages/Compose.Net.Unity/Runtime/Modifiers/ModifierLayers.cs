using System;
using System.Collections.Generic;
using Compose.Net;
using Compose.Net.Unity.Runtime.Adapters.Modifiers.Other;
using StableCollections;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

public partial class UnityReusableComposeNode
{
    private readonly List<int> _modifierLayers = new();
    private readonly List<int> _pendingModifierLayers = new();
    private readonly List<string> _pendingLayerNames = new();
    private readonly List<UnityReusableComposeNode> _modifierWrappers = new();
    private StyleFloat _contentGrow;

    /// <summary>The single element tracked by the parent layout, including exceptional modifier boundaries.</summary>
    public VisualElement LayoutElement => _modifierWrappers.Count == 0
        ? VisualElement : _modifierWrappers[0].VisualElement;

    private bool PrepareModifierChain(IMutableStableList<IModifier> modifiers)
    {
        Coalesce(modifiers);
        _pendingModifierLayers.Clear();
        _pendingLayerNames.Clear();
        var layer = 0;
        var bounds = false;
        var visualBounds = false;
        var padding = false;
        var drawIndex = 0;
        var width = new AxisConstraints();
        var height = new AxisConstraints();

        for (var i = 0; i < modifiers.Size; i++)
        {
            var modifier = modifiers[i];
            if (modifier is SizeModifierImpl constrained)
            {
                // An exact outer constraint already fixes this axis, including any
                // padding subtracted on the way in. No native boundary is needed
                // merely to repeat that constraint on the same content.
                if (width.IsExact) constrained = constrained with { Width = Dp.Unspecified };
                if (height.IsExact) constrained = constrained with { Height = Dp.Unspecified };
                if (constrained.Width.IsUnspecified && constrained.Height.IsUnspecified)
                {
                    modifiers.RemoveAt(i--);
                    continue;
                }
                modifier = constrained;
            }
            var parentData = IsParentData(modifier);
            var size = modifier is SizeModifierImpl or SizeInModifierImpl or FillMaxSizeModifierImpl or ObservedSizeModifierImpl;
            var offset = modifier is OffsetModifierImpl or ObservedOffsetModifierImpl;
            var observesBounds = !parentData && (modifier is IAppearanceModifier || IsInputOrCoordinates(modifier));

            // A later operation needs its own bounds only when an existing operation
            // must keep the pre-padding/pre-offset bounds. Plain padding/size/offset
            // chains continue to share the original element.
            // Native clipping, pointer handling and foreground indication can use
            // the padded element itself. They do not need a separate content box.
            var needsPaddingBoundary = modifier is IAppearanceModifier && !parentData && !size &&
                modifier is not ClipModifierImpl and not IDrawOnModifier and not HoverableModiferImpl &&
                !IsPointerInput(modifier);
            if (!parentData && ((offset && visualBounds) || (padding && bounds && needsPaddingBoundary)))
            {
                _pendingLayerNames.Add($"Compose.Modifier.{modifier.GetType().Name.Replace("ModifierImpl", "")}.{layer}");
                layer++;
                bounds = false;
                visualBounds = false;
                padding = false;
                drawIndex = 0;
            }

            if (modifier is PaddingModifierImpl inset)
            {
                modifier = inset with { Type = bounds ? PaddingType.Padding : PaddingType.Margin };
                // Size constraints alone do not paint an outer box. Native
                // padding can share that element with a later background/input.
                // Split only if an earlier visual operation needs outer bounds.
                padding |= visualBounds;
                width.Inset(Value(inset.Left) + Value(inset.Right));
                height.Inset(Value(inset.Top) + Value(inset.Bottom));
            }
            else if (modifier is SizeModifierImpl requested)
            {
                modifier = requested with
                {
                    Width = width.Size(requested.Width),
                    Height = height.Size(requested.Height),
                };
            }
            else if (modifier is SizeInModifierImpl range)
            {
                width.Restrict(range.MinWidth, range.MaxWidth);
                height.Restrict(range.MinHeight, range.MaxHeight);
                modifier = range with
                {
                    MinWidth = new Dp(width.Min), MaxWidth = width.Maximum,
                    MinHeight = new Dp(height.Min), MaxHeight = height.Maximum,
                };
            }

            if (modifier is DrawUnityModifier draw)
                modifier = draw with { DrawIndex = drawIndex++ };
            if (observesBounds || size) bounds = true;
            // Measurement and input observe the element; they do not paint an
            // outer box that must stay behind when a later offset moves it.
            // Opacity/visibility also travel with the content on the same element.
            if (modifier is IAppearanceModifier and not AlphaModifierImpl and not VisibleModifierImpl &&
                !parentData && !size) visualBounds = true;
            modifiers[i] = modifier;
            // Parent data belongs to the child as seen by its parent, regardless
            // of where it appears in the modifier chain.
            _pendingModifierLayers.Add(parentData ? 0 : layer);
        }

        var changed = _pendingLayerNames.Count != _modifierWrappers.Count;
        if (!changed && layer > 0)
        {
            changed = _modifierLayers.Count != _pendingModifierLayers.Count;
            for (var i = 0; !changed && i < _modifierLayers.Count; i++)
                changed = _modifierLayers[i] != _pendingModifierLayers[i];
        }
        if (!changed)
        {
            _modifierLayers.Clear();
            _modifierLayers.AddRange(_pendingModifierLayers);
        }
        return changed;
    }

    private void CommitModifierChain()
    {
        // All modifier nodes have detached before this runs. In particular, scroll
        // content containers and parent-data styles have already been restored.
        var previous = LayoutElement;
        var parent = previous.parent;
        var index = parent?.IndexOf(previous) ?? -1;
        (parent as ILayout)?.Untrack(previous);
        if (_modifierWrappers.Count > 0)
            VisualElement.style.flexGrow = _contentGrow;
        _contentGrow = VisualElement.style.flexGrow;
        VisualElement.RemoveFromHierarchy();
        previous.RemoveFromHierarchy();

        foreach (var wrapper in _modifierWrappers)
        {
            wrapper.VisualElement.Clear();
        }
        while (_modifierWrappers.Count > _pendingLayerNames.Count)
        {
            var last = _modifierWrappers.Count - 1;
            _modifierWrappers[last].Dispose();
            _modifierWrappers.RemoveAt(last);
        }
        while (_modifierWrappers.Count < _pendingLayerNames.Count)
        {
            var element = new Wrapper();
            var wrapper = new UnityReusableComposeNode(element);
            element.SetReusableComposeNode(this);
            _modifierWrappers.Add(wrapper);
        }
        for (var i = 0; i < _modifierWrappers.Count; i++)
        {
            var element = _modifierWrappers[i].VisualElement;
            element.name = _pendingLayerNames[i];
            element.Add(i + 1 < _modifierWrappers.Count
                ? _modifierWrappers[i + 1].VisualElement : VisualElement);
            element[0].style.flexGrow = 1;
        }
        _modifierLayers.Clear();
        _modifierLayers.AddRange(_pendingModifierLayers);
        if (parent != null)
        {
            parent.Insert(index, LayoutElement);
            (parent as ILayout)?.Track(LayoutElement);
        }
    }

    private object GetModifierTarget(IModifierNodeElement modifier, int index)
    {
        var layer = _modifierLayers[index];
        var target = layer < _modifierWrappers.Count ? _modifierWrappers[layer] : this;
        return modifier.GetTarget(target);
    }

    private static bool IsParentData(IModifier modifier) => modifier is
        WeightModifierImpl or AlignModifierImpl or HorizontalAlignModifierImpl or
        VerticalAlignModifierImpl or PositionModifierImpl or FloatModifierImpl or
        ZIndexModifierImpl or TestTagModifierImpl;

    private static bool IsInputOrCoordinates(IModifier modifier) =>
        modifier is HoverableModiferImpl or OnSizeChangedImpl or OnPlacedModifierImpl or
            OnLayoutRectChangedModifierImpl or OnGloballyPositionedModifierImpl || IsPointerInput(modifier);

    private static bool IsPointerInput(IModifier modifier)
    {
        var type = modifier.GetType();
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(PointerInputModifierImpl<>);
    }

    private static float Value(Dp value) => value.IsUnspecified ? 0 :
        value.ToPx(UnityDensity.Current) / UnityDensity.Current.Density;

    private static void Coalesce(IMutableStableList<IModifier> modifiers)
    {
        for (var i = 0; i < modifiers.Size; i++)
        {
            var current = modifiers[i];
            if (current is OffsetModifierImpl zero && Value(zero.X) == 0 && Value(zero.Y) == 0 ||
                current is PaddingModifierImpl empty && Value(empty.Top) == 0 && Value(empty.Bottom) == 0 &&
                Value(empty.Left) == 0 && Value(empty.Right) == 0)
            {
                modifiers.RemoveAt(i--);
                continue;
            }
            if (i == 0) continue;
            IModifier? combined = (modifiers[i - 1], current) switch
            {
                (OffsetModifierImpl a, OffsetModifierImpl b) => new OffsetModifierImpl(a.X + b.X, a.Y + b.Y),
                (PaddingModifierImpl a, PaddingModifierImpl b) => new PaddingModifierImpl(
                    a.Top + b.Top, a.Bottom + b.Bottom, a.Left + b.Left, a.Right + b.Right),
                (SizeModifierImpl a, SizeModifierImpl b) => new SizeModifierImpl(
                    a.Width.IsUnspecified ? b.Width : a.Width, a.Height.IsUnspecified ? b.Height : a.Height),
                _ => null,
            };
            if (combined == null) continue;
            modifiers[i - 1] = combined;
            modifiers.RemoveAt(i);
            i -= 2;
        }
    }

    private struct AxisConstraints
    {
        public float Min;
        private float _max;
        public AxisConstraints() { Min = 0; _max = float.PositiveInfinity; }
        public bool IsExact => Min == _max;
        public Dp Maximum => float.IsPositiveInfinity(_max) ? Dp.Unspecified : new Dp(_max);
        private float Constrain(float value) => Math.Max(Min, Math.Min(_max, value));
        public Dp Size(Dp value)
        {
            if (value.IsUnspecified) return value;
            Min = _max = Constrain(Value(value));
            return new Dp(Min);
        }
        public void Restrict(Dp min, Dp max)
        {
            var nextMin = min.IsUnspecified ? 0 : Value(min);
            var nextMax = max.IsUnspecified ? float.PositiveInfinity : Value(max);
            var constrainedMin = Constrain(nextMin);
            _max = Constrain(Math.Max(nextMin, nextMax));
            Min = constrainedMin;
        }
        public void Inset(float amount)
        {
            Min = Math.Max(0, Min - amount);
            _max = Math.Max(0, _max - amount);
        }
    }
    private readonly List<IModifierNodeElement> _appliedUnityModifiers = new();
    private readonly List<IModifier.Node> _unityModifierNodes = new();

    internal void ApplyUnityModifiers(IModifier[] descriptions)
    {
        var next = MutableStableListOf<IModifier>();
        foreach (var description in descriptions) next.Add(description);
        var changed = PrepareModifierChain(next);
        foreach (var modifier in next)
            if (modifier is not IModifierNodeElement)
                throw new NotSupportedException($"Unity modifier {modifier.GetType().Name} must implement IModifierNodeElement");

        if (changed)
        {
            DetachUnityModifiers();
            CommitModifierChain();
        }

        var oldCount = _appliedUnityModifiers.Count;
        var newCount = next.Size;
        var prefix = 0;
        while (prefix < oldCount && prefix < newCount &&
               _appliedUnityModifiers[prefix].GetType() == next[prefix].GetType()) prefix++;
        var suffix = 0;
        while (suffix < oldCount - prefix && suffix < newCount - prefix &&
               _appliedUnityModifiers[oldCount - suffix - 1].GetType() == next[newCount - suffix - 1].GetType()) suffix++;

        var retained = new List<IModifier.Node>(newCount);
        try
        {
            for (var i = oldCount - suffix - 1; i >= prefix; i--)
                DisposeUnityModifier(_unityModifierNodes[i]);
            for (var i = 0; i < newCount; i++)
            {
                var element = (IModifierNodeElement)next[i];
                var oldIndex = i < prefix ? i : i >= newCount - suffix ? oldCount - (newCount - i) : -1;
                IModifier.Node node;
                if (oldIndex >= 0)
                {
                    node = _unityModifierNodes[oldIndex];
                    if (!Equals(_appliedUnityModifiers[oldIndex], element)) element.Update(node);
                }
                else
                {
                    node = element.Create();
                    retained.Add(node);
                    node.Attach(GetModifierTarget(element, i));
                    continue;
                }
                retained.Add(node);
            }
        }
        catch
        {
            foreach (var node in retained) DisposeUnityModifier(node);
            DetachUnityModifiers();
            throw;
        }
        _unityModifierNodes.Clear();
        _unityModifierNodes.AddRange(retained);
        _appliedUnityModifiers.Clear();
        foreach (var modifier in next) _appliedUnityModifiers.Add((IModifierNodeElement)modifier);
    }

    internal void ClearUnityModifiers()
    {
        DetachUnityModifiers();
        var empty = MutableStableListOf<IModifier>();
        if (PrepareModifierChain(empty)) CommitModifierChain();
    }

    private void DetachUnityModifiers()
    {
        for (var i = _unityModifierNodes.Count - 1; i >= 0; i--)
            DisposeUnityModifier(_unityModifierNodes[i]);
        _unityModifierNodes.Clear();
        _appliedUnityModifiers.Clear();
    }

    private static void DisposeUnityModifier(IModifier.Node node)
    {
        try { node.Detach(); }
        finally { node.Dispose(); }
    }
}

/// <summary>Created only at a modifier boundary that cannot share native bounds.</summary>
internal sealed class Wrapper : VisualElement
{
    public Wrapper() => pickingMode = UnityEngine.UIElements.PickingMode.Ignore;
}
