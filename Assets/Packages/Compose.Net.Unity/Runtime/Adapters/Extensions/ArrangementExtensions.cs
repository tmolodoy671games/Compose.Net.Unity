using Compose.Net;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Runtime.Adapters.Extensions;

internal static class ArrangementExtensions
{
    public static Justify ToJustify(this Arrangement arrangement)
    {
        if (arrangement == Arrangement.Top)
            return Justify.FlexStart;
        if (arrangement == Arrangement.Center)
            return Justify.Center;
        if (arrangement == Arrangement.Bottom)
            return Justify.FlexEnd;
        if (arrangement == Arrangement.Left)
            return Justify.FlexStart;
        if (arrangement == Arrangement.Right)
            return Justify.FlexEnd;
        return Justify.FlexStart;
    }
}