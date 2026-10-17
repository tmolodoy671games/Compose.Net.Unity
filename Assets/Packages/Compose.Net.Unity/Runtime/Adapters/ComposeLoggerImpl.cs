using Compose.Net;

namespace Compose.Net.Unity.Runtime.Adapters;

internal class ComposeLoggerImpl : IComposeLogger
{
    public void Debug(object? message)
    {
        UnityEngine.Debug.Log(message);
    }
}