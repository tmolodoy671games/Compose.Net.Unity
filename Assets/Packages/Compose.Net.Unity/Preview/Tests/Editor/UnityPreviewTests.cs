using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Compose.Net.Unity.Packages.Compose.Net.Unity.Runtime.Adapters;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.UIElements.TestFramework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Compose.Net.Unity.Preview.Tests;

public sealed partial class UnityPreviewTests : EditorWindowUITestFixture<UnityPreviewWindow>
{
    private static UnityPreviewEntry Counter(int width = 360) => UnityPreviewCatalog.Discover()
        .Single(entry => entry.Method.DeclaringType == typeof(UnityPreviewSamples) && entry.Settings.Width == width);

    [Test]
    public void DiscoversPrivateMethodsAndIndependentVariants()
    {
        var phone = Counter();
        var wide = Counter(640);
        Assert.That(phone.Method.IsPrivate, Is.True);
        Assert.That(phone.Error, Is.Null);
        Assert.That(phone.Id, Is.Not.EqualTo(wide.Id));
        Assert.That(phone.CreateContent(), Is.Not.Null);
    }

    [UnityTest]
    public IEnumerator StoppedPreviewIsVisibleAndRunAcceptsClicksRestartResetsState()
    {
        panelSize = new Vector2(800, 600);
        yield return null;
        window.Refresh();
        yield return Settle();
        Assert.That(window.IsRunning, Is.False);
        Assert.That(rootVisualElement.Q("preview.counter"), Is.Not.Null);
        Assert.That(rootVisualElement.Q("preview.viewport").enabledSelf, Is.True);
        Assert.That(rootVisualElement.Q("preview.input-shield"), Is.Not.Null);
        Assert.That(rootVisualElement.Query<ToolbarButton>().ToList().Count, Is.EqualTo(1));
        Assert.That(rootVisualElement.Q<ToolbarButton>("preview.play-stop").text, Is.EqualTo("Play"));
        window.StartPreview(Counter().Id);
        yield return Settle();
        Assert.That(window.IsRunning, Is.True);
        Assert.That(rootVisualElement.Q<ToolbarButton>("preview.play-stop").text, Is.EqualTo("Stop"));
        var viewport = rootVisualElement.Q("preview.viewport");
        Assert.That(viewport.resolvedStyle.width, Is.EqualTo(360));
        Assert.That(viewport.resolvedStyle.height, Is.EqualTo(240));
        Assert.That(rootVisualElement.Query<Label>().ToList().Any(label => label.text.StartsWith("Count: 0")), Is.True);
        simulate.Click(rootVisualElement.Q("preview.counter"));
        yield return Settle();
        Assert.That(rootVisualElement.Query<Label>().ToList().Any(label => label.text.StartsWith("Count: 1")), Is.True);
        window.StartPreview(Counter(640).Id);
        yield return Settle();
        Assert.That(viewport.resolvedStyle.width, Is.EqualTo(640));
        Assert.That(rootVisualElement.Query<Label>().ToList().Any(label => label.text.StartsWith("Count: 0")), Is.True);
        window.StopPreview();
        Assert.That(window.IsRunning, Is.False);
        Assert.That(rootVisualElement.Q<ToolbarButton>("preview.play-stop").text, Is.EqualTo("Play"));
        yield return Settle();
        Assert.That(viewport.childCount, Is.GreaterThan(0));
        Assert.That(viewport.enabledSelf, Is.True);
        Assert.That(rootVisualElement.Q("preview.input-shield"), Is.Not.Null);
        Assert.That(rootVisualElement.Query<Label>().ToList().Any(label => label.text.StartsWith("Count: 0")), Is.True);
        window.Refresh();
        yield return Settle();
        Assert.That(window.IsRunning, Is.False);
        Assert.That(rootVisualElement.Q("preview.counter"), Is.Not.Null);
    }

