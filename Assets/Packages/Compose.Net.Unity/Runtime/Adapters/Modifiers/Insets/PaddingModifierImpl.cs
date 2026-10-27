// ReSharper disable CheckNamespace

using System;
using System.Runtime.CompilerServices;
using Compose.Net;
using SharpExtensions;
using StableCollections;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record PaddingModifierImpl(
    Optional<Dp> Top,
    Optional<Dp> Bottom,
    Optional<Dp> Left,
    Optional<Dp> Right,
    Optional<PaddingType> Type = default
) : UnityModifier, IModifier
{
    public IModifier Fill(IStableList<IModifier> modifiers)
        => Type.HasValue ? this : this with { Type = GetPaddingType(this, modifiers) };

    protected override void Apply(UnityReusableComposeNode node, VisualElement element, IStableList<IModifier> newModifiers)
        => Change(element, 1);

    protected override void Revert(UnityReusableComposeNode node, VisualElement element, IStableList<IModifier> newModifiers)
        => Change(element, -1);

    private void Change(VisualElement element, float sign)
    {
        if (!Type.HasValue) throw new InvalidOperationException("Padding must be filled before application or removal");
        var padding = Type.Value == PaddingType.Padding;
        var style = element.style;
        if (Top.HasValue)
        {
            if (padding) style.paddingTop = NullIfNeed(style.paddingTop.value.value + sign * Top.Value.Value);
            else style.marginTop = NullIfNeed(style.marginTop.value.value + sign * Top.Value.Value);
        }
        if (Bottom.HasValue)
        {
            if (padding) style.paddingBottom = NullIfNeed(style.paddingBottom.value.value + sign * Bottom.Value.Value);
            else style.marginBottom = NullIfNeed(style.marginBottom.value.value + sign * Bottom.Value.Value);
        }
        if (Left.HasValue)
        {
            if (padding) style.paddingLeft = NullIfNeed(style.paddingLeft.value.value + sign * Left.Value.Value);
            else style.marginLeft = NullIfNeed(style.marginLeft.value.value + sign * Left.Value.Value);
        }
        if (Right.HasValue)
        {
            if (padding) style.paddingRight = NullIfNeed(style.paddingRight.value.value + sign * Right.Value.Value);
            else style.marginRight = NullIfNeed(style.marginRight.value.value + sign * Right.Value.Value);
        }
    }

    private static StyleLength NullIfNeed(float value)
        => value.AlmostEquals(0f) ? StyleKeyword.Null : value;

    private static PaddingType GetPaddingType(PaddingModifierImpl modifier, IStableList<IModifier> modifiers)
    {
        var type = PaddingType.Margin;
        for (var i = 0; i < modifiers.Size; i++)
        {
            var current = modifiers[i];
            if (ReferenceEquals(current, modifier)) return type;
            if (current is IAppearanceModifier) type = PaddingType.Padding;
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

internal enum PaddingType
{
    Padding,
    Margin,
}