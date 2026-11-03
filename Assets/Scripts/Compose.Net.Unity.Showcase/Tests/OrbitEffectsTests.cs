using System.Collections;
using System.Linq;
using Compose.Net;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static Compose.Net.ComposeFunctions;
using Color = Compose.Net.Color;

namespace Compose.Net.Unity.Showcase.Tests;

public sealed partial class OrbitScreenTests
{
    private readonly IMutableState<float> _pathShift = MutableStateOf(0f);
    [Composable] private void IndependentPathsProbe() => Box(Modifier.Size(200.Dp(), 100.Dp()).Background(Color.Black).TestTag("effects.paths").DrawBehind(d =>
    {
        using var first = Path(); first.AddRect(new Offset(20, 20), new Size(40, 40));
        d.DrawPath(first, Color.RedColor);
        using var second = Path(); second.MoveTo(100, 20 + _pathShift.Value); second.LineTo(160, 20 + _pathShift.Value);
        d.DrawPath(second, Color.Cyan, style: Stroke(4));
    }));

    [UnityTest] public IEnumerator AnimatedPathsStartFreshOnEveryDrawAndRepaint()
    {
        _composer.Dispose(); rootVisualElement.Clear(); _composer = IComposer.Create();
        Bootstrap(_composer, _adapter, false, new ComposeConfig(Logging: false), new RootNode(rootVisualElement), IndependentPathsProbe);
        yield return Settle();
        foreach (var shift in new[] { 0f, 20f, 40f, 0f })
        {
            _pathShift.Value = shift; yield return Settle();
            using var pixels = new Pixels(Find("effects.paths"));
            Assert.That(pixels.At(40, 20).r, Is.GreaterThan(.9f), "A later stroke must not recolor an earlier path");
            Assert.That(pixels.At(40, 20).g, Is.LessThan(.1f));
            Assert.That(pixels.At(130, 20 + shift).g, Is.GreaterThan(.9f), "The current path must render after repaint");
            Assert.That(pixels.At(130, shift == 0 ? 60 : 20).g, Is.LessThan(.1f), "Previous-frame contours must be cleared");
        }
    }

    [UnityTest] public IEnumerator ResonancePatternsMorphAnimateAndKeepTheirControlsReachable()
    {
        yield return Settle(); Click("nav.Motion"); yield return Settle();
        var canvas = Find("resonance.canvas");
        for (var pattern = 0; pattern < 3; pattern++)
        {
            Click("resonance.pattern." + pattern); yield return Settle();
            canvas.GetFirstAncestorOfType<ScrollView>().ScrollTo(Find("resonance.card")); yield return Settle();
            Assert.That(_state.ResonancePattern.Value, Is.EqualTo(pattern));
            var bounds = canvas.worldBound; var before = OrbitPixels.Read(canvas);
            CapturePreview("resonance-" + pattern);
            for (var frame = 0; frame < 40; frame++) { yield return null; Frame(); }
            Assert.That(OrbitPixels.Changed(before, OrbitPixels.Read(canvas)), Is.GreaterThan(500), "Each ribbon pattern must animate in the actual renderer");
            Assert.That(Find("resonance.canvas"), Is.SameAs(canvas));
            Assert.That(canvas.worldBound, Is.EqualTo(bounds), "Drawing must not animate the layout");
            Click("resonance.tempo." + pattern); yield return Settle();
            Assert.That(_state.ResonanceTempo.Value, Is.EqualTo(pattern));
        }
        Click("resonance.pulse"); Assert.That(_state.ResonancePulse.Value, Is.EqualTo(1));
        panelSize = new Vector2(720, 560); yield return Settle();
        foreach (var tag in new[] { "resonance.pattern.0", "resonance.pattern.2", "resonance.pulse", "resonance.tempo.0", "resonance.tempo.2" })
        {
            Click(tag); yield return Settle();
            Assert.That(Find(tag).worldBound.xMax, Is.LessThanOrEqualTo(720));
        }
        _state.LightTheme.Value = true; yield return Settle();
        canvas.GetFirstAncestorOfType<ScrollView>().ScrollTo(Find("resonance.card")); yield return Settle();
        CapturePreview("resonance-compact-light");
        Click("nav.Overview"); yield return Settle();
        Assert.That(rootVisualElement.Q("resonance.canvas"), Is.Null);
    }

