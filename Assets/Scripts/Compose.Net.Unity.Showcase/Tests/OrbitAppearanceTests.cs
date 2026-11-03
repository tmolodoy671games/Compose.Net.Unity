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

namespace Compose.Net.Unity.Showcase.Tests;

public sealed partial class OrbitScreenTests
{
    private sealed class Pixels : IDisposable
    {
        private readonly Texture2D _texture;
        internal readonly int Width, Height;
        internal Pixels(VisualElement element)
        {
            var render = element.CaptureToRenderTexture();
            var previous = RenderTexture.active;
            Width = render.width; Height = render.height;
            _texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            try { RenderTexture.active = render; _texture.ReadPixels(new UnityEngine.Rect(0, 0, Width, Height), 0, 0); _texture.Apply(); }
            finally { RenderTexture.active = previous; render.Release(); UnityEngine.Object.DestroyImmediate(render); }
        }
        internal UnityEngine.Color At(float x, float y) => _texture.GetPixel(Mathf.Clamp(Mathf.FloorToInt(x), 0, Width - 1), Mathf.Clamp(Height - 1 - Mathf.FloorToInt(y), 0, Height - 1));
        public void Dispose() => UnityEngine.Object.DestroyImmediate(_texture);
    }

    [Composable] private void AppearanceProbe()
    {
        Column(modifier: Modifier.FillMaxSize().Background(Color.Black).Padding(24.Dp()), content: () =>
        {
            Box(Modifier.Size(28.Dp(), 6.Dp()).Background(Color.White, RoundedCornerShape(16.Dp())).TestTag("probe.dot"));
            Spacer(Modifier.Height(20.Dp()));
            Box(Modifier.Size(96.Dp(), 10.Dp()).Background(Color.White, RoundedCornerShape(16.Dp())).TestTag("probe.pill"));
            Spacer(Modifier.Height(20.Dp()));
            Box(Modifier.Size(180.Dp(), 100.Dp()).Background(new Color(.1f, .15f, .2f), RoundedCornerShape(16.Dp()))
                .Border(3.Dp(), SolidColor(Color.White), RoundedCornerShape(16.Dp())).TestTag("probe.border"));
            Spacer(Modifier.Height(20.Dp()));
            Box(Modifier.Size(100.Dp(), 60.Dp()).Background(SolidColor(Color.White), RoundedCornerShape(bottomLeft: 20.Dp())).TestTag("probe.asymmetric"));
        });
    }
    private void MountAppearanceProbe()
    {
        _composer.Dispose(); rootVisualElement.Clear(); _composer = IComposer.Create();
        Bootstrap(_composer, _adapter, false, new ComposeConfig(Logging: false), new RootNode(rootVisualElement), AppearanceProbe);
    }

    [UnityTest] public IEnumerator OversizedRadiiKeepSmallIndicatorsCapsuleShaped()
    {
        MountAppearanceProbe(); yield return Settle();
        foreach (var tag in new[] { "probe.dot", "probe.pill" })
        {
            var element = Find(tag);
            Assert.That(element.resolvedStyle.borderTopLeftRadius, Is.LessThanOrEqualTo(element.layout.height / 2 + .01f), "Clip must use the same circular cap as the drawing");
            using var pixels = new Pixels(element);
            Assert.That(pixels.At(pixels.Height / 2f, 1).r, Is.GreaterThan(.8f), tag + " must have a straight top after the circular end cap");
            Assert.That(pixels.At(pixels.Width - pixels.Height / 2f - 1, 1).r, Is.GreaterThan(.8f), tag + " right cap");
        }
        CapturePreview("rounded-indicators");
    }

    [UnityTest] public IEnumerator RoundedBorderRendersAllFourStraightEdges()
    {
        MountAppearanceProbe(); yield return Settle(); using var pixels = new Pixels(Find("probe.border"));
        foreach (var point in new[] { new Vector2(pixels.Width / 2f, 1), new Vector2(pixels.Width / 2f, pixels.Height - 2), new Vector2(1, pixels.Height / 2f), new Vector2(pixels.Width - 2, pixels.Height / 2f) })
            Assert.That(pixels.At(point.x, point.y).r, Is.GreaterThan(.75f), "Missing border at " + point);
        CapturePreview("rounded-border");
    }

    [UnityTest] public IEnumerator IndependentBottomCornerRadiusIsRespected()
    {
        MountAppearanceProbe(); yield return Settle(); using var pixels = new Pixels(Find("probe.asymmetric"));
        Assert.That(pixels.At(2, pixels.Height - 3).r, Is.LessThan(.1f), "Bottom-left must use its own radius");
        Assert.That(pixels.At(pixels.Width - 3, pixels.Height - 3).r, Is.GreaterThan(.9f), "Bottom-right must remain square");
    }

