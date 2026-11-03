using System;
using System.Collections;
using System.Linq;
using Compose.Net;
using NUnit.Framework;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static Compose.Net.ComposeFunctions;
using Color = Compose.Net.Color;

namespace Compose.Net.Unity.Tests;

public sealed partial class BoxTests
{
    private sealed class CornerPixels : IDisposable
    {
        private readonly Texture2D _texture;
        internal readonly int Width, Height;
        internal CornerPixels(VisualElement element)
        {
            var render = element.CaptureToRenderTexture();
            var previous = RenderTexture.active;
            Width = render.width; Height = render.height;
            _texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            try { RenderTexture.active = render; _texture.ReadPixels(new UnityEngine.Rect(0, 0, Width, Height), 0, 0); _texture.Apply(); }
            finally { RenderTexture.active = previous; render.Release(); UnityEngine.Object.DestroyImmediate(render); }
        }
        internal UnityEngine.Color[] Read() => _texture.GetPixels();
        internal float Alpha(float x, float y) => _texture.GetPixel(Mathf.Clamp((int)x, 0, Width - 1), Mathf.Clamp(Height - 1 - (int)y, 0, Height - 1)).a;
        public void Dispose() => UnityEngine.Object.DestroyImmediate(_texture);
    }

    [UnityTest] public IEnumerator RoundedCorners_OversizedUniformRadiiMatchCircularCaps()
    {
        _emptyOverload = true; Mount();
        foreach (var size in new[] { new Vector2(200, 80), new Vector2(80, 200), new Vector2(80, 80) })
        foreach (var mode in new[] { "color", "brush", "clip", "draw", "border", "inner-shadow" })
        {
            UnityEngine.Color[]? reference = null;
            foreach (var radius in new[] { 40f, 100f, 1000f })
            {
                var shape = RoundedCornerShape(radius.Dp());
                var modifier = Modifier.Size(size.x.Dp(), size.y.Dp());
                _box = mode switch
                {
                    "color" => modifier.Background(Color.White, shape),
                    "brush" => modifier.Background(SolidColor(Color.White), shape),
                    "clip" => modifier.Clip(shape).Background(Color.White),
                    "draw" => modifier.DrawBehind(scope => scope.DrawRoundRect(Color.White, cornerRadius: radius)),
                    "inner-shadow" => modifier.Background(Color.White, shape).InnerShadow(shape, new Compose.Net.Shadow(0.Dp(), 2.Dp(), Color.Black, new Offset(0, 0))),
                    _ => modifier.Border(3.Dp(), SolidColor(Color.White), shape)
                };
                Update(); yield return null; Flush();
                using var pixels = new CornerPixels(E("box"));
                var actual = pixels.Read();
                Assert.That(pixels.Alpha(2, 2), Is.LessThan(.1f), mode + " must round its corner");
                Assert.That(pixels.Alpha(40, 1), Is.GreaterThan(.8f), mode + " must reach the straight top at r=40");
                if (reference != null)
                {
                    Assert.That(actual.Length, Is.EqualTo(reference.Length));
                    var maxError = 0f;
                    for (var i = 0; i < actual.Length; i++)
                    {
                        maxError = Mathf.Max(maxError, Mathf.Abs(actual[i].a - reference[i].a));
                        maxError = Mathf.Max(maxError, Mathf.Abs(actual[i].r - reference[i].r));
                    }
                    Assert.That(maxError, Is.LessThan(.02f), mode + " " + size + " radius=" + radius);
                }
                else reference = actual;
            }
        }
    }

    [UnityTest] public IEnumerator RoundedCorners_ClipRecalculatesAfterResizeAndShapeUpdate()
    {
        _emptyOverload = true;
        _box = Modifier.Size(200.Dp(), 80.Dp()).Clip(RoundedCornerShape(1000.Dp())).Background(Color.White);
        Mount(); yield return null; Flush();
        Near(E("box").resolvedStyle.borderTopLeftRadius, 40);
        _box = Modifier.Size(200.Dp(), 20.Dp()).Clip(RoundedCornerShape(1000.Dp())).Background(Color.White);
        Update(); yield return null; Flush();
        Near(E("box").resolvedStyle.borderTopLeftRadius, 10);
        _box = Modifier.Size(200.Dp(), 20.Dp()).Clip(RoundedCornerShape(4.Dp())).Background(Color.White);
        Update(); yield return null; Flush();
        Near(E("box").resolvedStyle.borderTopLeftRadius, 4);
    }

    [UnityTest] public IEnumerator RoundedCorners_InnerShadowUsesCircularCapsAfterResize()
    {
        _emptyOverload = true;
        var shape = RoundedCornerShape(1000.Dp());
        var shadow = new Compose.Net.Shadow(0.Dp(), 2.Dp(), Color.Black, new Offset(0, 0));
        _box = Modifier.Size(200.Dp(), 80.Dp()).Background(Color.White, shape).InnerShadow(shape, shadow);
        Mount(); yield return null; Flush();
        var foreground = E("box").Children().SingleOrDefault(element => element.GetType().Name == "Shadow");
        Assert.That(foreground, Is.Not.Null);
        Near(foreground!.resolvedStyle.borderTopLeftRadius, 42);
        _box = Modifier.Size(200.Dp(), 20.Dp()).Background(Color.White, shape).InnerShadow(shape, shadow);
        Update(); yield return null; Flush();
        Near(foreground.resolvedStyle.borderTopLeftRadius, 12);
        _box = Modifier.Size(200.Dp(), 20.Dp()).Background(Color.White).InnerShadow(RoundedCornerShape(4.Dp()), shadow);
        Update(); yield return null; Flush();
        Near(foreground.resolvedStyle.borderTopLeftRadius, 6);
        _box = Modifier.Size(200.Dp(), 60.Dp()).Background(Color.White);
        Update(); yield return null; Flush();
        Assert.That(foreground.parent, Is.Null, "Removing the modifier must release the shadow and geometry callback");
    }
}
