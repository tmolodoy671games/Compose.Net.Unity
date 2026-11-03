using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Compose.Net;
using Compose.Net.Showcase;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Showcase;

public sealed partial class OrbitUnityHost : IOrbitMaterialHost
{
    public void DrawMaterialTexture(IDrawScope scope, OrbitMaterialTexture material, float radius, float opacity, float tileSize, Compose.Net.Color tint)
        => OrbitMaterialTextures.Draw(scope, material, radius, opacity, tileSize, tint);
}

internal static class OrbitMaterialTextures
{
    private static readonly Dictionary<OrbitMaterialTexture, Texture2D> Textures = new();
    private static readonly ConditionalWeakTable<VisualElement, TextureGeometry> Geometry = new();

    internal static void Draw(IDrawScope scope, OrbitMaterialTexture material, float radius, float opacity, float tileSize, Compose.Net.Color color)
    {
        if (opacity <= 0 || scope is not IDrawScope<MeshGenerationContext> native) return;
        if (!Textures.TryGetValue(material, out var texture))
        {
            texture = LoadRelief(material);
            Textures.Add(material, texture);
        }
        var geometry = Geometry.GetValue(native.Context.visualElement, static _ => new TextureGeometry());
        geometry.Update(scope.Size.Width, scope.Size.Height, radius, tileSize);
        if (geometry.Vertices.Count == 0) return;
        var mesh = native.Context.Allocate(geometry.Vertices.Count, geometry.Indices.Count, texture);
        var uv = mesh.uvRegion;
        var tint = new UnityEngine.Color(color.Red, color.Green, color.Blue, Mathf.Clamp01(opacity) * color.Alpha);
        foreach (var vertex in geometry.Vertices)
        {
            mesh.SetNextVertex(new Vertex
            {
                position = new Vector3(vertex.Position.x, vertex.Position.y, Vertex.nearZ),
                uv = new Vector2(uv.xMin + vertex.Uv.x * uv.width, uv.yMin + vertex.Uv.y * uv.height),
                tint = tint
            });
        }
        foreach (var index in geometry.Indices) mesh.SetNextIndex(index);
    }

    private static Texture2D LoadRelief(OrbitMaterialTexture material)
    {
        var name = material switch
        {
            OrbitMaterialTexture.Linen => "linen",
            OrbitMaterialTexture.Paper => "paper",
            _ => "metal"
        };
        var source = Resources.Load<Texture2D>("OrbitMaterials/" + name)
            ?? throw new InvalidOperationException("Missing ORBIT material: " + name);
        var pixels = source.GetPixels32();
        double total = 0;
        foreach (var pixel in pixels) total += (pixel.r + pixel.g + pixel.b) / (3f * 255);
        var mean = (float)(total / pixels.Length);
        for (var i = 0; i < pixels.Length; i++)
        {
            var pixel = pixels[i];
            var relief = Mathf.Clamp(((pixel.r + pixel.g + pixel.b) / (3f * 255) - mean) * 5, -1, 1);
            // The bitmap supplies alpha only. The vertex tint supplies a lighter
            // or darker version of the actual surface color, never a grey wash.
            pixels[i] = new Color32(255, 255, 255, (byte)(Mathf.Abs(relief) * 255));
        }
        var texture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false)
        {
            name = "ORBIT " + name + " relief",
            hideFlags = HideFlags.HideAndDontSave,
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear
        };
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return texture;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Clear()
    {
        foreach (var texture in Textures.Values)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(texture);
            else UnityEngine.Object.DestroyImmediate(texture);
        }
        Textures.Clear();
    }

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    private static void RegisterCleanup() => UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += Clear;
#endif

    private sealed class TextureGeometry
    {
        internal readonly List<(Vector2 Position, Vector2 Uv)> Vertices = new();
        internal readonly List<ushort> Indices = new();
        private Vector4 _key;

        internal void Update(float width, float height, float radius, float tile)
        {
            var key = new Vector4(width, height, radius, tile);
            if (_key == key) return;
            _key = key; Vertices.Clear(); Indices.Clear();
            if (width <= 0 || height <= 0) return;
            var r = Mathf.Clamp(radius, 0, Mathf.Min(width, height) / 2);
            var outline = new List<Vector2>();
            if (r < .1f)
                outline.AddRange(new[] { new Vector2(0, 0), new Vector2(width, 0), new Vector2(width, height), new Vector2(0, height) });
            else
            {
                var centers = new[] { new Vector2(width - r, r), new Vector2(width - r, height - r), new Vector2(r, height - r), new Vector2(r, r) };
                var steps = Mathf.Clamp(Mathf.CeilToInt(r / 8), 4, 16);
                for (var corner = 0; corner < 4; corner++)
                    for (var step = 0; step <= steps; step++)
                    {
                        var angle = (-90 + corner * 90 + step * 90f / steps) * Mathf.Deg2Rad;
                        outline.Add(centers[corner] + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r);
                    }
            }
            // Each tile has its own 0..1 UVs. This remains correct when Unity
            // packs the texture into a dynamic atlas and at every window size.
            for (var top = 0f; top < height; top += tile)
                for (var left = 0f; left < width; left += tile)
                {
                    var polygon = Clip(outline, 0, left, true);
                    polygon = Clip(polygon, 0, left + tile, false);
                    polygon = Clip(polygon, 1, top, true);
                    polygon = Clip(polygon, 1, top + tile, false);
                    if (polygon.Count < 3) continue;
                    var start = (ushort)Vertices.Count;
                    foreach (var p in polygon)
                    {
                        var u = (p.x - left) / tile; var v = (p.y - top) / tile;
                        // Mirrored neighbors meet on the exact same source edge,
                        // hiding seams without altering the original bitmap.
                        if ((Mathf.RoundToInt(left / tile) & 1) != 0) u = 1 - u;
                        if ((Mathf.RoundToInt(top / tile) & 1) != 0) v = 1 - v;
                        Vertices.Add((p, new Vector2(u, 1 - v)));
                    }
                    for (var i = 1; i < polygon.Count - 1; i++)
                    { Indices.Add(start); Indices.Add((ushort)(start + i)); Indices.Add((ushort)(start + i + 1)); }
                }
        }

        private static List<Vector2> Clip(List<Vector2> input, int axis, float boundary, bool keepGreater)
        {
            var output = new List<Vector2>();
            if (input.Count == 0) return output;
            var previous = input[input.Count - 1];
            var previousInside = keepGreater ? previous[axis] >= boundary : previous[axis] <= boundary;
            foreach (var point in input)
            {
                var inside = keepGreater ? point[axis] >= boundary : point[axis] <= boundary;
                if (inside != previousInside)
                    output.Add(Vector2.LerpUnclamped(previous, point, (boundary - previous[axis]) / (point[axis] - previous[axis])));
                if (inside) output.Add(point);
                previous = point; previousInside = inside;
            }
            return output;
        }
    }
}
