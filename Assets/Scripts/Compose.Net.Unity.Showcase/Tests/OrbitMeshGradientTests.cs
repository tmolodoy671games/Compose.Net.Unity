using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Showcase.Tests;

public sealed partial class OrbitScreenTests
{
    [UnityTest] public IEnumerator MeshGradientRendersAnimatesAndRespectsMotionSettings()
    {
        yield return Settle(); Click("nav.Motion"); yield return Settle();
        var canvas = Find("mesh.canvas");
        canvas.GetFirstAncestorOfType<ScrollView>().ScrollTo(Find("mesh.card"));
        yield return Settle();
        var before = OrbitPixels.Read(canvas);
        Assert.That(before.Select(p => (p.r, p.g, p.b)).Distinct().Count(), Is.GreaterThan(256), "The mesh must render a continuous range of colors");
        CapturePreview("mesh-gradient-dark");
        for (var frame = 0; frame < 40; frame++) { yield return null; Frame(); }
        Assert.That(OrbitPixels.Changed(before, OrbitPixels.Read(canvas)), Is.GreaterThan(1000), "State reads in the remembered painter must invalidate drawing");

        _state.AmbientMotion.Value = false; yield return Settle();
        var paused = OrbitPixels.Read(canvas);
        for (var frame = 0; frame < 30; frame++) { yield return null; Frame(); }
        Assert.That(OrbitPixels.Changed(paused, OrbitPixels.Read(canvas)), Is.Zero, "Ambient pause freezes the mesh");
        _state.AmbientMotion.Value = true; yield return Settle();
        Assert.That(OrbitPixels.Changed(paused, OrbitPixels.Read(canvas)), Is.GreaterThan(1000), "Ambient resume restarts the mesh");

        _state.ReducedMotion.Value = true; yield return Settle();
        var reduced = OrbitPixels.Read(canvas);
        for (var frame = 0; frame < 30; frame++) { yield return null; Frame(); }
        Assert.That(OrbitPixels.Changed(reduced, OrbitPixels.Read(canvas)), Is.Zero, "Reduced motion freezes the mesh");
        _state.LightTheme.Value = true; yield return Settle();
        Assert.That(OrbitPixels.Changed(reduced, OrbitPixels.Read(canvas)), Is.GreaterThan(1000), "A retained painter follows the current palette");
        CapturePreview("mesh-gradient-light");
        panelSize = new Vector2(760, 900); yield return Settle();
        canvas.GetFirstAncestorOfType<ScrollView>().ScrollTo(Find("mesh.card")); yield return Settle();
        Assert.That(canvas.layout.width, Is.InRange(400, 650));
        Assert.That(OrbitPixels.Read(canvas).Select(p => (p.r, p.g, p.b)).Distinct().Count(), Is.GreaterThan(256));
        CapturePreview("mesh-gradient-compact");
        Click("nav.Overview"); yield return Settle();
        Assert.That(rootVisualElement.Q("mesh.canvas"), Is.Null, "Navigating away disposes the mesh surface");
    }
}
