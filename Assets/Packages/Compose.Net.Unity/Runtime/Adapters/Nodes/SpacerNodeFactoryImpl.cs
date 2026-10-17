using Compose.Net;

namespace Compose.Net.Unity.Runtime.Adapters.Nodes;

internal class SpacerNodeFactoryImpl : ISpacerNodeFactory
{
    public IReusableComposeNode CreateNode(string? type = null)
    {
        // No typed spacers yet, so the type only names the node in the UI Toolkit debugger.
        return new UnityReusableComposeNode(new Spacer { name = type });
    }
}