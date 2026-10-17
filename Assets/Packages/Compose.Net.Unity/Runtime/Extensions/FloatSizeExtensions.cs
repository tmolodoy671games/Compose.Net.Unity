// ReSharper disable CheckNamespace

using Compose.Net;
using UnityEngine;

namespace Compose.Net.Unity;

public static class FloatSizeExtensions
{
    public static Vector2 ToVector2(this Size floatSize) => new(floatSize.Width, floatSize.Height);
}