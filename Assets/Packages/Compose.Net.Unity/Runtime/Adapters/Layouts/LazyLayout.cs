using System;
using System.Collections.Generic;
using System.Linq;
using SharpExtensions;
using Compose.Net;
using Compose.Net.Unity.Runtime.Adapters.Nodes;
using static Compose.Net.ComposeFunctions;

namespace Compose.Net.Unity.Runtime.Adapters;

internal partial class UnityComposeLayouts : IComposeLayouts
{
    [Composable]
    public void LazyList(Action<ILazyListScope> content, ILazyListState? state, IModifier? modifier, LazyListSettings settings)
    {
        if (content == null) throw new ArgumentNullException(nameof(content));
        state ??= RememberLazyListState();
        if (state is not ILazyListStateAdapter bridge)
            throw new ArgumentException("Lazy list state must implement ILazyListStateAdapter.", nameof(state));
        var items = new LazyListScopeImpl();
        content(items);
        ReusableComposeNode(
            nodeFactory: () => new LazyListNode(settings.Orientation == LazyListOrientation.Horizontal),
            modifier: modifier,
            initializer: node =>
            {
                if (node is not ILazyListLayoutNode layout)
                    throw new NotSupportedException("This adapter does not support lazy list virtualization.");
                layout.ConfigureLazyList(state, items, settings);
            },
            content: () => LazyItems(bridge, items, settings));
    }

    // The DSL keys must be siblings. A generated foreach adds positional replace groups
    // around each iteration, which would prevent a keyed item from moving between indices.
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Compose.Net", "CN_COMPOSABLE_CALL:Invalid Composable Member Call Site")]
    [Composable, Compiled]
    private static void LazyItems(ILazyListStateAdapter state, ILazyListItemProvider items, LazyListSettings settings)
    {
        var composer = CurrentComposer;
        const int group = 0x4C415A59;
        composer.StartRestartGroup(group);
        var changed = composer.Changed(state) | composer.Changed(items) | composer.Changed(settings);
        var restart = changed ? composer.UpdateRememberedValue<Action>(() => LazyItems(state, items, settings)) : composer.RememberedValue<Action>();
        if (!changed && !composer.IsRestarted()) composer.SkipToGroupEnd();
        else
        {
            var viewport = state.ItemParentSize;
            var p = settings.ContentPadding; var d = settings.Density;
            var parentSize = new Size(Math.Max(0, viewport.Width - d.ToPx(p.Left) - d.ToPx(p.Right)),
                Math.Max(0, viewport.Height - d.ToPx(p.Top) - d.ToPx(p.Bottom)));
            var keys = new HashSet<object>();
            foreach (var index in state.ComposedItems)
            {
                if (index < 0 || index >= items.Count) continue;
                var key = items.GetKey(index);
                if (!keys.Add(key)) throw new ArgumentException($"Duplicate lazy list key '{key}'. Keys must be unique.");
                composer.StartMovableGroup(group, key);
                LazyItem(items, index, key, parentSize);
                composer.EndMovableGroup(group);
            }
        }
        composer.EndRestartGroup(group)?.UpdateScope(restart);
    }

    [Composable]
    private static void LazyItem(ILazyListItemProvider items, int index, object key, Size parentSize) =>
        CompositionLocalProvider(LocalLazyItemParentSize.Provides(parentSize), () => ReusableComposeNode(
            nodeFactory: () => new LazyListItemNode(),
            initializer: node => ((ILazyListItemNode)node).ConfigureLazyItem(index, key, items.GetContentType(index)),
            content: () => items.ComposeItem(index)));
}
