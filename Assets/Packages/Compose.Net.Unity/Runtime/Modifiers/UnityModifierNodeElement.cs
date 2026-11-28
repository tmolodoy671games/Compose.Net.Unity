using System;
using System.Runtime.CompilerServices;
using Compose.Net;
using StableCollections;

namespace Compose.Net.Unity;

/// <summary>Routes a modifier to its native coordinate space when the chain needs nested layers.</summary>
public abstract record UnityModifierNodeElement<T> : ModifierNodeElement<T>, IModifierNodeElement, IUnityLayerParticipant
    where T : IModifier.Node
{
    IModifier IModifier.Fill(IStableList<IModifier> chain)
    {
        var depth = ModifierLayers.Depth(this, chain);
        var filled = FillAtDepth(chain, depth);
        // Parent data targets the outermost layer even when it is the last modifier.
        if (filled is not UnityModifierNodeElement<T> element || (depth == 0 && !ModifierLayers.IsParentData(this)))
            return filled;
        return new LayerElement(element, depth);
    }

    protected virtual IModifier FillAtDepth(IStableList<IModifier> chain, int depth) => Fill(chain);

    // Compose.Net may update a remembered prefix's final leaf without calling Fill again.
    void IModifierNodeElement.Update(IModifier.Node node)
    {
        if (node is LayerNode layer) layer.Update(this, layer.Depth);
        else Update((T)node);
    }

    private sealed record LayerElement(UnityModifierNodeElement<T> Modifier, int Depth)
        : ModifierNodeElement<LayerNode>, ILayerElement
    {
        IModifier ILayerElement.Original => Modifier;
        public override LayerNode Create() => new(Modifier, Depth);
        public override void Update(LayerNode node) => node.Update(Modifier, Depth);
    }

    private sealed class LayerNode(UnityModifierNodeElement<T> modifier, int depth) : IModifier.Node
    {
        internal int Depth { get; private set; } = depth;
        private T? _inner;
        private UnityReusableComposeNode? _host;

        public override void OnAttach()
        {
            var owner = (UnityReusableComposeNode)Target;
            _host = owner.AcquireModifierLayer(Depth);
            try
            {
                _inner = modifier.Create();
                _inner.Attach(modifier.GetTarget(_host));
            }
            catch
            {
                OnDetach();
                throw;
            }
        }

        internal void Update(UnityModifierNodeElement<T> next, int nextDepth)
        {
            if (Depth == nextDepth)
            {
                modifier = next;
                if (_inner != null) next.Update(_inner);
                return;
            }
            OnDetach();
            Depth = nextDepth;
            modifier = next;
            OnAttach();
        }

        public override void OnDetach()
        {
            if (_host == null) return;
            try { _inner?.Detach(); }
            finally
            {
                _inner?.Dispose();
                _inner = null;
                _host = null;
                ((UnityReusableComposeNode)Target).ReleaseModifierLayer(Depth);
            }
        }
    }
}

// The last leaf of a remembered chain can be updated without Flatten/Fill. When its
// axes or shape change, earlier elements may need a different layer too.
internal sealed record ChainDependentModifier(IModifierNodeElement Element) : IModifier
{
    public void Flatten(IMutableStableCollection<IModifier> modifiers) => modifiers.Add(Element);
    public bool Any(Func<IModifier, bool> predicate) => predicate(Element);
    public bool All(Func<IModifier, bool> predicate) => predicate(Element);
    public void Apply(IReusableComposeNode node, IStableList<IModifier> modifiers) => Element.Apply(node, modifiers);
    public void Revert(IReusableComposeNode node, IStableList<IModifier> modifiers) => Element.Revert(node, modifiers);
}

internal interface ILayerElement
{
    IModifier Original { get; }
    int Depth { get; }
}

internal static class ModifierLayers
{
    internal static bool IsParentData(IModifier item) =>
        item is AlignModifierImpl or WeightModifierImpl or VerticalAlignModifierImpl or HorizontalAlignModifierImpl
            or FloatModifierImpl or PositionModifierImpl or ZIndexModifierImpl;

    internal static IModifier Original(IModifier entry) => entry is ILayerElement layer ? layer.Original : entry;

    internal static bool AtDepth(IModifier entry, int depth) =>
        (entry is ILayerElement layer ? layer.Depth : 0) == depth;

    // Compose.Net fills each flattened list from left to right. Cache only the integer
    // positions, never descriptions or callbacks, since these lists are reused.
    private static readonly ConditionalWeakTable<IStableList<IModifier>, Plan> Plans = new();

    internal static int Depth(IModifier current, IStableList<IModifier> chain) =>
        Plans.GetValue(chain, static _ => new Plan()).Next(current, chain);

