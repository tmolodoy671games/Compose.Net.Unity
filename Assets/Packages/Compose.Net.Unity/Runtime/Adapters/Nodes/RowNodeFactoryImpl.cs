using Compose.Net;
using Compose.Net.Unity.Runtime.Adapters.Extensions;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Runtime.Adapters.Nodes;

internal class RowNodeFactoryImpl : IRowNodeFactory
{
    public IReusableComposeNode CreateNode(string? type = null)
    {
        return new UnityReusableComposeNode(CreateRow(type));
    }

    // Typed nodes only differ by class, so the UI Toolkit debugger shows a readable name.
    private static Row CreateRow(string? type) => type switch
    {
        null => new Row(),
        NodeTypes.HorizontalPagerContent => new HorizontalPagerContent(),
        _ => new Row { name = type },
    };

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
    }
}

internal class HorizontalPagerContent : Row
{
}