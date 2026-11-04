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
    public void LazyGrid(Action<ILazyGridScope> content, IModifier? modifier, ILazyGridState? state, LazyGridSettings settings)
    {
        if (content == null) throw new ArgumentNullException(nameof(content));
        if (settings.Cells == null) throw new ArgumentNullException(nameof(settings.Cells));
        state ??= RememberLazyGridState();
        if (state is not ILazyGridStateAdapter bridge) throw new ArgumentException("Grid state must implement ILazyGridStateAdapter.", nameof(state));
        var items = new LazyGridScopeImpl(); content(items);
        ReusableComposeNode(
            nodeFactory: () => new LazyGridNode(settings.Orientation == LazyGridOrientation.Horizontal),
            modifier: modifier,
            initializer: node =>
            {
                if (node is not ILazyGridLayoutNode grid) throw new NotSupportedException("This adapter does not support lazy grids.");
                grid.ConfigureLazyGrid(state, items, settings);
            }, content: () => LazyGridItems(bridge, items));
    }

    // The DSL keys must be siblings. A generated foreach adds positional replace groups
    // around each iteration, which would prevent a keyed item from moving between indices.
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Compose.Net", "CN_COMPOSABLE_CALL:Invalid Composable Member Call Site")]
    [Composable, Compiled]
    private static void LazyGridItems(ILazyGridStateAdapter state, ILazyGridItemProvider items)
    {
        var composer = CurrentComposer;
        const int group = 0x47524944;
        composer.StartRestartGroup(group);
        var changed = composer.Changed(state) | composer.Changed(items);
        var restart = changed ? composer.UpdateRememberedValue<Action>(() => LazyGridItems(state, items)) : composer.RememberedValue<Action>();
        if (!changed && !composer.IsRestarted()) composer.SkipToGroupEnd();
        else
        {
            var keys = new HashSet<object>();
            foreach (var index in state.ComposedItems)
            {
                if (index < 0 || index >= items.Count) continue;
                var key = items.GetKey(index);
                if (!keys.Add(key)) throw new ArgumentException($"Duplicate grid key '{key}'. Keys must be unique.");
                composer.StartMovableGroup(group, key);
                GridItem(items, index, key);
                composer.EndMovableGroup(group);
            }
        }
        composer.EndRestartGroup(group)?.UpdateScope(restart);
    }

    [Composable]
    private static void GridItem(ILazyGridItemProvider items, int index, object key) =>
        ReusableComposeNode(
            nodeFactory: () => new LazyGridItemNode(),
            initializer: node => ((ILazyGridItemNode)node).ConfigureGridItem(index, key, items.GetContentType(index)),
            content: () => items.ComposeItem(index));
}
