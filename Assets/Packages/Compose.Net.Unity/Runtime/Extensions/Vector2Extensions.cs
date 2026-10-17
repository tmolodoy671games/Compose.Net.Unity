// ReSharper disable CheckNamespace

using Compose.Net;
using UnityEngine;

namespace Compose.Net.Unity;

public static class Vector2Extensions
{
    public static Offset ToOffset(this Vector2 vector2) => new(vector2.x, vector2.y);
    public static Offset ToOffset(this Vector3 vector2) => new(vector2.x, vector2.y);
    public static Vector2 ToVector2(this Vector3 vector2) => new(vector2.x, vector2.y);
    
    public static Size ToSize(this Vector2 vector2) => new(vector2.x, vector2.y);
}