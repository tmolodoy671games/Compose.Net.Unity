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

[AiGenerated]
public sealed partial class UnityPreviewTests : EditorWindowUITestFixture<UnityPreviewWindow>
{
    private static UnityPreviewEntry Counter(int width = 360) => UnityPreviewCatalog.Discover()
        .Single(entry => entry.Method.DeclaringType == typeof(UnityPreviewTestContent) && entry.Settings.Width == width);

    [Test]
    public void DiscoversPrivateMethodsAndIndependentVariants()
    {
        var phone = Counter();
        var wide = Counter(640);
        Assert.That(phone.Method.IsPrivate, Is.True);
        Assert.That(phone.Error, Is.Null);
        Assert.That(phone.Id, Is.Not.EqualTo(wide.Id));
        Assert.That(phone.CreateContent(), Is.Not.Null);
        Assert.That(typeof(UnityPreview).Assembly, Is.EqualTo(typeof(ComposeView).Assembly));
        Assert.That(typeof(Composable).Assembly.GetType("Compose.Net.UnityPreviewAttribute"), Is.Null);
    }

    [UnityTest]
    public IEnumerator StoppedPreviewIsVisibleAndRunAcceptsClicksRestartResetsState()
    {
        panelSize = new Vector2(800, 600);
        yield return null;
        window.SelectPreview(Counter().Id);
        yield return Settle();
        Assert.That(window.IsRunning, Is.False);
        Assert.That(rootVisualElement.Q("preview.counter"), Is.Not.Null);
        Assert.That(rootVisualElement.Q("preview.viewport").enabledSelf, Is.True);
        Assert.That(rootVisualElement.Q("preview.input-shield"), Is.Not.Null);
        Assert.That(rootVisualElement.Query<ToolbarButton>().ToList().Count, Is.EqualTo(1));
        Assert.That(rootVisualElement.Q<ToolbarButton>("preview.play-stop").text, Is.Empty);
        Assert.That(rootVisualElement.Q<PopupField<string>>(), Is.Null);
        Assert.That(rootVisualElement.Q("preview.status"), Is.Null);
        Assert.That(rootVisualElement.Q("preview.error").resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
        Assert.That(rootVisualElement.Q<ToolbarButton>("preview.play-stop").tooltip, Is.EqualTo("Play"));
        window.StartPreview(Counter().Id);
        yield return Settle();
        Assert.That(window.IsRunning, Is.True);
        Assert.That(rootVisualElement.Q<ToolbarButton>("preview.play-stop").tooltip, Is.EqualTo("Stop"));
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
        Assert.That(rootVisualElement.Q<ToolbarButton>("preview.play-stop").tooltip, Is.EqualTo("Play"));
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
        window.SelectPreview(Counter().Id);
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
        var entry = new UnityPreviewEntry(method, new UnityPreview(), 0);
        Assert.That(entry.Error, Does.Contain("static, parameterless void"));
        Assert.Throws<InvalidOperationException>(() => entry.CreateContent());
        var size = new UnityPreviewEntry(Counter().Method, new UnityPreview { Width = -2 }, 0);
        Assert.That(size.Error, Does.Contain("must be positive"));
    }
    private void Invalid(int value) { }

    [Test]
    public void FixedDimensionsScaleOnlyWhenTheyExceedTheScreen()
    {
        var fixedSize = UnityPreviewSizing.Resolve(new UnityPreview("Fixed", 500, 400), new Vector2(100, 100), new Vector2(1200, 800));
        Assert.That(fixedSize.Scale, Is.EqualTo(1));
        Assert.That(fixedSize.Logical, Is.EqualTo(new Vector2(500, 400)));
        var huge = UnityPreviewSizing.Resolve(new UnityPreview("Huge", 2400, 1800), new Vector2(100, 100), new Vector2(1200, 800));
        Assert.That(huge.Scale, Is.LessThan(1));
        Assert.That(huge.Logical, Is.EqualTo(new Vector2(2400, 1800)));
        Assert.That(huge.Display.x, Is.LessThanOrEqualTo(1200));
        Assert.That(huge.Display.y, Is.LessThanOrEqualTo(800));
        var mixed = UnityPreviewSizing.Resolve(new UnityPreview("Mixed", 2400), new Vector2(400, 300), new Vector2(1200, 800));
        Assert.That(mixed.Scale, Is.EqualTo(.5f));
        Assert.That(mixed.Display.y, Is.EqualTo(300));
        var screen = new UnityEngine.Rect(0, 0, 1200, 900);
        var bounded = UnityPreviewSizing.Constrain(new UnityEngine.Rect(2000, 2000, 5000, 5000), screen);
        Assert.That(bounded.xMin, Is.GreaterThanOrEqualTo(screen.xMin)); Assert.That(bounded.yMin, Is.GreaterThanOrEqualTo(screen.yMin));
        Assert.That(bounded.xMax, Is.LessThanOrEqualTo(screen.xMax)); Assert.That(bounded.yMax, Is.LessThanOrEqualTo(screen.yMax));
    }

    [UnityTest]
    public IEnumerator FixedNativePreviewLocksWindowDimensions()
    {
        panelSize = new Vector2(180, 180);
        window.SelectPreview(Counter().Id);
        yield return Settle();
        var viewport = rootVisualElement.Q("preview.viewport");
        Assert.That(viewport.worldBound.width, Is.EqualTo(360).Within(1));
        Assert.That(viewport.worldBound.height, Is.EqualTo(240).Within(1));
        Assert.That(window.minSize.x, Is.EqualTo(360).Within(1));
        Assert.That(window.minSize.y, Is.GreaterThan(240)); // Includes the toolbar.
        Assert.That(window.maxSize, Is.EqualTo(window.minSize));
        var responsive = UnityPreviewCatalog.Discover().Single(item => item.Method.DeclaringType == typeof(UnityPreviewTestContent)
            && item.Method.Name == "Responsive");
        window.SelectPreview(responsive.Id);
        yield return Settle();
        Assert.That(window.maxSize.x, Is.GreaterThan(window.minSize.x));
        Assert.That(window.maxSize.y, Is.GreaterThan(window.minSize.y));
    }

    [UnityTest]
    public IEnumerator FloatingFixedPreviewFitsWithoutScrollbars()
    {
        var floating = ScriptableObject.CreateInstance<UnityPreviewWindow>();
        try
        {
            floating.Show();
            floating.SelectPreview(Counter().Id);
            yield return Settle();
            var host = floating.rootVisualElement.Q("preview.host");
            var viewport = floating.rootVisualElement.Q("preview.viewport");
            var scroll = floating.rootVisualElement.Q<ScrollView>();
            Assert.That(host.contentRect.width, Is.EqualTo(360).Within(1));
            Assert.That(host.contentRect.height, Is.EqualTo(240).Within(1));
            Assert.That(scroll.horizontalScrollerVisibility, Is.EqualTo(ScrollerVisibility.Hidden));
            Assert.That(scroll.verticalScrollerVisibility, Is.EqualTo(ScrollerVisibility.Hidden));
            Assert.That(scroll.contentViewport.contentRect.width, Is.EqualTo(viewport.worldBound.width).Within(1));
            Assert.That(scroll.contentViewport.contentRect.height, Is.EqualTo(viewport.worldBound.height).Within(1));
        }
        finally { floating.Close(); }
    }

    [UnityTest]
    public IEnumerator EachPositiveAxisLocksIndependently()
    {
        foreach (var entry in UnityPreviewCatalog.Discover().Where(item => item.Method.DeclaringType == typeof(UnityPreviewTestContent)
                     && item.Method.Name == "Mixed"))
        {
            window.SelectPreview(entry.Id);
            yield return Settle();
            Assert.That(window.minSize.x == window.maxSize.x, Is.EqualTo(entry.Settings.Width > 0));
            Assert.That(window.minSize.y == window.maxSize.y, Is.EqualTo(entry.Settings.Height > 0));
            using var session = new UnityPreviewRenderSession(entry, 800, 600);
            session.Resize(500, 400);
            Assert.That(session.Width, Is.EqualTo(entry.Settings.Width > 0 ? entry.Settings.Width : 500));
            Assert.That(session.Height, Is.EqualTo(entry.Settings.Height > 0 ? entry.Settings.Height : 400));
        }
    }

    [UnityTest]
    public IEnumerator OversizedStaticPreviewKeepsLayoutButFitsTheScreen()
    {
        var entry = new UnityPreviewEntry(Counter().Method, new UnityPreview("Huge", 2400, 1800), 0);
        using var session = new UnityPreviewRenderSession(entry, 100, 100, 1200, 800);
        yield return Settle(); session.Capture();
        Assert.That(session.Root.resolvedStyle.width, Is.EqualTo(2400));
        Assert.That(session.Root.resolvedStyle.height, Is.EqualTo(1800));
        Assert.That(session.Width, Is.LessThanOrEqualTo(1200)); Assert.That(session.Height, Is.LessThanOrEqualTo(800));
        Assert.That(session.Root.worldBound.width, Is.EqualTo(session.Width).Within(1));
        Assert.That(session.Root.worldBound.height, Is.EqualTo(session.Height).Within(1));
    }

    [UnityTest]
    public IEnumerator AutomaticSizeFollowsNativeWindowAndKeepsRunningState()
    {
        var entry = UnityPreviewCatalog.Discover().Single(item => item.Method.DeclaringType == typeof(UnityPreviewTestContent)
            && item.Method.Name == "Responsive");
        panelSize = new Vector2(420, 340);
        window.SelectPreview(entry.Id);
        yield return Settle();
        var viewport = rootVisualElement.Q("preview.viewport");
        var available = rootVisualElement.Q("preview.host");
        Assert.That(viewport.resolvedStyle.width, Is.EqualTo(available.resolvedStyle.width).Within(.5f));
        Assert.That(viewport.resolvedStyle.height, Is.EqualTo(available.resolvedStyle.height).Within(.5f));
        window.StartPreview();
        yield return Settle();
        simulate.Click(rootVisualElement.Q("preview.counter"));
        yield return Settle();
        panelSize = new Vector2(700, 480);
        yield return Settle();
        Assert.That(window.IsRunning, Is.True);
        Assert.That(viewport.resolvedStyle.width, Is.EqualTo(available.resolvedStyle.width).Within(.5f));
        Assert.That(viewport.resolvedStyle.height, Is.EqualTo(available.resolvedStyle.height).Within(.5f));
        Assert.That(rootVisualElement.Query<Label>().ToList().Any(label => label.text.StartsWith("Count: 1")), Is.True);
        var label = rootVisualElement.Query<Label>().ToList().Single(item => item.text.StartsWith("Count:"));
        Assert.That(label.worldBound.center.x, Is.EqualTo(viewport.worldBound.center.x).Within(1));
        Assert.That(label.worldBound.center.y, Is.EqualTo(viewport.worldBound.center.y).Within(1));
        window.StopPreview();
        using var auto = new UnityPreviewRenderSession(entry, 800, 600);
        Assert.That(auto.Width, Is.EqualTo(800)); Assert.That(auto.Height, Is.EqualTo(600));
        Assert.That(auto.Resize(640, 420), Is.True);
        Assert.That(auto.Width, Is.EqualTo(640)); Assert.That(auto.Height, Is.EqualTo(420));
        auto.Capture();
        var staticLabel = auto.Root.Query<Label>().ToList().Single(item => item.text.StartsWith("Count:"));
        Assert.That(staticLabel.worldBound.center.x, Is.EqualTo(auto.Root.worldBound.center.x).Within(1));
        Assert.That(staticLabel.worldBound.center.y, Is.EqualTo(auto.Root.worldBound.center.y).Within(1));
        using var fixedSize = new UnityPreviewRenderSession(Counter(), 800, 600);
        Assert.That(fixedSize.Resize(1000, 700), Is.False);
        Assert.That(fixedSize.Width, Is.EqualTo(360)); Assert.That(fixedSize.Height, Is.EqualTo(240));
    }

    [Test]
    public void UnityProjectButtonFindsPreviewsForScriptAssets()
    {
        Assert.That(UnityPreviewProjectButton.Find("Assets/Packages/Compose.Net.Unity/Preview/Tests/Editor/UnityPreviewTests.cs").Count, Is.GreaterThan(0));
        Assert.That(UnityPreviewProjectButton.Find("Assets/Packages/Compose.Net.Unity/Preview/Editor/UnityPreviewAdapter.cs"), Is.Empty);
    }

    [Test]
    public void RiderSourceLocationResolvesPrivatePreviewAndAllVariants()
    {
        var path = System.IO.Path.GetFullPath("Assets/Packages/Compose.Net.Unity/Preview/Tests/Editor/UnityPreviewTests.cs");
        var lines = System.IO.File.ReadAllLines(path);
        var line = Array.FindIndex(lines, text => text.TrimStart().StartsWith("private static void Counter(")) + 1;
        var entries = UnityPreviewSource.Resolve(path, line);
        Assert.That(entries.Count, Is.EqualTo(2));
        Assert.That(entries.All(entry => entry.Method.Name == "Counter"), Is.True);
        Assert.That(entries.Select(entry => entry.Settings.Width), Is.EquivalentTo(new[] { 360, 640 }));
    }

    [UnityTest]
    public IEnumerator RiderRendererExportsStaticFrameWithoutAWindow()
    {
        var windows = Resources.FindObjectsOfTypeAll<EditorWindow>().Length;
        using var session = new UnityPreviewRenderSession(Counter());
        yield return Settle();
        Assert.That(Resources.FindObjectsOfTypeAll<EditorWindow>().Length, Is.EqualTo(windows));
        var png = session.Capture();
        var texture = new Texture2D(1, 1);
        try
        {
            Assert.That(texture.LoadImage(png), Is.True);
            Assert.That(texture.width, Is.EqualTo(360));
            Assert.That(texture.height, Is.EqualTo(240));
            Assert.That(texture.GetPixels32().Distinct().Count(), Is.GreaterThan(8), "The frame must contain rendered text, not a blank or transparent texture");
        }
        finally { UnityEngine.Object.DestroyImmediate(texture); }
        Assert.That(session.IsRunning, Is.False);
    }

    [UnityTest]
    public IEnumerator RiderMailboxOpensChosenAttributeAndReleasesItsRenderer()
    {
        var source = System.IO.Path.GetFullPath("Assets/Packages/Compose.Net.Unity/Preview/Tests/Editor/UnityPreviewTests.cs");
        var line = Array.FindIndex(System.IO.File.ReadAllLines(source), text => text.TrimStart().StartsWith("[UnityPreview(\"Counter — wide\"")) + 1;
        var id = Guid.NewGuid().ToString("N");
        var requestId = Guid.NewGuid().ToString();
        var inbox = System.IO.Path.GetFullPath("Library/ComposeNetPreview/requests");
        var directory = System.IO.Path.GetFullPath("Library/ComposeNetPreview/clients/" + id);
        System.IO.Directory.CreateDirectory(inbox);
        void Send(UnityPreviewRequest request)
        {
            request.clientId = id; request.requestId = requestId;
            UnityPreviewLauncher.AtomicWrite(System.IO.Path.Combine(inbox, Guid.NewGuid().ToString("N") + ".json"),
                System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(request)));
        }
        UnityPreviewFrameState? state = null;
        var existingWindows = Resources.FindObjectsOfTypeAll<UnityPreviewWindow>();
        try
        {
            Send(new UnityPreviewRequest { op = "open", sourceFile = source, line = line, refreshed = true });
            var deadline = EditorApplication.timeSinceStartup + 8;
            var path = System.IO.Path.Combine(directory, "state.json");
            while (EditorApplication.timeSinceStartup < deadline)
            {
                if (System.IO.File.Exists(path))
                {
                    state = JsonUtility.FromJson<UnityPreviewFrameState>(System.IO.File.ReadAllText(path));
                    if (!string.IsNullOrEmpty(state.error) || state.revision > 0) break;
                }
                yield return null;
            }
            Assert.That(state, Is.Not.Null);
            Assert.That(state!.error, Is.Empty);
            Assert.That(state.requestId, Is.EqualTo(requestId));
            Assert.That(state.width, Is.EqualTo(640));
            Assert.That(state.running, Is.False);
            Assert.That(state.revision, Is.GreaterThan(0));
            Assert.That(System.IO.File.Exists(System.IO.Path.Combine(directory, "frame.png")), Is.True);
            Send(new UnityPreviewRequest { op = "playInUnity", previewId = state.previewId });
            var playDeadline = EditorApplication.timeSinceStartup + 4;
            while (!Resources.FindObjectsOfTypeAll<UnityPreviewWindow>().Any(item => item.IsRunning && item.titleContent.text == "Counter — wide")
                && EditorApplication.timeSinceStartup < playDeadline) yield return null;
            Assert.That(Resources.FindObjectsOfTypeAll<UnityPreviewWindow>().Any(item => item.IsRunning && item.titleContent.text == "Counter — wide"), Is.True);
            state = JsonUtility.FromJson<UnityPreviewFrameState>(System.IO.File.ReadAllText(path));
            Assert.That(state.running, Is.False, "Rider remains static while Unity runs the interactive composition");
        }
        finally
        {
            Send(new UnityPreviewRequest { op = "release" });
            foreach (var opened in Resources.FindObjectsOfTypeAll<UnityPreviewWindow>().Except(existingWindows)) opened.Close();
        }
        var releaseDeadline = EditorApplication.timeSinceStartup + 3;
        while (System.IO.Directory.Exists(directory) && EditorApplication.timeSinceStartup < releaseDeadline) yield return null;
        Assert.That(System.IO.Directory.Exists(directory), Is.False);
    }