    [UnityTest] public IEnumerator NestedPagerLeavesVerticalTrackpadGesturesToTheScreen()
    {
        yield return Settle();
        var outer = Find("screen.Overview").Q<ScrollView>(); var pager = (ScrollView)Find("hero.pager");
        outer.ScrollTo(pager); yield return Settle();
        var before = outer.scrollOffset.y; var page = pager.scrollOffset.x;
        simulate.ScrollWheel(new Vector2(.08f, 2), pager.contentViewport.worldBound.center); Frame();
        Assert.That(outer.scrollOffset.y, Is.GreaterThan(before + 1), "Vertical wheel over the pager must move its containing screen");
        Assert.That(pager.scrollOffset.x, Is.EqualTo(page).Within(.5f), "Minor horizontal noise must not change pages");
        outer.ScrollTo(pager); yield return Settle(); before = outer.scrollOffset.y;
        simulate.ScrollWheel(new Vector2(3, .02f), pager.contentViewport.worldBound.center); Frame();
        Assert.That(pager.scrollOffset.x, Is.GreaterThan(page + 1), "Horizontal wheel must still move the pager");
        Assert.That(outer.scrollOffset.y, Is.EqualTo(before).Within(.5f));
    }

    [UnityTest] public IEnumerator NativeCollectionHoverDoesNotPaintGreyRows()
    {
        _state.AmbientMotion.Value = false;
        yield return Settle(); Click("nav.Projects"); yield return Settle();
        foreach (var light in new[] { false, true })
        {
            _state.LightTheme.Value = light; yield return Settle();
            simulate.MouseMove(Find("project.new").worldBound.center); yield return Settle();
            var card = Find("project.card.1"); var second = Find("project.card.2");
            var point = new Vector2((card.worldBound.xMax + second.worldBound.xMin) / 2, card.worldBound.yMin + 80);
            // The background now has linen texture: compare the same point before
            // and after hover instead of expecting a flat palette color.
            using var unhovered = new Pixels(rootVisualElement);
            var expected = unhovered.At(point.x * unhovered.Width / rootVisualElement.layout.width, point.y * unhovered.Height / rootVisualElement.layout.height);
            simulate.MouseMove(card.worldBound.center); yield return Settle();
            var list = Find("projects.grid").Q<ListView>();
            Assert.That(card.worldBound.yMin, Is.GreaterThanOrEqualTo(list.Q<ScrollView>().contentViewport.worldBound.yMin + 1), "Hover lift must not clip the top border of the first row");
            foreach (var e in list.Query<VisualElement>().ToList())
            {
                if (!e.ClassListContains("unity-collection-view__item")) continue;
                Assert.That(e.resolvedStyle.backgroundColor.a, Is.Zero.Within(.01), "Native row background: " + string.Join(",", e.GetClasses()));
            }
            using var pixels = new Pixels(rootVisualElement);
            var actual = pixels.At(point.x * pixels.Width / rootVisualElement.layout.width, point.y * pixels.Height / rootVisualElement.layout.height);
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(.02f), "Hover must preserve the textured background between cards");
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(.02f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(.02f));
            CapturePreview(light ? "grid-hover-light" : "grid-hover-dark");
        }
    }

    [UnityTest] public IEnumerator ActivitySectionsNeverOverlapAtShortAndWideViewports()
    {
        yield return Settle(); Click("nav.Activity"); yield return Settle();
        foreach (var size in new[] { new Vector2(1280, 720), new Vector2(2650, 742), new Vector2(760, 650) })
        {
            panelSize = size; yield return Settle();
            var heading = Find("activity.heading"); var metrics = Find("activity.metrics"); var caption = Find("activity.caption"); var list = Find("activity.list");
            var textBottom = heading.Query<TextElement>().ToList().Max(e => e.worldBound.yMax);
            var metricsBottom = metrics.Query<TextElement>().ToList().Max(e => e.worldBound.yMax);
            Assert.That(metrics.worldBound.yMin, Is.GreaterThanOrEqualTo(textBottom + 8), "Metrics overlap the subtitle at " + size);
            Assert.That(caption.worldBound.yMin, Is.GreaterThanOrEqualTo(metricsBottom + 8), "Activity caption overlaps the metric cards at " + size);
            Assert.That(list.worldBound.yMin, Is.GreaterThanOrEqualTo(caption.worldBound.yMax + 4), "List overlaps its caption");
            Assert.That(list.layout.height, Is.GreaterThan(40), "The list must retain a usable viewport");
            CapturePreview("activity-layout-" + (int)size.x);
        }
    }
}
