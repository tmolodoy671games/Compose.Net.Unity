// ReSharper disable CheckNamespace

using System;
using ComposeColor = Compose.Net.Color;
using UnityColor = UnityEngine.Color;

namespace Compose.Net.Unity;

public static class ColorExtensions
{
    public static ComposeColor ToComposeColor(this UnityColor color)
    {
        return new ComposeColor(
            Red: color.r,
            Green: color.g,
            Blue: color.b,
            Alpha: color.a
        );
    }
    
    public static UnityColor ToUnityColor(this ComposeColor color)
    {
        return new UnityColor(
            r: color.Red,
            g: color.Green,
            b: color.Blue,
            a: color.Alpha
        );
    }
}