    [UnityTest] public IEnumerator ResonancePauseResumeKeepsPhaseAndReducedMotionFreezesEffects()
    {
        yield return Settle(); Click("nav.Motion"); yield return Settle();
        var canvas = Find("resonance.canvas");
        canvas.GetFirstAncestorOfType<ScrollView>().ScrollTo(Find("resonance.card")); yield return Settle();
        _state.AmbientMotion.Value = false; yield return Settle();
        var paused = OrbitPixels.Read(canvas);
        for (var frame = 0; frame < 40; frame++) { yield return null; Frame(); }
        Assert.That(OrbitPixels.Changed(paused, OrbitPixels.Read(canvas)), Is.Zero);
        _state.AmbientMotion.Value = true; Frame(0); yield return null; Frame(0);
        Assert.That(OrbitPixels.Changed(paused, OrbitPixels.Read(canvas)), Is.Zero, "Resume must retain phase instead of restarting the ribbons");
        for (var frame = 0; frame < 40; frame++) { yield return null; Frame(); }
        Assert.That(OrbitPixels.Changed(paused, OrbitPixels.Read(canvas)), Is.GreaterThan(500));
        _state.ReducedMotion.Value = true; yield return Settle();
        var reduced = OrbitPixels.Read(canvas);
        _state.ResonancePulse.Value++;
        for (var frame = 0; frame < 40; frame++) { yield return null; Frame(); }
        Assert.That(OrbitPixels.Changed(reduced, OrbitPixels.Read(canvas)), Is.Zero, "Reduced motion suppresses both ambient and triggered pulses");
        _state.LightTheme.Value = true; yield return Settle();
        Assert.That(OrbitPixels.Changed(reduced, OrbitPixels.Read(canvas)), Is.GreaterThan(1000), "Frozen animation must still respond to the palette");
    }

    [UnityTest] public IEnumerator ChromeGlintsAnimateWithoutMovingOrCoveringControls()
    {
        yield return Settle();
        var button = Find("project.new"); var bounds = button.worldBound;
        var before = OrbitPixels.Read(button);
        for (var frame = 0; frame < 60; frame++) { yield return null; Frame(); }
        Assert.That(OrbitPixels.Changed(before, OrbitPixels.Read(button)), Is.GreaterThan(5), "The primary control has a moving edge highlight");
        Assert.That(button.worldBound, Is.EqualTo(bounds));
        _state.AmbientMotion.Value = false; yield return Settle();
        var paused = OrbitPixels.Read(button);
        for (var frame = 0; frame < 40; frame++) { yield return null; Frame(); }
        Assert.That(OrbitPixels.Changed(paused, OrbitPixels.Read(button)), Is.Zero);
        Click("project.new"); yield return Settle();
        Assert.That(Find("dialog.create"), Is.Not.Null, "Decorative layers must not intercept clicks");
        Click("project.cancel"); yield return Settle();
        Assert.That(rootVisualElement.Q("dialog.create"), Is.Null);
    }

    [UnityTest] public IEnumerator DenseParticlesAndRepeatedPulsesRetainOneInteractiveCanvas()
    {
        yield return Settle(); Click("nav.Motion"); yield return Settle();
        Click("particles.256"); yield return Settle();
        Assert.That(_state.ParticleCount.Value, Is.EqualTo(256));
        var canvas = Find("motion.canvas"); var bounds = canvas.worldBound;
        var nodes = canvas.Query<VisualElement>().ToList().Count;
        var before = OrbitPixels.Read(canvas);
        for (var i = 0; i < 12; i++) { Click("motion.pulse"); Frame(30); yield return null; }
        Assert.That(_state.ParticlePulse.Value, Is.EqualTo(12));
        Assert.That(OrbitPixels.Changed(before, OrbitPixels.Read(canvas)), Is.GreaterThan(1000));
        Assert.That(canvas.Query<VisualElement>().ToList().Count, Is.EqualTo(nodes), "Pulses and trails must not accumulate UI elements");
        Assert.That(Find("motion.canvas"), Is.SameAs(canvas));
        Assert.That(canvas.worldBound, Is.EqualTo(bounds));
        CapturePreview("particles-256-pulse");
        _state.ReducedMotion.Value = true; yield return Settle();
        var reduced = OrbitPixels.Read(canvas);
        Click("motion.pulse"); yield return Settle();
        Assert.That(OrbitPixels.Changed(reduced, OrbitPixels.Read(canvas)), Is.Zero);
    }

    [UnityTest] public IEnumerator CompletingMilestonesPlaysABoundedCelebrationAndDisposesIt()
    {
        // Keep the independently expiring toast out of the captured burst area.
        _state.Notifications.Value = false;
        yield return Settle(); Click("nav.Projects"); yield return Settle();
        Click("project.card.1"); yield return Settle();
        Click("task.3"); yield return Settle(); Click("task.4");
        for (var frame = 0; frame < 28; frame++) { yield return null; Frame(); }
        var fireworks = Find("completion.fireworks");
        fireworks.GetFirstAncestorOfType<ScrollView>().ScrollTo(fireworks); Frame(0);
        // Exclude the two edge pixels where adjacent SparkBars are antialiased.
        var before = OrbitPixels.Read(fireworks, inset: 2);
        for (var frame = 0; frame < 25; frame++) { yield return null; Frame(); }
        Assert.That(OrbitPixels.Changed(before, OrbitPixels.Read(fireworks, inset: 2)), Is.GreaterThan(30), "Completing all milestones fires the celebration");
        CapturePreview("completion-fireworks");
        yield return Settle(); var completed = OrbitPixels.Read(fireworks, inset: 2);
        for (var frame = 0; frame < 40; frame++) { yield return null; Frame(); }
        Assert.That(OrbitPixels.Changed(completed, OrbitPixels.Read(fireworks, inset: 2)), Is.Zero, "The burst ends rather than looping forever");
        Click("nav.Projects"); yield return Settle();
        Assert.That(rootVisualElement.Q("completion.fireworks"), Is.Null);
    }
}
