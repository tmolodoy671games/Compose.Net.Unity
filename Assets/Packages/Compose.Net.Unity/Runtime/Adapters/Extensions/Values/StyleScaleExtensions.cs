using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Runtime.Adapters.Extensions.Values;

internal static class StyleScaleExtensions
{
    public static StyleScale CompareAndSetNull(this StyleScale value, StyleScale compareTo)
    {
        return value == compareTo ? StyleKeyword.Null : value;
    }
    public static Vector2 ToVector2(this StyleScale value)
    {
        return new Vector2(value.value.value.x, value.value.value.y);
    }
}