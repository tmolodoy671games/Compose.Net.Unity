#if COMPOSE_UNITY_VECTOR_GRAPHICS && UNITY_6000_3_OR_NEWER
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using global::Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Runtime.Adapters.Nodes;

internal sealed class ComposeImageElement : Image
{
    private VectorImageKey? _key;
    private VectorImage? _ownedImage;

    internal ComposeImageElement()
    {
        RegisterCallback<AttachToPanelEvent>(_ => AttachVector());
        RegisterCallback<DetachFromPanelEvent>(_ => ReleaseVector());
    }

    internal void SetVector(ImageVector vector, Size intrinsicSize, Color tint)
    {
        var key = new VectorImageKey(vector, intrinsicSize, tint.IsSpecified);
        if (_key != key)
        {
            // Acquire before releasing the current image so a failed build leaves it usable.
            var next = panel != null ? VectorImageCache.Acquire(key) : null;
            ReleaseVector();
            _key = key;
            _ownedImage = next;
            vectorImage = next;
        }
        tintColor = tint.IsSpecified ? tint.ToUnityColor() : UnityEngine.Color.white;
    }

    internal void ClearVector()
    {
        if (_key == null) return;
        ReleaseVector();
        _key = null;
        tintColor = UnityEngine.Color.white;
    }

    private void AttachVector()
    {
        if (_key is not { } key || _ownedImage != null) return;
        _ownedImage = VectorImageCache.Acquire(key);
        vectorImage = _ownedImage;
    }

    private void ReleaseVector()
    {
        if (_ownedImage == null || _key is not { } key) return;
        vectorImage = null;
        _ownedImage = null;
        VectorImageCache.Release(key);
    }
}

internal readonly record struct VectorImageKey(ImageVector Vector, Size IntrinsicSize, bool Monochrome);

#if UNITY_7000_0_OR_NEWER
[global::Unity.Scripting.LifecycleManagement.NoAutoStaticsCleanup]
#endif
internal static class VectorImageCache
{
    private static readonly Dictionary<VectorImageKey, Entry> Images = new();

    internal static VectorImage Acquire(VectorImageKey key)
    {
        if (!Images.TryGetValue(key, out var entry))
        {
            entry = new Entry(VectorImageRenderer.Create(key));
            Images.Add(key, entry);
        }
        entry.Users++;
        return entry.Image;
    }

    internal static void Release(VectorImageKey key)
    {
        var entry = Images[key];
        if (--entry.Users != 0) return;
        Images.Remove(key);
        if (Application.isPlaying) UnityEngine.Object.Destroy(entry.Image);
        else UnityEngine.Object.DestroyImmediate(entry.Image);
    }

    private sealed class Entry(VectorImage image)
    {
        internal readonly VectorImage Image = image;
        internal int Users;
    }
}

internal static class VectorImageRenderer
{
    private static XNamespace Svg => "http://www.w3.org/2000/svg";

    internal static VectorImage Create(VectorImageKey key)
    {
        var vector = key.Vector;
        var size = key.IntrinsicSize;
        var definitions = new XElement(Svg + "defs");
        var content = new XElement(Svg + "g",
            new XAttribute("transform", FormattableString.Invariant(
                $"scale({size.Width / vector.ViewportWidth:R} {size.Height / vector.ViewportHeight:R})")));
        var clipCount = 0;
        WriteChildren(vector.Root, content, definitions, key.Monochrome, ref clipCount);
        var svg = new XElement(Svg + "svg", new XAttribute("width", size.Width),
            new XAttribute("height", size.Height), definitions, content);
        using var reader = new StringReader(svg.ToString(SaveOptions.DisableFormatting));
        var scene = SVGParser.ImportSVG(reader, clipViewport: true);
        var image = VectorUtils.BuildVectorImage(scene, new UnityEngine.Rect(0f, 0f, size.Width, size.Height));
        image.name = vector.Name;
        image.hideFlags = HideFlags.HideAndDontSave;
        return image;
    }

    private static void WriteChildren(VectorGroup group, XElement parent, XElement definitions,
        bool monochrome, ref int clipCount)
    {
        foreach (var node in group.Children)
        {
            if (node is VectorPath path)
            {
                if (!string.IsNullOrWhiteSpace(path.PathData)) parent.Add(WritePath(path, monochrome));
                continue;
            }
            if (node is not VectorGroup child) continue;

            var radians = child.Rotate * Mathf.Deg2Rad;
            var a = Mathf.Cos(radians) * child.ScaleX;
            var b = Mathf.Sin(radians) * child.ScaleX;
            var c = -Mathf.Sin(radians) * child.ScaleY;
            var d = Mathf.Cos(radians) * child.ScaleY;
            var x = child.TranslationX + child.PivotX - a * child.PivotX - c * child.PivotY;
            var y = child.TranslationY + child.PivotY - b * child.PivotX - d * child.PivotY;
            var element = new XElement(Svg + "g", new XAttribute("transform",
                FormattableString.Invariant($"matrix({a:R} {b:R} {c:R} {d:R} {x:R} {y:R})")));
            parent.Add(element);

            if (!string.IsNullOrWhiteSpace(child.ClipPathData))
            {
                var id = "clip" + (++clipCount).ToString(CultureInfo.InvariantCulture);
                definitions.Add(new XElement(Svg + "clipPath", new XAttribute("id", id),
                    new XElement(Svg + "path", new XAttribute("d", child.ClipPathData))));
                var clipped = new XElement(Svg + "g", new XAttribute("clip-path", $"url(#{id})"));
                element.Add(clipped);
                element = clipped;
            }

            WriteChildren(child, element, definitions, monochrome, ref clipCount);
        }
    }

    private static XElement WritePath(VectorPath path, bool monochrome)
    {
        var element = new XElement(Svg + "path", new XAttribute("d", path.PathData),
            new XAttribute("fill-rule", path.PathFillType == PathFillType.EvenOdd ? "evenodd" : "nonzero"));
        WriteColor(element, "fill", path.Fill, path.FillAlpha, monochrome);
        WriteColor(element, "stroke", path.Stroke, path.StrokeAlpha, monochrome);
        if (path.Stroke.IsSpecified)
        {
            element.Add(new XAttribute("stroke-width", path.StrokeLineWidth));
            element.Add(new XAttribute("stroke-linecap", path.StrokeLineCap.ToString().ToLowerInvariant()));
            element.Add(new XAttribute("stroke-linejoin", path.StrokeLineJoin.ToString().ToLowerInvariant()));
            element.Add(new XAttribute("stroke-miterlimit", path.StrokeLineMiter));
        }
        return element;
    }

    private static void WriteColor(XElement element, string attribute, Color color, float alpha, bool monochrome)
    {
        element.SetAttributeValue(attribute, color.IsUnspecified
            ? "none"
            : monochrome ? "#ffffff" : "#" + ColorUtility.ToHtmlStringRGB(color.ToUnityColor()));
        if (color.IsSpecified)
            element.SetAttributeValue(attribute + "-opacity", Mathf.Clamp01(color.Alpha * alpha));
    }
}
#endif