    [UnityTest]
    public IEnumerator RuntimePanelCanRenderWithoutAnEditorWindowOrPlayMode()
    {
        var settings = ScriptableObject.CreateInstance<PanelSettings>();
        settings.hideFlags = HideFlags.HideAndDontSave;
        settings.scaleMode = PanelScaleMode.ConstantPixelSize;
        settings.scale = 1;
        var texture = new RenderTexture(320, 200, 0);
        texture.Create();
        settings.targetTexture = texture;
        var go = new GameObject("Compose preview capture test") { hideFlags = HideFlags.HideAndDontSave };
        try
        {
            var document = go.AddComponent<UIDocument>();
            document.panelSettings = settings;
            document.rootVisualElement.Add(new Label("Capture without a Unity window"));
            EditorApplication.QueuePlayerLoopUpdate();
            for (var i = 0; i < 5; i++) yield return null;
            Assert.That(document.rootVisualElement.panel, Is.Not.Null);
            var render = document.rootVisualElement.CaptureToRenderTexture();
            try { Assert.That(render.width, Is.EqualTo(320)); Assert.That(render.height, Is.EqualTo(200)); }
            finally { render.Release(); UnityEngine.Object.DestroyImmediate(render); }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(settings);
            texture.Release(); UnityEngine.Object.DestroyImmediate(texture);
        }
    }

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

    [AiGenerated]
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


[AiGenerated]
public static partial class UnityPreviewTestContent
{
    [UnityPreview("Fixed width", 280)]
    [UnityPreview("Fixed height", -1, 180)]
    [Composable]
    private static void Mixed() => Counter();

    [UnityPreview("Responsive")]
    [Composable]
    private static void Responsive() => Counter();

    [UnityPreview("Counter", 360, 240)]
    [UnityPreview("Counter — wide", 640, 240)]
    [Composable]
    private static void Counter()
    {
        var count = RememberFunctions.Remember(() => ComposeFunctions.MutableStateOf(0));
        BoxFunctions.Box(modifier: ComposeFunctions.Modifier.FillMaxSize().TestTag("preview.counter")
                .Clickable(() => count.Value++), contentAlignment: Alignment.Center,
            content: () => TextFunctions.Text($"Count: {count.Value} — click to increment"));
    }
}
