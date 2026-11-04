using Compose.Net;
using Compose.Net.Unity.Runtime.Adapters.Extensions;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Runtime.Adapters.Nodes;

internal class RowNodeFactoryImpl : IRowNodeFactory
{
    public IReusableComposeNode CreateNode() => new UnityReusableComposeNode(new Row());

    public void Apply(
        IReusableComposeNode node,
        Arrangement.Horizontal horizontalArrangement,
        Alignment.Vertical verticalAlignment
    )
    {
        var row = node.VisualElement<Row>();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = (verticalAlignment ?? Alignment.Top).ToAlign();
        row.style.justifyContent = (horizontalArrangement ?? Arrangement.Left).ToJustify();
        row.style.columnGap = horizontalArrangement is Arrangement.Spaced spaced ? spaced.Space.ToPx(global::Compose.Net.Density.Default) : 0;
        ((UnityReusableComposeNode)node).SyncScrollContentLayout();
    }
}
