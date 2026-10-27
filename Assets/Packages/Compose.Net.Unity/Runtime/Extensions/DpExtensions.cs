// ReSharper disable CheckNamespace

using Compose.Net;
using Compose.Net.Unity.Packages.Compose.Net.Unity.Runtime.Adapters;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal static class UnityDensity
{
    public static IDensity Current => ComposeUnityAdapter.Instance.Density;
}

public static class DpExtensions
{
    public static Length ToLength(this Dp dp, IDensity density)
    {
        return density.ToPx(dp);
    }
}
