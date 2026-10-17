using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Runtime.Adapters.Extensions.Values;

internal static class StyleTranslateExtensions
{
    public static StyleTranslate CompareAndSetNull(this StyleTranslate value, StyleTranslate compareTo)
    {
        return value == compareTo ? StyleKeyword.Null : value;
    }

    public static StyleTranslate Add(this StyleTranslate value, Translate addition)
    {
        StyleTranslate result = new Vector2(value.value.x.value, value.value.y.value) +
                                new Vector2(addition.x.value, addition.y.value);
        if (result.value == new Translate(0, 0))
            result = StyleKeyword.Null;
        return result;
    }

    public static StyleTranslate Subtract(this StyleTranslate value, Translate addition)
    {
        StyleTranslate result = new Vector2(value.value.x.value, value.value.y.value) -
                                new Vector2(addition.x.value, addition.y.value);
        if (result.value == new Translate(0, 0))
            result = StyleKeyword.Null;
        return result;
    }

    public static Vector2 ToVector2(this StyleTranslate value)
    {
        return new Vector2(value.value.x.value, value.value.y.value);
    }
}