    [Flags]
    private enum NativeProperties
    {
        None = 0, Width = 1, Height = 2, MinWidth = 4, MaxWidth = 8,
        MinHeight = 16, MaxHeight = 32, Background = 64, ClipShape = 128
    }

    private static NativeProperties PropertiesOf(IModifier item) => item switch
    {
        SizeModifierImpl size => (size.Width.IsSpecified ? NativeProperties.Width : 0)
            | (size.Height.IsSpecified ? NativeProperties.Height : 0),
        FillMaxSizeModifierImpl fill => (fill.WidthFraction != FillMaxSizeModifierImpl.Omitted ? NativeProperties.Width | NativeProperties.MinWidth | NativeProperties.MaxWidth : 0)
            | (fill.HeightFraction != FillMaxSizeModifierImpl.Omitted ? NativeProperties.Height | NativeProperties.MinHeight | NativeProperties.MaxHeight : 0),
        SizeInModifierImpl size => (size.MinWidth.IsSpecified ? NativeProperties.MinWidth : 0)
            | (size.MaxWidth.IsSpecified ? NativeProperties.MaxWidth : 0)
            | (size.MinHeight.IsSpecified ? NativeProperties.MinHeight : 0)
            | (size.MaxHeight.IsSpecified ? NativeProperties.MaxHeight : 0),
        BackgroundColorModifierImpl => NativeProperties.Background,
        ClipModifierImpl clip when !clip.Shape.IsUnspecified => NativeProperties.ClipShape,
        _ => NativeProperties.None
    };

    private sealed class Plan
    {
        private int[] _groups = Array.Empty<int>();
        private int _cursor = -1;
        private int _outerDepth;
        private int _lastParticipant;

        internal int Next(IModifier current, IStableList<IModifier> chain)
        {
            while (_cursor >= 0 && _cursor < chain.Size && chain[_cursor] is not IUnityLayerParticipant)
                _cursor++;
            if (_cursor < 0 || _cursor >= chain.Size || !ReferenceEquals(chain[_cursor], current))
            {
                Build(chain);
                _cursor = 0;
                while (!ReferenceEquals(chain[_cursor], current)) _cursor++;
            }
            var group = _groups[_cursor];
            _cursor = _cursor == _lastParticipant ? -1 : _cursor + 1;
            return IsParentData(current) ? _outerDepth : _outerDepth - group;
        }

        private void Build(IStableList<IModifier> chain)
        {
            if (_groups.Length < chain.Size) Array.Resize(ref _groups, chain.Size);
            _outerDepth = 0;
            var hasEffect = false;
            var previousOffset = false;
            var properties = NativeProperties.None;
            var hasInnerModifier = false;
            var paddingType = PaddingType.Margin;
            var hasNativePadding = false;
            for (var i = 0; i < chain.Size; i++)
            {
                var item = Original(chain[i]);
                if (item is IUnityLayerParticipant) _lastParticipant = i;
                var offset = item is OffsetModifierImpl or ObservedOffsetModifierImpl;
                var transform = offset || item is ScaleModifierImpl or RotateModifierImpl or AlphaModifierImpl;
                var writes = PropertiesOf(item);
                var measuresContent = item is IContentMeasureModifier;
                if ((item is FillMaxSizeModifierImpl && hasNativePadding) ||
                    (measuresContent && hasInnerModifier) ||
                    (hasEffect && transform && !(offset && previousOffset)) || (writes & properties) != 0)
                {
                    _outerDepth++;
                    hasEffect = hasNativePadding = false;
                    properties = NativeProperties.None;
                }
                properties |= writes;
                _groups[i] = _outerDepth;
                if (measuresContent)
                {
                    // A measure modifier owns a Box around the rest of the chain, even on a leaf.
                    _outerDepth++;
                    hasInnerModifier = hasEffect = previousOffset = hasNativePadding = false;
                    paddingType = PaddingType.Margin;
                    properties = NativeProperties.None;
                    continue;
                }
                // Padding drawn inside an outer background reduces the maximum before an inner fill.
                if (item is PaddingModifierImpl && paddingType == PaddingType.Padding) hasNativePadding = true;
                if (item is IAppearanceModifier) paddingType = PaddingType.Padding;
                hasInnerModifier |= item is IUnityLayerParticipant && !IsParentData(item);
                // Sizing and parent data can share a transform's element. Earlier drawing
                // and pointer handlers must keep their own coordinate space.
                hasEffect |= transform || item is IModifierInputLayer or OnPlacedModifierImpl
                    || item is IAppearanceModifier and not (SizeModifierImpl or SizeInModifierImpl or FillMaxSizeModifierImpl or ZIndexModifierImpl);
                previousOffset = offset;
            }
        }
    }
}

internal interface IUnityLayerParticipant { }
internal interface IContentMeasureModifier : IAppearanceModifier { }
internal interface IModifierInputLayer { }
