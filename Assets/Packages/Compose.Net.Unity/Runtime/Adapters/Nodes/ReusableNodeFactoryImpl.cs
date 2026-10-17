using Compose.Net;

namespace Compose.Net.Unity.Runtime.Adapters.Nodes;

internal class ReusableNodeFactoryImpl : IReusableNodeFactory
{
    public IColumnNodeFactory Column { get; } = new ColumnNodeFactoryImpl();
    public IRowNodeFactory Row { get; } = new RowNodeFactoryImpl();
    public IBoxNodeFactory Box { get; } = new BoxNodeFactoryImpl();
    public ISpacerNodeFactory Spacer { get; } = new SpacerNodeFactoryImpl();
    public IImageNodeFactory Image { get; } = new ImageNodeFactoryImpl();
    public ITextNodeFactory Text { get; } = new TextNodeFactoryImpl();
}