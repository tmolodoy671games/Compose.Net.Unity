using System;
using System.Collections;
using System.IO;
using System.Linq;
using Compose.Net.SoftDemo;
using Compose.Net.Unity.SoftDemo.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.UIElements.TestFramework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Compose.Net.Unity.SoftDemo.Tests;

public sealed class SoftDemoTests : EditorWindowUITestFixture<SoftDemoWindow>
{
    [SetUp] public void ResetSession() { RecreatePanel(); panelSize = new Vector2(1240, 940); simulate.needsRendering = true; }

    private IEnumerator Settle(float seconds = .65f)
    {
        var deadline = EditorApplication.timeSinceStartup + seconds;
        while (EditorApplication.timeSinceStartup < deadline) { yield return null; simulate.FrameUpdate(); }
    }
    private VisualElement Find(string tag) { var item = rootVisualElement.Q(tag); Assert.That(item, Is.Not.Null, tag); return item; }
    private void Click(string tag) => simulate.Click(Find(tag));
    private void Capture(string name)
    {
        var dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "compose-soft", "captures"); Directory.CreateDirectory(dir);
        rootVisualElement.CaptureToPNG(System.IO.Path.Combine(dir, name + ".png"));
    }

    [UnityTest] public IEnumerator ThreePhonesRenderAndCompactNavigationRemainsReachable()
    {
        yield return Settle(1.2f); Capture("soft-desktop");
        for (var i = 0; i < 3; i++)
        {
            var phone = Find("phone." + i); Assert.That(phone.worldBound.width, Is.InRange(290, 310));
            Assert.That(phone.worldBound.xMin, Is.GreaterThan(0)); Assert.That(phone.worldBound.xMax, Is.LessThan(1240));
            Assert.That(Find("phone." + i + ".nav.2").worldBound.yMax, Is.LessThan(940));
        }
        Assert.That(Find("phone.0").worldBound.xMax, Is.LessThan(Find("phone.1").worldBound.xMin));
        foreach (var tag in new[] { "phone.0.begin", "phone.1.practice", "phone.1.reset", "phone.2.record" })
        {
            var action = Find(tag); var scroll = action.GetFirstAncestorOfType<ScrollView>();
            Assert.That(action.worldBound.yMax, Is.LessThanOrEqualTo(scroll.worldBound.yMax), tag + " must be visible without scrolling on desktop");
        }
        panelSize = new Vector2(390, 740); yield return Settle();
        Assert.That(rootVisualElement.Q("phone.1"), Is.Null);
        foreach (var page in new[] { 2, 1, 0 })
        {
            Click("phone.0.nav." + page); yield return Settle();
            Assert.That(Find("phone.0.page." + page), Is.Not.Null);
            Assert.That(Find("phone.0.nav.2").worldBound.yMax, Is.LessThan(740));
            Assert.That(Find("phone.0").worldBound.xMax, Is.LessThan(390));
        }
        Capture("soft-compact");
        panelSize = new Vector2(1240, 940); yield return Settle();
        Assert.That(Find("phone.2.page.2"), Is.Not.Null);
    }

    [UnityTest] public IEnumerator PracticeTicksPausesSurvivesNavigationAndFinishes()
    {
        yield return Settle(); Click("phone.1.practice"); yield return Settle(1.3f);
        Assert.That(window.State.Running.Value, Is.True); Assert.That(window.State.Remaining.Value, Is.LessThan(60));
        Click("phone.1.nav.0"); yield return Settle(1.1f);
        Assert.That(window.State.Elapsed.Value, Is.GreaterThan(2));
        Click("phone.1.nav.1"); yield return Settle(); Click("phone.1.practice"); yield return Settle(.2f);
        var elapsed = window.State.Elapsed.Value; yield return Settle(.35f); Assert.That(window.State.Elapsed.Value, Is.EqualTo(elapsed));
        Click("phone.1.practice"); yield return Settle(.2f); window.State.Advance(65); yield return Settle();
        Assert.That(window.State.Remaining.Value, Is.Zero); Assert.That(window.State.Running.Value, Is.False);
        Assert.That(window.State.Cue.Value, Is.EqualTo("A little lighter."));
        Click("phone.1.practice"); yield return Settle(.2f); Assert.That(window.State.Running.Value, Is.True);
        Click("phone.1.reset"); yield return Settle(); Assert.That(window.State.Remaining.Value, Is.EqualTo(60)); Assert.That(window.State.Running.Value, Is.False);
    }

    [UnityTest] public IEnumerator MoodSaveFavoritesAndArtworkUseRealPointerTargets()
    {
        yield return Settle(); Click("phone.2.mood.2"); yield return Settle();
        Assert.That(window.State.Mood.Value, Is.EqualTo(2)); Click("phone.2.record"); yield return Settle();
        Assert.That(window.State.RecordedMood.Value, Is.EqualTo(2));
        Click("phone.0.save"); yield return Settle(); Assert.That(window.State.Saved.Value, Is.True);
        Click("phone.2.save"); yield return Settle(); Assert.That(window.State.Saved.Value, Is.False);
        Click("phone.0.art"); yield return Settle(); Assert.That(window.State.Arrangement.Value, Is.EqualTo(1));
        Click("phone.0.begin"); yield return Settle(); Assert.That(window.State.Pages[0].Value, Is.EqualTo(1));
    }

    [UnityTest] public IEnumerator ShapesActuallyRepaintAndMotionPauseFreezesThem()
    {
        yield return Settle(); var art = Find("phone.0.art.surface"); var bounds = art.worldBound;
        var before = Pixels(art); yield return Settle(.8f);
        Assert.That(Changed(before, Pixels(art)), Is.GreaterThan(100), "Spheres must move in the actual renderer");
        Click("soft.motion"); yield return Settle(); var paused = Pixels(art);
        yield return Settle(.5f); Assert.That(Changed(paused, Pixels(art)), Is.Zero);
        Assert.That(art.worldBound, Is.EqualTo(bounds));
        Click("soft.motion"); yield return Settle(.8f); Assert.That(Changed(paused, Pixels(art)), Is.GreaterThan(100));
    }

    [UnityTest] public IEnumerator ShadowsExtendPastHitTargetsAndCardsHaveViewportGutters()
    {
        yield return Settle(); window.State.Motion.Value = false; yield return Settle();
        var button = Find("soft.motion"); var face = Find("soft.motion.surface");
        Assert.That(face.resolvedStyle.overflow, Is.EqualTo(Overflow.Visible));
        var capture = ReadPixels(rootVisualElement);
        float Luminance(Vector2 point)
        {
            var local = rootVisualElement.WorldToLocal(point);
            var x = Mathf.Clamp((int)(local.x / rootVisualElement.layout.width * capture.Width), 0, capture.Width - 1);
            var y = Mathf.Clamp((int)((1 - local.y / rootVisualElement.layout.height) * capture.Height), 0, capture.Height - 1);
            var pixel = capture.Pixels[y * capture.Width + x]; return (pixel.r + pixel.g + pixel.b) / 3f;
        }
        var bounds = button.worldBound;
        var outsideShadow = Luminance(new Vector2(bounds.center.x, bounds.yMax + 3));
        var background = Luminance(new Vector2(bounds.xMin - 24, bounds.yMax + 3));
        Assert.That(background - outsideShadow, Is.GreaterThan(1), "The rendered shadow must remain visible outside the clickable rectangle");
        var card = Find("phone.0.art.surface"); var viewport = card.GetFirstAncestorOfType<ScrollView>();
        Assert.That(card.worldBound.xMin - viewport.worldBound.xMin, Is.EqualTo(20).Within(2));
        Assert.That(viewport.worldBound.xMax - card.worldBound.xMax, Is.EqualTo(20).Within(2));
        foreach (var tag in new[] { "phone.0.ritual-card", "phone.1.pace-card" })
        {
            var last = Find(tag); var scroll = last.GetFirstAncestorOfType<ScrollView>();
            Assert.That(last.worldBound.yMax + 14, Is.LessThanOrEqualTo(scroll.worldBound.yMax), tag + " needs room for its lower shadow");
        }
        Capture("soft-shadows");
    }

    [Test] public void BreathingPhasesAndCompletionUseElapsedTime()
    {
        var s = new SoftState(); s.TogglePractice(); s.Advance(3);
        Assert.That(s.Cue.Value, Is.EqualTo("Breathe in")); Assert.That(s.Breath, Is.EqualTo(.75f));
        s.Advance(2); Assert.That(s.Cue.Value, Is.EqualTo("And hold"));
        s.Advance(2); Assert.That(s.Cue.Value, Is.EqualTo("Breathe out"));
        s.TogglePractice(); s.Advance(30); Assert.That(s.Elapsed.Value, Is.EqualTo(7));
        s.TogglePractice(); s.Advance(100); Assert.That(s.Remaining.Value, Is.Zero); Assert.That(s.Running.Value, Is.False);
        s.Advance(100); Assert.That(s.Elapsed.Value, Is.EqualTo(60));
        s.TogglePractice(); Assert.That(s.Remaining.Value, Is.EqualTo(60)); Assert.That(s.Running.Value, Is.True);
    }

    private static Color32[] Pixels(VisualElement element) => ReadPixels(element).Pixels;
    private static (Color32[] Pixels, int Width, int Height) ReadPixels(VisualElement element)
    {
        var render = element.CaptureToRenderTexture(); var previous = RenderTexture.active;
        var texture = new Texture2D(render.width, render.height, TextureFormat.RGBA32, false);
        try { RenderTexture.active = render; texture.ReadPixels(new UnityEngine.Rect(0, 0, render.width, render.height), 0, 0); texture.Apply(); return (texture.GetPixels32(), render.width, render.height); }
        finally { RenderTexture.active = previous; UnityEngine.Object.DestroyImmediate(texture); render.Release(); UnityEngine.Object.DestroyImmediate(render); }
    }
    private static int Changed(Color32[] a, Color32[] b) => a.Zip(b, (x, y) => Math.Abs(x.r - y.r) + Math.Abs(x.g - y.g) + Math.Abs(x.b - y.b)).Count(delta => delta > 15);
}
