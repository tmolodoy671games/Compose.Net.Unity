using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Compose.Net;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record OffsetModifierImpl(Dp X, Dp Y) : UnityModifierNodeElement<OffsetModifierImpl.OffsetNode>
{
    public override OffsetNode Create() => new(ToPixels());
    public override void Update(OffsetNode node) => node.SetOffset(ToPixels());
    public override object GetTarget(IReusableComposeNode node) => node.VisualElement();

    private Offset ToPixels()
    {
        var density = UnityDensity.Current;
        return new Offset(MathF.Floor(X.ToPx(density) + .5f), MathF.Floor(Y.ToPx(density) + .5f));
    }

    internal sealed class OffsetNode(Offset offset) : OffsetPlacementNode(offset) { }
}

internal abstract class OffsetPlacementNode(Offset offset) : IModifier.Node
{
    private static readonly ConditionalWeakTable<VisualElement, Placement> Placements = new();
    private Placement? _placement;
    private Offset Value { get; set; } = offset;

    public override void OnAttach()
    {
        var element = (VisualElement)Target;
        _placement = Placements.GetValue(element, static view => new Placement(view));
        _placement.Offsets.Add(this);
        _placement.Apply();
    }

    internal void SetOffset(Offset next)
    {
        if (Value == next) return;
        Value = next;
        _placement?.Apply();
    }

    public override void OnDetach()
    {
        if (_placement == null) return;
        _placement.Offsets.Remove(this);
        _placement.Apply();
        if (_placement.Offsets.Count == 0)
        {
            var element = (VisualElement)Target;
            _placement.Dispose();
            Placements.Remove(element);
        }
        _placement = null;
    }

    // Adjacent placements share one native translation. Rebuild it from the live nodes
    // so updating/removing one cannot change another or accumulate rounding errors.
    private sealed class Placement
    {
        internal readonly List<OffsetPlacementNode> Offsets = new();
        private readonly VisualElement _element;
        private readonly StyleTranslate _original;
        private readonly bool _relativeToSize;

        internal Placement(VisualElement element)
        {
            _element = element;
            _original = element.style.translate;
            var translation = _original.value;
            _relativeToSize = translation.x.unit == LengthUnit.Percent || translation.y.unit == LengthUnit.Percent;
            if (_relativeToSize) element.RegisterCallback<GeometryChangedEvent>(OnGeometry);
        }

        internal void Apply()
        {
            var total = Offset.Zero;
            foreach (var node in Offsets) total += node.Value;
            if (total == Offset.Zero)
            {
                _element.style.translate = _original;
                return;
            }
            var direction = _element;
            while (direction != null && direction.languageDirection == LanguageDirection.Inherit)
                direction = direction.parent;
            var x = direction?.languageDirection == LanguageDirection.RTL ? -total.X : total.X;
            var translation = _original.value;
            _element.style.translate = new Translate(
                Pixels(translation.x, _element.layout.width) + x,
                Pixels(translation.y, _element.layout.height) + total.Y, translation.z);
        }

        private void OnGeometry(GeometryChangedEvent evt) => Apply();
        private static float Pixels(Length length, float size) =>
            length.unit == LengthUnit.Percent ? length.value * (float.IsFinite(size) ? size : 0) / 100f : length.value;

        internal void Dispose()
        {
            if (_relativeToSize) _element.UnregisterCallback<GeometryChangedEvent>(OnGeometry);
        }
    }
}
