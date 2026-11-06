using System;

// ReSharper disable CheckNamespace

namespace Compose.Net.Unity;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
[AiGenerated]
public sealed class UnityPreview(
    string name = "",
    int width = -1,
    int height = -1
) : Attribute
{
    public string Name { get; set; } = name;
    /// <summary>A positive fixed width, or -1/0 to follow the preview window.</summary>
    public int Width { get; set; } = width;
    /// <summary>A positive fixed height, or -1/0 to follow the preview window.</summary>
    public int Height { get; set; } = height;
}
