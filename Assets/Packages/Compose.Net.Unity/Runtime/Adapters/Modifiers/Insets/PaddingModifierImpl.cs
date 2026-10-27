// ReSharper disable CheckNamespace

using System;
using System.Runtime.CompilerServices;
using Compose.Net;
using SharpExtensions;
using StableCollections;
using UnityEngine;
using UnityEngine.UIElements;
using Compose.Net.Unity.Runtime.Adapters.Extensions.Values;

namespace Compose.Net.Unity;

internal record PaddingModifierImpl : UnityStyleModifier, IModifier
{
    public override bool DependsOnChain => true;
    public override void UpdateStyle(UnityStyleModifierNode node)
    {
        var padding = GetPaddingType(this, node.Modifiers) == PaddingType.Padding;
        if (_top.HasValue) node.Set<StyleLength>(padding ? node.Styles.PaddingTop : node.Styles.MarginTop, _top.Value.ToLength());
        if (_bottom.HasValue) node.Set<StyleLength>(padding ? node.Styles.PaddingBottom : node.Styles.MarginBottom, _bottom.Value.ToLength());
        if (_left.HasValue) node.Set<StyleLength>(padding ? node.Styles.PaddingLeft : node.Styles.MarginLeft, _left.Value.ToLength());
        if (_right.HasValue) node.Set<StyleLength>(padding ? node.Styles.PaddingRight : node.Styles.MarginRight, _right.Value.ToLength());
    }

    private enum PaddingType
    {
        Padding,
        Margin,
    }

    private readonly Optional<Dp> _top;
    private readonly Optional<Dp> _bottom;
    private readonly Optional<Dp> _left;
    private readonly Optional<Dp> _right;
    private readonly ReferenceKey _key;

    public PaddingModifierImpl(
        Optional<Dp> top,
        Optional<Dp> bottom,
        Optional<Dp> left,
        Optional<Dp> right
    )
    {
        _key = new ReferenceKey(this);
        _top = top;
        _bottom = bottom;
        _left = left;
        _right = right;
    }

    private static void AddTop(
        VisualElement element,
        PaddingType paddingType,
        float value
    )
    {
        if (paddingType == PaddingType.Padding)
            element.style.paddingTop = NullIfNeed(element.style.paddingTop.value.value + value);
        else
            element.style.marginTop = NullIfNeed(element.style.marginTop.value.value + value);
    }

    private static void AddBottom(
        VisualElement element,
        PaddingType paddingType,
        float value)
    {
        if (paddingType == PaddingType.Padding)
            element.style.paddingBottom = NullIfNeed(element.style.paddingBottom.value.value + value);
        else
            element.style.marginBottom = NullIfNeed(element.style.marginBottom.value.value + value);
    }

    private static void AddLeft(
        VisualElement element,
        PaddingType paddingType,
        float value)
    {
        if (paddingType == PaddingType.Padding)
            element.style.paddingLeft = NullIfNeed(element.style.paddingLeft.value.value + value);
        else
            element.style.marginLeft = NullIfNeed(element.style.marginLeft.value.value + value);
    }

    private static void AddRight(
        VisualElement element,
        PaddingType paddingType,
        float value)
    {
        if (paddingType == PaddingType.Padding)
            element.style.paddingRight = NullIfNeed(element.style.paddingRight.value.value + value);
        else
            element.style.marginRight = NullIfNeed(element.style.marginRight.value.value + value);
    }

    bool IModifier.Equals(
        IStableList<IModifier> modifiers,
        IModifier other,
        IStableList<IModifier> otherModifiers
    )
    {
        if (other is not PaddingModifierImpl paddingOther)
            return false;
        var type = GetPaddingType(this, modifiers);
        var otherType = GetPaddingType(paddingOther, otherModifiers);
        return type == otherType && Equals(other);
    }

    public virtual bool Equals(PaddingModifierImpl? other)
    {
        return other != null &&
               _top.Equals(other._top) &&
               _bottom.Equals(other._bottom) &&
               _left.Equals(other._left) &&
               _right.Equals(other._right);
    }

    public override int GetHashCode() => HashCode.Combine(_top, _bottom, _left, _right);

    private static StyleLength NullIfNeed(float value)
    {
        return value.AlmostEquals(0f) ? StyleKeyword.Null : value;
    }

    private static PaddingType GetPaddingType(PaddingModifierImpl modifier, IStableList<IModifier> newModifiers)
    {
        var index = -1;
        for (var i = 0; i < newModifiers.Size; i++)
        {
            if (ReferenceEquals(newModifiers[i], modifier))
            {
                index = i;
                break;
            }
        }

        for (var i = 0; i < newModifiers.Size; i++)
        {
            if (i >= index)
                return PaddingType.Margin;
            if (newModifiers[i] is not IAppearanceModifier)
                continue;
            return PaddingType.Padding;
        }

        return PaddingType.Margin;
    }
}

internal class ReferenceKey
{
    private readonly object? _reference;

    public ReferenceKey(object? reference)
    {
        _reference = reference;
    }

    public override int GetHashCode()
    {
        return RuntimeHelpers.GetHashCode(_reference);
    }

    public override bool Equals(object? obj)
    {
        if (obj == null)
            return false;
        if (obj.GetType() != GetType())
            return false;
        return ReferenceEquals(_reference, obj.CastTo<ReferenceKey>()._reference);
    }
}
