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
    public void Pager(IPagerState state, [Composable] Action<int> pageContent, IModifier? modifier,
        PagerSettings settings, Func<int, object>? key)
    {
        if (state is not MutablePagerStateImpl mutable)
            throw new ArgumentException("Use RememberPagerState or PagerState to create a pager state.", nameof(state));
        mutable.Configure(settings, key);
        ReusableComposeNode(
            nodeFactory: () => new PagerLayoutNode(settings.Orientation == PagerOrientation.Horizontal),
            modifier: modifier,
            initializer: node =>
            {
                if (node is not IPagerLayoutNode pager) throw new NotSupportedException("This adapter must implement IPagerLayoutNode.");
                pager.ConfigurePager(mutable, settings);
            },
            content: () =>
            {
                var first = mutable.FirstComposedPage;
                var last = mutable.LastComposedPage;
                var keys = new HashSet<object>();
                for (var i = first; i <= last; i++)
                {
                    var index = i;
                    var pageKey = mutable.KeyAt(i);
                    if (!keys.Add(pageKey)) throw new ArgumentException("Pager keys must be unique.", nameof(key));
                    Key(pageKey, () => ReusableComposeNode(
                        nodeFactory: () => new PagerItemNode(),
                        initializer: node =>
                        {
                            if (node is not IPagerItemNode item) throw new NotSupportedException("This adapter must implement IPagerItemNode.");
                            item.ConfigurePage(mutable, index, settings);
                        },
                        content: () => pageContent(index)));
                }
            });
    }
}
