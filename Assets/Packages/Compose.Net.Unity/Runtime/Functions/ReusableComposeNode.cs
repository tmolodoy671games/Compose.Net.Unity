// ReSharper disable CheckNamespace

using System;
using Compose.Net;
using Compose.Net.Unity.Runtime.Adapters.Nodes;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

public static partial class ComposeUnityFunctions
{
    private static readonly BoxNodeFactoryImpl CustomBoxFactory = new();
    private static readonly RowNodeFactoryImpl CustomRowFactory = new();
    private static readonly ColumnNodeFactoryImpl CustomColumnFactory = new();
    /// <summary>Creates the element once per composition node and updates it on recomposition.</summary>
    [Composable, NonRestartableComposable]
    public static void ReusableComposeNode<TElement>(
        Func<TElement> visualElementFactory,
        IModifier? modifier = null,
        Action<TElement>? initializer = null,
        [Composable] Action? content = null
    ) where TElement : VisualElement
    {
        if (visualElementFactory == null) throw new ArgumentNullException(nameof(visualElementFactory));
        ComposeFunctions.ReusableComposeNode(
            nodeFactory: () => new UnityReusableComposeNode(visualElementFactory()),
            modifier: modifier,
            initializer: initializer == null ? null : node => initializer((TElement)((UnityReusableComposeNode)node).VisualElement),
            content: content
        );
    }

    [Composable, NonRestartableComposable]
    public static void Column(
        Func<Column> visualElementFactory,
        [Composable, ColumnScope] Action content,
        IModifier? modifier = null,
        Alignment.Horizontal? horizontalAlignment = null,
        Arrangement.Vertical? verticalArrangement = null
    )
    {
        if (visualElementFactory == null) throw new ArgumentNullException(nameof(visualElementFactory));
        var factory = CustomColumnFactory;
        ComposeFunctions.ReusableComposeNode(
            nodeFactory: () => new UnityReusableComposeNode(visualElementFactory()),
            initializer: node => factory.Apply(node, horizontalAlignment ?? Alignment.Left, verticalArrangement ?? Arrangement.Top),
            modifier: modifier,
            content: content
        );
    }

    [Composable, NonRestartableComposable]
    public static void Row(
        Func<Row> visualElementFactory,
        [Composable, RowScope] Action content,
        IModifier? modifier = null,
        Arrangement.Horizontal? horizontalArrangement = null,
        Alignment.Vertical? verticalAlignment = null
    )
    {
        if (visualElementFactory == null) throw new ArgumentNullException(nameof(visualElementFactory));
        var factory = CustomRowFactory;
        ComposeFunctions.ReusableComposeNode(
            nodeFactory: () => new UnityReusableComposeNode(visualElementFactory()),
            initializer: node => factory.Apply(node, horizontalArrangement ?? Arrangement.Left, verticalAlignment ?? Alignment.Top),
            modifier: modifier,
            content: content
        );
    }

    [Composable, NonRestartableComposable]
    public static void Box(
        Func<Box> visualElementFactory,
        [Composable, BoxScope] Action content,
        IModifier? modifier = null,
        Alignment? contentAlignment = null,
        bool propagateMinConstraints = false
    )
    {
        if (visualElementFactory == null) throw new ArgumentNullException(nameof(visualElementFactory));
        var factory = CustomBoxFactory;
        ComposeFunctions.ReusableComposeNode(
            nodeFactory: () => new UnityReusableComposeNode(visualElementFactory()),
            initializer: node => factory.Apply(node, contentAlignment ?? Alignment.TopStart, propagateMinConstraints),
            modifier: modifier,
            content: content
        );
    }

    [Composable, NonRestartableComposable]
    public static void Box(Func<Box> visualElementFactory, IModifier? modifier = null)
    {
        if (visualElementFactory == null) throw new ArgumentNullException(nameof(visualElementFactory));
        var factory = CustomBoxFactory;
        ComposeFunctions.ReusableComposeNode(
            nodeFactory: () => new UnityReusableComposeNode(visualElementFactory()),
            initializer: node => factory.Apply(node, Alignment.TopStart, false),
            modifier: modifier
        );
    }

    [Composable, NonRestartableComposable]
    public static void Spacer(Func<VisualElement> visualElementFactory, IModifier modifier)
    {
        ReusableComposeNode(visualElementFactory, modifier);
    }
}