    [UnityTest]
    public IEnumerator StoppedAndRunningTextHaveIdenticalStylesWhileStoppedInputIsBlocked()
    {
        panelSize = new Vector2(800, 600);
        window.StartPreview(Counter().Id);
        window.StopPreview();
        yield return Settle();
        var stopped = rootVisualElement.Query<Label>().ToList().Single(label => label.text.StartsWith("Count:"));
        var color = stopped.resolvedStyle.color;
        var font = stopped.resolvedStyle.unityFontStyleAndWeight;
        var size = stopped.resolvedStyle.fontSize;
        var opacity = stopped.resolvedStyle.opacity;
        Assert.That(stopped.enabledInHierarchy, Is.True);
        var counter = rootVisualElement.Q("preview.counter");
        Assert.That(rootVisualElement.panel.Pick(counter.worldBound.center), Is.SameAs(rootVisualElement.Q("preview.input-shield")));
        simulate.Click(counter);
        yield return Settle();
        Assert.That(stopped.text, Does.StartWith("Count: 0"));
        simulate.Click(rootVisualElement.Q("preview.play-stop"));
        yield return Settle();
        Assert.That(window.IsRunning, Is.True);
        Assert.That(rootVisualElement.Q("preview.input-shield"), Is.Null);
        var running = rootVisualElement.Query<Label>().ToList().Single(label => label.text.StartsWith("Count:"));
        Assert.That(running.resolvedStyle.color, Is.EqualTo(color));
        Assert.That(running.resolvedStyle.unityFontStyleAndWeight, Is.EqualTo(font));
        Assert.That(running.resolvedStyle.fontSize, Is.EqualTo(size));
        Assert.That(running.resolvedStyle.opacity, Is.EqualTo(opacity));
    }

    [Test]
    public void InvalidDeclarationsCannotRun()
    {
        var method = typeof(UnityPreviewTests).GetMethod(nameof(Invalid), BindingFlags.NonPublic | BindingFlags.Instance)!;
        var entry = new UnityPreviewEntry(method, new UnityPreviewAttribute(), 0);
        Assert.That(entry.Error, Does.Contain("static, parameterless void"));
        Assert.Throws<InvalidOperationException>(() => entry.CreateContent());
        var size = new UnityPreviewEntry(Counter().Method, new UnityPreviewAttribute { Width = 0 }, 0);
        Assert.That(size.Error, Does.Contain("positive"));
    }
    private void Invalid(int value) { }

    private int _asyncStarts;
    private int _timedStarts;

    [Composable]
    private void EffectContent()
    {
        LaunchedEffectFunctions.LaunchedEffect(0, (ICoroutineScope _) =>
        {
            _asyncStarts++;
            return Task.CompletedTask;
        });
        LaunchedEffectFunctions.LaunchedEffect(0, () => TimedEffect());
    }

    private IEnumerable<TimeSpan?> TimedEffect()
    {
        _timedStarts++;
        yield return null;
    }

    [Test]
    public void StaticPreviewDoesNotLaunchEffectsButRunningPreviewDoes()
    {
        _asyncStarts = _timedStarts = 0;
        using var adapter = new UnityPreviewAdapter();
        using var composer = IComposer.Create();
        var root = new EffectRoot(new VisualElement());
        ComposeFunctions.Bootstrap(composer, adapter, true, new ComposeConfig(Logging: false), root, EffectContent, runLaunchedEffects: false);
        adapter.Tick();
        Assert.That(_asyncStarts, Is.Zero);
        Assert.That(_timedStarts, Is.Zero);
        ComposeFunctions.Bootstrap(composer, adapter, false, new ComposeConfig(Logging: false), root, EffectContent);
        Assert.That(_asyncStarts, Is.EqualTo(1));
        Assert.That(_timedStarts, Is.EqualTo(1));
    }

    private sealed class EffectRoot : UnityReusableComposeNode, IReusableComposeNode
    {
        public EffectRoot(VisualElement element) : base(element) { }
        public new bool IsRoot => true;
    }

    [Test]
    public void EditorSchedulerAdvancesFrameClockAndCancelsCoroutinesOnDispose()
    {
        using var adapter = new UnityPreviewAdapter();
        IComposeAdapter contract = adapter;
        using var token = new CancellationTokenSource();
        var frame = contract.FrameClock.NextFrame(token.Token);
        Assert.That(frame.IsCompleted, Is.False);
        adapter.Tick();
        Assert.That(frame.IsCompletedSuccessfully, Is.True);
        var disposed = false;
        IEnumerable<TimeSpan?> Loop()
        {
            try { while (true) yield return null; }
            finally { disposed = true; }
        }
        adapter.StartTimeCoroutine(Loop());
        adapter.Dispose();
        Assert.That(disposed, Is.True);
    }

    private IEnumerator Settle()
    {
        for (var i = 0; i < 10; i++) { yield return null; simulate.FrameUpdate(); }
    }
}
