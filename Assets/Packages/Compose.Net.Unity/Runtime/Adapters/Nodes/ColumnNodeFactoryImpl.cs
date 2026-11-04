using Compose.Net;
using Compose.Net.Unity.Runtime.Adapters.Extensions;

namespace Compose.Net.Unity.Runtime.Adapters.Nodes;

internal class ColumnNodeFactoryImpl : IColumnNodeFactory
{
    public IReusableComposeNode CreateNode() => new UnityReusableComposeNode(new Column());

    public void Apply(
        IReusableComposeNode node,
        Alignment.Horizontal horizontalAlignment,
        Arrangement.Vertical verticalArrangement
    )
    {
        var column = node.VisualElement<Column>();
        column.style.alignItems = horizontalAlignment.ToAlign();
        column.style.justifyContent = verticalArrangement.ToJustify();
        column.style.rowGap = verticalArrangement is Arrangement.Spaced spaced ? spaced.Space.ToPx(global::Compose.Net.Density.Default) : 0;
        ((UnityReusableComposeNode)node).SyncScrollContentLayout();
    }
}
