using System;
using System.Linq;
using Compose.Net;
using NUnit.Framework;
using StableCollections;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static Compose.Net.ComposeFunctions;
using Color = Compose.Net.Color;
using Object = UnityEngine.Object;

namespace Compose.Net.Unity.Tests;

public sealed partial class BoxTests
{
    private static Texture2D[] TransientTextures() => Resources.FindObjectsOfTypeAll<Texture2D>()
        .Where(t => !AssetDatabase.Contains(t)).ToArray();

    private void PaintGradientFrame()
    {
        E("box").MarkDirtyRepaint();
        Flush();
        var rt = E("box").CaptureToRenderTexture();
        rt.Release(); Object.DestroyImmediate(rt);
    }

    [Test]
    public void GradientResourcesStayBoundedAcrossRepaintsChangesAndDetach()
    {
        _emptyOverload = true;
        var existingTextures = TransientTextures().ToHashSet();
        var phase = MutableStateOf(0f);
        var brush = Brush.VerticalGradient(new[] { new Color(1, 0, 0), new Color(0, 0, 1) }.ToPersistentStableList());
        _box = Modifier.Size(160.Dp(), 100.Dp()).Draw(d =>
        {
            d.DrawRoundRect(brush, new Offset(8 + phase.Value, 8), new Size(100, 60), 12, alpha: .8f);
            d.DrawRoundRect(brush, new Offset(8, 8), new Size(100, 60), 12, style: Stroke(3));
        });
        Mount(); PaintGradientFrame(); PaintGradientFrame();
        var initial = TransientTextures().Length;
        for (var i = 0; i < 80; i++) PaintGradientFrame();
        Assert.That(TransientTextures().Length, Is.LessThanOrEqualTo(initial + 4), "Unchanged gradient repaints must reuse textures.");
        for (var i = 0; i < 80; i++) { phase.Value = i % 20; PaintGradientFrame(); }
        Assert.That(TransientTextures().Length, Is.LessThanOrEqualTo(initial + 6), "Animated geometry must release replaced textures.");
        var owned = TransientTextures().Where(t => !existingTextures.Contains(t) && t.width == 128 && t.height == 128).ToArray();
        _box = Modifier.Size(160.Dp(), 100.Dp()); Update(); PaintGradientFrame(); PaintGradientFrame();
        Assert.That(owned.Count(t => t), Is.Zero, "Removing the draw modifier must release its atlases.");
        _composer.Dispose(); Flush();
    }

    [Test]
    public void GradientCacheTracksMutableStopsColorsAndOpacity()
    {
        var colors = new[] { new Color(1, 0, 0), new Color(0, 0, 1) }.ToMutableStableList();
        var stops = new[] { 0f, 1f }.ToMutableStableList();
        var brush = _adapter.BrushFactory.LinearGradient(colors, stops, Offset.Zero, new Offset(120, 0), TileMode.Clamp);
        var opacity = MutableStateOf(1f);
        _emptyOverload = true;
        _box = Modifier.Size(120.Dp(), 100.Dp()).Draw(d => d.DrawRect(brush, alpha: opacity.Value));
        Mount(); PaintGradientFrame();
        UnityEngine.Color CenterPixel()
        {
            using var pixels = new CornerPixels(E("box"));
            return pixels.Read()[pixels.Height / 2 * pixels.Width + pixels.Width / 2];
        }
        Assert.That(CenterPixel().r, Is.GreaterThan(.2f));
        colors[0] = new Color(0, 1, 0); PaintGradientFrame();
        var changed = CenterPixel();
        Assert.That(changed.r, Is.LessThan(.02f));
        Assert.That(changed.g, Is.GreaterThan(.2f), "Mutating an existing color list must invalidate the cached image.");
        stops[1] = .25f; PaintGradientFrame();
        Assert.That(CenterPixel().b, Is.GreaterThan(.95f), "Mutating stop positions must invalidate the cached image.");
        opacity.Value = .25f; PaintGradientFrame();
        Assert.That(CenterPixel().a, Is.EqualTo(.25f).Within(.02f), "Draw-state opacity must not reuse stale gradient data.");
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void GradientPixelsMatchNativeFillAndStroke(bool radial, bool stroke)
    {
        var colors = new[] { new Color(1, 0, 0), new Color(0, 0, 1) }.ToPersistentStableList();
        var brush = radial ? Brush.RadialGradient(colors, new Offset(42, 32), 35, TileMode.Clamp)
            : Brush.LinearGradient(colors, new Offset(0, 0), new Offset(84, 64));
        _emptyOverload = true;
        _box = Modifier.Size(120.Dp(), 100.Dp()).Draw(d => d.DrawRect(brush,
            new Offset(13, 17), new Size(84, 64), .65f, stroke ? Stroke(5) : Fill));
        Mount(); PaintGradientFrame();
        var reference = new VisualElement();
        reference.style.position = Position.Absolute; reference.style.left = 0; reference.style.top = 0;
        reference.style.width = 120; reference.style.height = 100;
        rootVisualElement.Add(reference);
        reference.generateVisualContent = c =>
        {
            var p = c.painter2D;
            var g = new Gradient
            {
                colorKeys = new[] { new GradientColorKey(UnityEngine.Color.red, 0), new GradientColorKey(UnityEngine.Color.blue, radial ? .5f : 1) },
                alphaKeys = new[] { new GradientAlphaKey(.65f, 0), new GradientAlphaKey(.65f, radial ? .5f : 1) }
            };
            var fill = radial ? FillGradient.MakeRadialGradient(g, new Vector2(55, 49), 70, new Vector2(55, 49), AddressMode.Clamp)
                : FillGradient.MakeLinearGradient(g, new Vector2(13, 17), new Vector2(97, 81));
            if (stroke) { p.strokeFillGradient = fill; p.lineWidth = 5; } else p.fillGradient = fill;
            p.BeginPath(); p.MoveTo(new Vector2(13, 17)); p.LineTo(new Vector2(97, 17));
            p.LineTo(new Vector2(97, 81)); p.LineTo(new Vector2(13, 81)); p.ClosePath();
            if (stroke) p.Stroke(); else p.Fill();
        };
        Flush();
        using var expected = new CornerPixels(reference);
        using var actual = new CornerPixels(E("box"));
        var a = actual.Read(); var b = expected.Read();
        Assert.That(a.Length, Is.EqualTo(b.Length));
        var mean = 0f; var max = 0f;
        for (var i = 0; i < a.Length; i++)
        {
            var delta = Mathf.Max(Mathf.Abs(a[i].r - b[i].r), Mathf.Abs(a[i].g - b[i].g),
                Mathf.Abs(a[i].b - b[i].b), Mathf.Abs(a[i].a - b[i].a));
            mean += delta; max = Mathf.Max(max, delta);
        }
        Assert.That(mean / a.Length, Is.LessThan(.01f), "Gradient rendering must retain its coordinates, color and opacity.");
        Assert.That(max, Is.LessThan(.08f), "Gradient edges must match the native path.");
        reference.RemoveFromHierarchy();
    }
}
