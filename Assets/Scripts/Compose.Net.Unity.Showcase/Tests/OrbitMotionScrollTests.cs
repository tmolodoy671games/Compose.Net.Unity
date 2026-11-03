using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Showcase.Tests;

internal static class OrbitPixels
{
    internal static Color32[] Read(VisualElement element, int inset = 0)
    {
        var render = element.CaptureToRenderTexture();
        try { return Read(render, new UnityEngine.Rect(inset, inset, render.width - inset * 2, render.height - inset * 2)); }
        finally { render.Release(); UnityEngine.Object.DestroyImmediate(render); }
    }

    internal static Color32[] Read(RenderTexture render, UnityEngine.Rect region)
    {
        var previous = RenderTexture.active;
        var texture = new Texture2D((int)region.width, (int)region.height, TextureFormat.RGBA32, false);
        try
        {
            RenderTexture.active = render;
            texture.ReadPixels(region, 0, 0);
            texture.Apply();
            return texture.GetPixels32();
        }
        finally
        {
            RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    internal static int Changed(Color32[] before, Color32[] after)
    {
        Assert.That(after.Length, Is.EqualTo(before.Length));
        var count = 0;
        for (var i = 0; i < before.Length; i++)
            if (Math.Abs(before[i].r - after[i].r) + Math.Abs(before[i].g - after[i].g) + Math.Abs(before[i].b - after[i].b) > 15) count++;
        return count;
    }
}

public sealed partial class OrbitScreenTests
{
    [UnityTest] public IEnumerator SidebarNavigationNeverStartsSharedBounds()
    {
        yield return Settle();
        foreach (var page in new[] { "Projects", "Overview", "Projects" })
        {
            simulate.Click(Find("nav." + page));
            for (var frame = 0; frame < 90; frame++)
            {
                yield return null; Frame();
                Assert.That(rootVisualElement.Q("shared-transition-overlay").childCount, Is.Zero, "Sidebar tab " + page + " must use only its screen transition");
            }
        }
        Click("project.card.1");
        var shared = false;
        for (var frame = 0; frame < 60; frame++)
        {
            yield return null; Frame();
            shared |= rootVisualElement.Q("shared-transition-overlay").childCount > 0;
        }
        Assert.That(shared, Is.True, "Opening a project's details should retain the shared cover transition");
        simulate.Click(Find("nav.Projects"));
        for (var frame = 0; frame < 90; frame++)
        {
            yield return null; Frame();
            Assert.That(rootVisualElement.Q("shared-transition-overlay").childCount, Is.Zero, "Sidebar navigation out of details must not morph covers");
        }
        Click("project.card.2"); yield return Settle(); Click("project.back");
        var returning = false;
        for (var frame = 0; frame < 90; frame++)
        {
            yield return null; Frame();
            returning |= rootVisualElement.Q("shared-transition-overlay").childCount > 0;
        }
        Assert.That(returning, Is.True, "The Back button should animate the cover back to its source card");
        simulate.Click(Find("project.card.1")); Frame(80); simulate.Click(Find("nav.Overview"));
        for (var frame = 0; frame < 90; frame++)
        {
            yield return null; Frame();
            Assert.That(rootVisualElement.Q("shared-transition-overlay").childCount, Is.Zero, "Sidebar navigation must cancel an unfinished shared transition");
        }
    }

    [UnityTest] public IEnumerator ParticlesRenderMovingFramesAndPauseResume()
    {
        yield return Settle(); Click("nav.Motion"); yield return Settle();
        var canvas = Find("motion.canvas");
        for (var mode = 0; mode < 3; mode++)
        {
            Click("pattern." + mode); yield return Settle();
            var before = OrbitPixels.Read(canvas);
            for (var frame = 0; frame < 30; frame++) { yield return null; Frame(); }
            Assert.That(OrbitPixels.Changed(before, OrbitPixels.Read(canvas)), Is.GreaterThan(100), "Particle pattern must actually move: " + mode);
        }
        Click("motion.pause"); yield return Settle();
        var paused = OrbitPixels.Read(canvas);
        for (var frame = 0; frame < 30; frame++) { yield return null; Frame(); }
        Assert.That(OrbitPixels.Changed(paused, OrbitPixels.Read(canvas)), Is.Zero, "Pause must freeze the rendered particles");
        Click("motion.pause"); yield return Settle();
        Assert.That(OrbitPixels.Changed(paused, OrbitPixels.Read(canvas)), Is.GreaterThan(100), "Resume must restart particle motion");
    }

    [UnityTest] public IEnumerator TrackpadBurstsKeepEveryPageWithinItsScrollRange()
    {
        panelSize = new Vector2(760, 650); yield return Settle();
        foreach (var page in new[] { "Overview", "Projects", "Project", "Activity", "Focus", "Settings", "Motion" })
        {
            if (page == "Project") Click("project.card.2");
            else Click("nav." + page);
            yield return Settle();
            var view = Find("screen." + page).Q<ScrollView>();
            Assert.That(view, Is.Not.Null);
            foreach (var direction in new[] { 1, -1, 1, -1 })
            {
                // Frequent fractional deltas followed by a decaying momentum tail.
                for (var i = 0; i < 120; i++)
                {
                    var delta = (i % 15 == 0 ? 15f : i < 80 ? 1.4f : 1.4f * (120-i)/40f) * direction;
                    simulate.ScrollWheel(new Vector2(.02f * direction, delta), view.contentViewport.worldBound.center);
                    Frame(8);
                    Assert.That(view.contentContainer.worldBound.Overlaps(view.contentViewport.worldBound), Is.True, page + " must remain visible during the gesture");
                    var limit = view.contentViewport.layout.height * .35f;
                    Assert.That(view.scrollOffset.y, Is.InRange(-limit, Math.Max(0, view.verticalScroller.highValue) + limit), page + " overscroll must remain bounded during the gesture");
                    if (i % 8 == 0) yield return null;
                }
                yield return Settle();
                Assert.That(view.scrollOffset.y, Is.InRange(-1f, Math.Max(0, view.verticalScroller.highValue) + 1), page + " offset after releasing the trackpad");
                Assert.That(view.contentContainer.worldBound.Overlaps(view.contentViewport.worldBound), Is.True, page + " must not turn into a blank viewport");
            }
        }
    }
}
