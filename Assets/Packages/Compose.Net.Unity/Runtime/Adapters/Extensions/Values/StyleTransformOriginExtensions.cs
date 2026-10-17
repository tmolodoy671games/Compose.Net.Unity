using System;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Runtime.Adapters.Extensions.Values;

internal static class StyleTransformOriginExtensions
{
    public static StyleTransformOrigin CompareAndSetNull(
        this StyleTransformOrigin value,
        StyleTransformOrigin compareTo
    )
    {
        return value == compareTo ? StyleKeyword.Null : value;
    }
}