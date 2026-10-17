// ReSharper disable CheckNamespace

using System;
using System.Runtime.CompilerServices;
using Compose.Net;

namespace Compose.Net.Unity;

internal static partial class FontStyleUtils
{
    public static UnityEngine.FontStyle ToUnityFontStyle(FontStyle fontStyle, FontWeight fontWeight)
    {
        if (fontStyle == FontStyle.Italic && fontWeight == FontWeight.Bold)
            return UnityEngine.FontStyle.BoldAndItalic;
        if (fontStyle == FontStyle.Normal && fontWeight == FontWeight.Bold)
            return UnityEngine.FontStyle.Bold;
        if (fontStyle == FontStyle.Italic && fontWeight == FontWeight.Normal)
            return UnityEngine.FontStyle.Italic;
        return UnityEngine.FontStyle.Normal;
    }
}