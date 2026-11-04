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
    [Composable, NonRestartableComposable]
    private static void BoxNode(Func<IReusableComposeNode> nodeFactory, [Composable, BoxScope] Action content,
        IModifier? modifier = null, Alignment? contentAlignment = null, bool propagateMinConstraints = false)
    {
        ReusableComposeNode(nodeFactory, modifier,
            node => ComposeFunctions.Adapter.NodeFactory.Box.Apply(node, contentAlignment ?? Alignment.TopStart, propagateMinConstraints),
            content);
    }
}
