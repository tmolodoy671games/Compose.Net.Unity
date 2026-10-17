using Compose.Net;

namespace Compose.Net.Unity.Runtime.Extensions;

internal static class StringExtensions
{
    public static PointerType ToPointerType(this string pointerType)
    {
        return pointerType switch
        {
            "mouse" => PointerType.Mouse,
            "touch" => PointerType.Touch,
            "pen" => PointerType.Stylus,
            _ => PointerType.Unknown
        };
    }
}