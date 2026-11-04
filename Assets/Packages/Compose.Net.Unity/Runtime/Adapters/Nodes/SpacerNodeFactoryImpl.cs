using Compose.Net;

namespace Compose.Net.Unity.Runtime.Adapters.Nodes;

internal class SpacerNodeFactoryImpl : ISpacerNodeFactory
{
    public IReusableComposeNode CreateNode() => new UnityReusableComposeNode(new Spacer());
}
