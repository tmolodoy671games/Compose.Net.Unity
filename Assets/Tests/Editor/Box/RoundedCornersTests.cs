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
        private readonly float _scaleX, _scaleY;
        internal CornerPixels(VisualElement element)
        {
            var render = element.CaptureToRenderTexture();
            var previous = RenderTexture.active;
            Width = render.width; Height = render.height;
            _scaleX = Width / element.layout.width; _scaleY = Height / element.layout.height;
            _texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            try { RenderTexture.active = render; _texture.ReadPixels(new UnityEngine.Rect(0, 0, Width, Height), 0, 0); _texture.Apply(); }
            finally { RenderTexture.active = previous; render.Release(); UnityEngine.Object.DestroyImmediate(render); }
        }
        internal UnityEngine.Color[] Read() => _texture.GetPixels();
        internal float Alpha(float x, float y) => _texture.GetPixel(Mathf.Clamp((int)(x * _scaleX), 0, Width - 1), Mathf.Clamp(Height - 1 - (int)(y * _scaleY), 0, Height - 1)).a;
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
        foreach (var height in new[] { 80f, 20f, 60f })
        {
            _box = Modifier.Size(200.Dp(), height.Dp()).Background(Color.White, shape).InnerShadow(shape, shadow);
            Update(); yield return null; Flush();
            using var oversized = new CornerPixels(E("box"));
            var expectedShape = RoundedCornerShape((height / 2).Dp());
            _box = Modifier.Size(200.Dp(), height.Dp()).Background(Color.White, expectedShape).InnerShadow(expectedShape, shadow);
            Update(); yield return null; Flush();
            using var capped = new CornerPixels(E("box"));
            var actual = oversized.Read(); var expected = capped.Read();
            Assert.That(actual.Length, Is.EqualTo(expected.Length));
            for (var i = 0; i < actual.Length; i++)
            {
                Assert.That(actual[i].a, Is.EqualTo(expected[i].a).Within(.02f), "Clip after resize");
                Assert.That(actual[i].r, Is.EqualTo(expected[i].r).Within(.02f), "Inset shadow after resize");
            }
            Assert.That(actual.Any(pixel => pixel.a > .9f && pixel.r < .5f), Is.True, "The inset shadow must be visible");
        }
        _box = Modifier.Size(200.Dp(), 60.Dp()).Background(Color.White);
        Update(); yield return null; Flush();
        using var removed = new CornerPixels(E("box"));
        Assert.That(removed.Read().Where(pixel => pixel.a > .9f).All(pixel => pixel.r > .98f), Is.True,
            "Removing the modifier must remove its shadow drawing callback");
    }
}
