// ReSharper disable CheckNamespace

using Compose.Net;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

public static class DpExtensions
{
    public static Length ToLength(this Dp dp)
    {
        return dp.Value;
    }
}