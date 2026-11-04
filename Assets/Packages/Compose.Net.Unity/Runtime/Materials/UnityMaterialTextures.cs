using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

/// <summary>Bitmap relief on atlas-safe tiled geometry, following ORBIT's texture rendering.</summary>
[global::Unity.Scripting.LifecycleManagement.NoAutoStaticsCleanup]
internal static class UnityMaterialTextures
{
    private static readonly Dictionary<string, Texture2D> Textures = new();
    private static readonly ConditionalWeakTable<VisualElement, Geometry> Meshes = new();

    internal static void Draw(IDrawScope<MeshGenerationContext> scope, SurfaceMaterial material, Shape shape, float opacity, float tileSize)
    {
        if (opacity <= 0 || material.TextureStrength <= 0) return;
        var name = material.Kind switch
        {
            SurfaceMaterialKind.Cloth => material.Weave switch
            {
                ClothWeave.Canvas => "canvas", ClothWeave.Felt => "felt", ClothWeave.Twill => "twill", _ => "linen"
            },
            SurfaceMaterialKind.Metal => "metal",
            _ => "paper"
        };
        if (!Textures.TryGetValue(name, out var texture)) Textures.Add(name, texture = Load(name));
        var geometry = Meshes.GetValue(scope.Context.visualElement, static _ => new Geometry());
        var radii = new Vector4(shape.TopLeft.ToPx(UnityDensity.Current), shape.TopRight.ToPx(UnityDensity.Current),
            shape.BottomRight.ToPx(UnityDensity.Current), shape.BottomLeft.ToPx(UnityDensity.Current));
        geometry.Update(scope.Size.Width, scope.Size.Height, radii, tileSize,
            material.Kind != SurfaceMaterialKind.Metal && !(material.Kind == SurfaceMaterialKind.Cloth && material.Weave == ClothWeave.Twill));
        if (geometry.Vertices.Count == 0) return;
        var strength = material.Kind switch
        {
            SurfaceMaterialKind.Matte => 0.12f, SurfaceMaterialKind.Ceramic => 0.06f,
            SurfaceMaterialKind.Metal => 0.45f, SurfaceMaterialKind.Paper => 0.5f, _ => 0.8f
        };
        var mesh = scope.Context.Allocate(geometry.Vertices.Count, geometry.Indices.Count, texture);
        var atlas = mesh.uvRegion;
        var tint = new UnityEngine.Color(1, 1, 1, opacity * material.TextureStrength * strength);
        foreach (var vertex in geometry.Vertices)
            mesh.SetNextVertex(new Vertex
            {
                position = new Vector3(vertex.Position.x, vertex.Position.y, Vertex.nearZ),
                uv = new Vector2(atlas.xMin + vertex.Uv.x * atlas.width, atlas.yMin + vertex.Uv.y * atlas.height),
                tint = tint
            });
        foreach (var index in geometry.Indices) mesh.SetNextIndex(index);
    }

    private static Texture2D Load(string name)
    {
        var source = Resources.Load<Texture2D>("ComposeMaterials/" + name)
            ?? throw new InvalidOperationException("Missing Compose material texture: " + name);
        var pixels = source.GetPixels32();
        double sum = 0;
        foreach (var p in pixels) sum += (p.r + p.g + p.b) / (3f * 255);
        var mean = (float)(sum / pixels.Length);
        for (var i = 0; i < pixels.Length; i++)
        {
            var p = pixels[i];
            var relief = Mathf.Clamp(((p.r + p.g + p.b) / (3f * 255) - mean) * 4, -1, 1);
            var value = relief > 0 ? (byte)255 : (byte)0;
            pixels[i] = new Color32(value, value, value, (byte)(Mathf.Abs(relief) * 255));
        }
        var texture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false)
        {
            name = "Compose " + name + " relief", hideFlags = HideFlags.HideAndDontSave,
            wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear
        };
        texture.SetPixels32(pixels); texture.Apply(false, true);
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
    private static void CleanupOnReload() => UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += Clear;
#endif

