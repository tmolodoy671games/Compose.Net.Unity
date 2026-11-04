using Compose.Net;

namespace Compose.Net.Unity.Runtime.Adapters.Nodes;

/// <summary>The adapter's native scroll view owns input, clipping and content translation.</summary>
internal interface IPagerLayoutNode
{
    void ConfigurePager(IMutablePagerState state, PagerSettings settings);
}

internal interface IPagerItemNode
{
    void ConfigurePage(IMutablePagerState state, int index, PagerSettings settings);
}

/// <summary>Native virtualizing host contract. It must compose only requested indices.</summary>
internal interface ILazyGridLayoutNode
{
    void ConfigureLazyGrid(ILazyGridState state, ILazyGridItemProvider items, LazyGridSettings settings);
}

internal interface ILazyGridItemNode
{
    void ConfigureGridItem(int index, object key, object? contentType);
}

/// <summary>Native virtualizing host contract. It must compose only requested indices.</summary>
internal interface ILazyListLayoutNode
{
    void ConfigureLazyList(ILazyListState state, ILazyListItemProvider items, LazyListSettings settings);
}

internal interface ILazyListItemNode
{
    void ConfigureLazyItem(int index, object key, object? contentType);
}

/// <summary>Layout contract for adapters: measure the target independently of outgoing content.</summary>
internal interface IAnimatedContentLayoutNode
{
    void UpdateAnimatedContent(AnimatedContentLayout layout);
}


internal sealed record AnimatedContentLayoutModifier(AnimatedContentLayout Layout, int Revision, Alignment Alignment, string Label)
    : ModifierNodeElement<AnimatedContentLayoutModifier.LayoutNode>
{
    public override LayoutNode Create() => new(Layout);
    public override void Update(LayoutNode node) => node.Update(Layout);
    internal sealed class LayoutNode(AnimatedContentLayout layout) : IModifier.Node
    {
        public override void OnAttach() => Update(layout);
        internal void Update(AnimatedContentLayout value) => (Target as IAnimatedContentLayoutNode)?.UpdateAnimatedContent(value);
    }
}

internal interface IAnimatedContentItemNode
{
    void UpdateAnimatedContentItem(int id, bool isTarget, bool hidden);
}
internal sealed record AnimatedContentItemModifier(int Id, bool IsTarget, bool Hidden, int Revision)
    : ModifierNodeElement<AnimatedContentItemModifier.ItemNode>
{
    public override ItemNode Create() => new(Id, IsTarget, Hidden);
    public override void Update(ItemNode node) => node.Update(Id, IsTarget, Hidden);
    internal sealed class ItemNode(int id, bool isTarget, bool hidden) : IModifier.Node
    {
        public override void OnAttach() => Update(id, isTarget, hidden);
        internal void Update(int entryId, bool target, bool hide) => (Target as IAnimatedContentItemNode)?.UpdateAnimatedContentItem(entryId, target, hide);
    }
}
