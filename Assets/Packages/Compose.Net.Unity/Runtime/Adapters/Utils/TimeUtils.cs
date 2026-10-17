using Compose.Net.Unity.Packages.Compose.Net.Unity.Runtime.Adapters;

namespace Compose.Net.Unity.Runtime.Adapters.Utils;

internal static class TimeUtils
{
    public static long ToLongTime(this float time)
    {
        return (long)(time * 1000);
    }

    public static long Frametime => 1000 / ComposeUnityAdapter.Instance.Framerate();
}