    private sealed class Geometry
    {
        internal readonly List<(Vector2 Position, Vector2 Uv)> Vertices = new();
        internal readonly List<ushort> Indices = new();
        private Vector3 _size;
        private Vector4 _radii;
        private bool _mirror;

        internal void Update(float width, float height, Vector4 radii, float tileSize, bool mirror)
        {
            if (!float.IsFinite(width) || !float.IsFinite(height) || width <= 0 || height <= 0)
            { Vertices.Clear(); Indices.Clear(); _size = default; return; }
            // Bound geometry for pathological scales/sizes, keeping well below ushort indices.
            var tile = Mathf.Max(Mathf.Max(8, tileSize), Mathf.Max(Mathf.Sqrt(width * height / 4096), Mathf.Max(width, height) / 1024));
            var size = new Vector3(width, height, tile);
            if (_size == size && _radii == radii && _mirror == mirror) return;
            _size = size; _radii = radii; _mirror = mirror; Vertices.Clear(); Indices.Clear();
            for (var i = 0; i < 4; i++) radii[i] = Mathf.Max(0, radii[i]);
            var factor = Mathf.Min(1, width / Mathf.Max(1, radii.x + radii.y), width / Mathf.Max(1, radii.w + radii.z),
                height / Mathf.Max(1, radii.x + radii.w), height / Mathf.Max(1, radii.y + radii.z));
            radii *= factor;
            var outline = new List<Vector2>();
            Corner(outline, new Vector2(width - radii.y, radii.y), radii.y, -90);
            Corner(outline, new Vector2(width - radii.z, height - radii.z), radii.z, 0);
            Corner(outline, new Vector2(radii.w, height - radii.w), radii.w, 90);
            Corner(outline, new Vector2(radii.x, radii.x), radii.x, 180);
            var rows = Mathf.CeilToInt(height / tile); var columns = Mathf.CeilToInt(width / tile);
            for (var row = 0; row < rows; row++)
            for (var col = 0; col < columns; col++)
            {
                var left = col * tile; var top = row * tile;
                var polygon = Clip(outline, 0, left, true);
                polygon = Clip(polygon, 0, left + tile, false);
                polygon = Clip(polygon, 1, top, true);
                polygon = Clip(polygon, 1, top + tile, false);
                if (polygon.Count < 3) continue;
                if (Vertices.Count + polygon.Count > 65535) throw new InvalidOperationException("Material texture geometry exceeds mesh limit");
                var start = (ushort)Vertices.Count;
                foreach (var p in polygon)
                {
                    var u = (p.x - left) / tile; var v = (p.y - top) / tile;
                    if (mirror && (col & 1) != 0) u = 1 - u;
                    if (mirror && (row & 1) != 0) v = 1 - v;
                    Vertices.Add((p, new Vector2(u, 1 - v)));
                }
                for (var i = 1; i < polygon.Count - 1; i++)
                { Indices.Add(start); Indices.Add((ushort)(start + i)); Indices.Add((ushort)(start + i + 1)); }
            }
        }

        private static void Corner(List<Vector2> outline, Vector2 center, float radius, float start)
        {
            if (radius < 0.01f) { outline.Add(center); return; }
            var steps = Mathf.Clamp(Mathf.CeilToInt(radius / 4), 4, 20);
            for (var step = 0; step <= steps; step++)
            {
                var angle = (start + step * 90f / steps) * Mathf.Deg2Rad;
                outline.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
        }

        private static List<Vector2> Clip(List<Vector2> input, int axis, float boundary, bool greater)
        {
            var output = new List<Vector2>();
            if (input.Count == 0) return output;
            var previous = input[input.Count - 1];
            var previousInside = greater ? previous[axis] >= boundary : previous[axis] <= boundary;
            foreach (var point in input)
            {
                var inside = greater ? point[axis] >= boundary : point[axis] <= boundary;
                if (inside != previousInside)
                    output.Add(Vector2.LerpUnclamped(previous, point, (boundary - previous[axis]) / (point[axis] - previous[axis])));
                if (inside) output.Add(point);
                previous = point; previousInside = inside;
            }
            return output;
        }
    }
}
