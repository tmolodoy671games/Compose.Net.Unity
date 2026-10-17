using Compose.Net;
using Compose.Net.Unity.Runtime.Adapters.Extensions;

namespace Compose.Net.Unity.Runtime.Adapters.Nodes;

internal class ColumnNodeFactoryImpl : IColumnNodeFactory
{
    public IReusableComposeNode CreateNode(string? type = null)
    {
        return new UnityReusableComposeNode(CreateColumn(type));
    }

    // Typed nodes only differ by class, so the UI Toolkit debugger shows a readable name.
    private static Column CreateColumn(string? type) => type switch
    {
        null => new Column(),
        NodeTypes.VerticalPagerContent => new VerticalPagerContent(),
        _ => new Column { name = type },
    };

    public void Apply(
        IReusableComposeNode node,
        Alignment.Horizontal horizontalAlignment,
        Arrangement.Vertical verticalArrangement
    )
    {
        var column = node.VisualElement<Column>();
        column.style.alignItems = horizontalAlignment.ToAlign();
        column.style.justifyContent = verticalArrangement.ToJustify();
    }
}

internal class VerticalPagerContent : Column
{
}