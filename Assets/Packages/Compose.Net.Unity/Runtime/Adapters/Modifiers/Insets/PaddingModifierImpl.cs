// ReSharper disable CheckNamespace

using System;
using System.Runtime.CompilerServices;
using Compose.Net;
using SharpExtensions;
using StableCollections;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record PaddingModifierImpl(
    Dp Top,
    Dp Bottom,
    Dp Left,
    Dp Right,
    Optional<PaddingType> Type = default
) : UnityModifierElement<PaddingModifierImpl.PaddingNode>
{
    public override IModifier Fill(IStableList<IModifier> modifiers)
        => Type.HasValue ? this : this with { Type = GetPaddingType(this, modifiers) };

    public override PaddingNode Create() => new(Top, Bottom, Left, Right, Type);
    public override void Update(PaddingNode node) => node.Update(Top, Bottom, Left, Right, Type);
    public override object GetTarget(IReusableComposeNode node) => node.VisualElement();

    internal sealed class PaddingNode(
        Dp top,
        Dp bottom,
        Dp left,
        Dp right,
        Optional<PaddingType> type
    ) : IModifier.Node
    {
        private Dp _top = top;
        private Dp _bottom = bottom;
        private Dp _left = left;
        private Dp _right = right;
        private Optional<PaddingType> _type = type;
        private VisualElement? _element;

        public override void OnAttach()
        {
            _element = (VisualElement)Target;
            Change(1f);
        }

        internal void Update(
            Dp nextTop,
            Dp nextBottom,
            Dp nextLeft,
            Dp nextRight,
            Optional<PaddingType> nextType
        )
        {
            Change(-1f);
            _top = nextTop;
            _bottom = nextBottom;
            _left = nextLeft;
            _right = nextRight;
            _type = nextType;
            Change(1f);
        }

        public override void OnDetach()
        {
            if (_element != null)
                Change(-1f);
            _element = null;
        }

        private void Change(float sign)
        {
            if (_element == null) return;
            if (!_type.HasValue)
                throw new InvalidOperationException("Padding must be filled before application or removal");
            var padding = _type.Value == PaddingType.Padding;
            var density = UnityDensity.Current;
            var style = _element.style;
            if (!_top.IsUnspecified)
            {
                if (padding) style.paddingTop = NullIfNeed(style.paddingTop.value.value + sign * _top.ToPx(density));
                else style.marginTop = NullIfNeed(style.marginTop.value.value + sign * _top.ToPx(density));
            }
            if (!_bottom.IsUnspecified)
            {
                if (padding) style.paddingBottom = NullIfNeed(style.paddingBottom.value.value + sign * _bottom.ToPx(density));
                else style.marginBottom = NullIfNeed(style.marginBottom.value.value + sign * _bottom.ToPx(density));
            }
            if (!_left.IsUnspecified)
            {
                if (padding) style.paddingLeft = NullIfNeed(style.paddingLeft.value.value + sign * _left.ToPx(density));
                else style.marginLeft = NullIfNeed(style.marginLeft.value.value + sign * _left.ToPx(density));
            }
            if (!_right.IsUnspecified)
            {
                if (padding) style.paddingRight = NullIfNeed(style.paddingRight.value.value + sign * _right.ToPx(density));
                else style.marginRight = NullIfNeed(style.marginRight.value.value + sign * _right.ToPx(density));
            }
        }

        private static StyleLength NullIfNeed(float value)
            => value.AlmostEquals(0f) ? StyleKeyword.Null : value;
    }

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
