using System;
using System.Collections.Generic;
using Compose.Net;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

// Independent shape-based shadow layers. No filter captures of text/children and
// no shared overlay whose color or clip can be overwritten by the next shadow.
internal sealed class ShapeShadowPainter(Shape shape, Compose.Net.Shadow shadow, bool inset)
{
    private readonly List<Vertex> _vertices = new();
    private readonly List<ushort> _indices = new();
    private Vector3 _key;
    private bool _valid;
    private const int Corners = 16;
    private const int Points = Corners * 4;

    internal void Draw(MeshGenerationContext context)
    {
        var size = context.visualElement.layout.size;
        if (size.x <= 0 || size.y <= 0 || !float.IsFinite(size.x) || !float.IsFinite(size.y) || shadow.Color.Alpha <= 0) return;
        var density = UnityDensity.Current;
        var key = new Vector3(size.x, size.y, density.Density);
        if (!_valid || _key != key) { _key = key; _valid = true; Build(size); }
        if (_vertices.Count == 0) return;
        var mesh = context.Allocate(_vertices.Count, _indices.Count);
        foreach (var vertex in _vertices) mesh.SetNextVertex(vertex);
        foreach (var index in _indices) mesh.SetNextIndex(index);
    }

    private void Build(Vector2 size)
    {
        _vertices.Clear(); _indices.Clear();
        var density = UnityDensity.Current;
        var blur = Mathf.Max(0, shadow.Radius.ToPx(density));
        var spread = shadow.Spread.ToPx(density);
        var sigma = Mathf.Max(0.25f, blur * 0.5f);
        var radii = RoundedCorners.Resolve(shape, size);
        var maxInset = Mathf.Min(size.x, size.y) * 0.5f - 0.01f;
        var reach = inset ? Mathf.Min(maxInset, Mathf.Max(1, spread + 4 * sigma + Mathf.Max(Mathf.Abs(shadow.Offset.X), Mathf.Abs(shadow.Offset.Y)))) : Mathf.Max(1, spread + 4 * sigma + Mathf.Max(Mathf.Abs(shadow.Offset.X), Mathf.Abs(shadow.Offset.Y)));
        const int rings = 18;
        for (var ring = 0; ring <= rings; ring++)
        {
            var expansion = inset ? -reach * ring / rings : reach * (1 - (float)ring / rings);
            expansion = Mathf.Max(-maxInset, expansion);
            var left = -expansion; var top = -expansion;
            var right = size.x + expansion; var bottom = size.y + expansion;
            var cap = Mathf.Max(0, Mathf.Min(right - left, bottom - top) / 2);
            for (var corner = 0; corner < 4; corner++)
            {
                var radius = Mathf.Clamp((corner == 0 ? radii.y : corner == 1 ? radii.w : corner == 2 ? radii.z : radii.x) + expansion, 0, cap);
                var center = new Vector2(corner < 2 ? right - radius : left + radius,
                    corner == 0 || corner == 3 ? top + radius : bottom - radius);
                for (var i = 0; i < Corners; i++)
                {
                    var angle = (-90 + corner * 90 + 90f * i / (Corners - 1)) * Mathf.Deg2Rad;
                    var point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    var alpha = inset
                        ? Falloff(InsideDistance(point - new Vector2(shadow.Offset.X, shadow.Offset.Y), size, radii) - spread, sigma)
                        : Falloff(-InsideDistance(point - new Vector2(shadow.Offset.X, shadow.Offset.Y), size, radii) - spread, sigma);
                    // Antialias the clipping edge geometrically, not by clipping the host or its children.
                    if (inset && ring == 0) alpha = 0;
                    if (!inset && ring == 0) alpha = 0;

                    Add(point, alpha);
                }
            }
            if (ring == 0) continue;
            var outer = (ring - 1) * Points; var inner = ring * Points;
            for (var i = 0; i < Points; i++)
            {
                var next = (i + 1) % Points;
                Triangle(outer + i, outer + next, inner + i);
                Triangle(inner + i, outer + next, inner + next);
            }
        }
        // Outer shadows are cut out at the unshifted shape boundary, so native
        // backgrounds and translucent fills are not darkened by their own shadow.
        if (!inset) return;
        // Complete the center when inset spread covers the entire surface.
        var middle = size * 0.5f;
        var centerAlpha = inset ? Falloff(InsideDistance(middle - new Vector2(shadow.Offset.X, shadow.Offset.Y), size, radii) - spread, sigma) : 1f;
        if (!inset) middle += new Vector2(shadow.Offset.X, shadow.Offset.Y);
        var centerIndex = _vertices.Count; Add(middle, centerAlpha);
        for (var i = 0; i < Points; i++) Triangle(rings * Points + i, rings * Points + (i + 1) % Points, centerIndex);
    }

    private void Add(Vector2 point, float alpha) => _vertices.Add(new Vertex
    {
        position = new Vector3(point.x, point.y, Vertex.nearZ),
        tint = new UnityEngine.Color(shadow.Color.Red, shadow.Color.Green, shadow.Color.Blue, shadow.Color.Alpha * alpha)
    });
    private void Triangle(int a, int b, int c) { _indices.Add((ushort)a); _indices.Add((ushort)b); _indices.Add((ushort)c); }
    private static float Falloff(float distance, float sigma) => 1f / (1f + Mathf.Exp(Mathf.Clamp(distance / sigma * 1.7f, -20, 20)));
    private static float InsideDistance(Vector2 p, Vector2 size, Vector4 radii)
    {
        var distance = Mathf.Min(p.x, size.x - p.x, p.y, size.y - p.y);
        if (p.x < radii.x && p.y < radii.x) return radii.x - Vector2.Distance(p, new Vector2(radii.x, radii.x));
        if (p.x > size.x - radii.y && p.y < radii.y) return radii.y - Vector2.Distance(p, new Vector2(size.x - radii.y, radii.y));
        if (p.x < radii.z && p.y > size.y - radii.z) return radii.z - Vector2.Distance(p, new Vector2(radii.z, size.y - radii.z));
        if (p.x > size.x - radii.w && p.y > size.y - radii.w) return radii.w - Vector2.Distance(p, new Vector2(size.x - radii.w, size.y - radii.w));
        return distance;
    }
}
