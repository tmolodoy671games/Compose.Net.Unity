using System;
using System.Collections;
using System.Linq;
using Compose.Net.Unity.Showcase.Editor;
using Compose.Net.Unity.Packages.Compose.Net.Unity.Runtime.Adapters;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements.TestFramework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;
using Object = UnityEngine.Object;

namespace Compose.Net.Unity.Showcase.Tests;

public sealed partial class OrbitWindowTests : EditorWindowUITestFixture<OrbitDemoWindow>
{
    // The Unity fixture normally reuses its window between successful tests.
    // Each scenario needs a fresh app session, including its theme and timer.
    [SetUp] public void ResetWindow() => RecreatePanel();

    private IEnumerator Wait(float seconds = .7f)
    {
        var deadline = EditorApplication.timeSinceStartup + seconds;
        while (EditorApplication.timeSinceStartup < deadline) { yield return null; simulate.FrameUpdate(); }
    }
    private void Click(string tag)
    {
        var target = rootVisualElement.Q(tag); Assert.That(target, Is.Not.Null, tag);
        simulate.Click(target);
    }
    [UnityTest] public IEnumerator LiveEditorWindowNavigatesTypesAndTicksWithoutPlayMode()
    {
        panelSize = new Vector2(1280,900); yield return Wait();
        foreach (var page in new[] { "Projects", "Focus", "Settings", "Overview", "Motion", "Projects" })
        { Click("nav." + page); yield return Wait(); Assert.That(rootVisualElement.Q("screen." + page), Is.Not.Null); }
        Click("project.new"); yield return Wait();
        var field = rootVisualElement.Q("project.name").Q<TextField>(); simulate.Click(field); simulate.TypingText("Live editor project");
        yield return Wait(.1f); Click("project.create"); yield return Wait();
        Assert.That(window.State.Projects.Last().Name, Is.EqualTo("Live editor project"));
        Click("nav.Focus"); yield return Wait(); Click("focus.toggle"); yield return Wait(1.25f);
        Assert.That(window.State.FocusSeconds.Value, Is.LessThan(1500));
        Click("focus.toggle"); var seconds = window.State.FocusSeconds.Value;
        yield return Wait(1.1f); Assert.That(window.State.FocusSeconds.Value, Is.EqualTo(seconds));
        Click("theme.toggle"); yield return Wait();
        Assert.That(rootVisualElement.Q("orbit.root").resolvedStyle.backgroundColor.r, Is.GreaterThan(.9f));
    }
    [UnityTest] public IEnumerator ClosingAnotherWindowDoesNotFreezeInFlightAnimation()
    {
        panelSize = new Vector2(1280,900); yield return Wait();
        var other = ScriptableObject.CreateInstance<OrbitDemoWindow>();
        try
        {
            other.Show(); yield return Wait(.2f);
            Click("theme.toggle"); yield return Wait(.15f);
            other.Close(); yield return Wait(.8f);
            Assert.That(rootVisualElement.Q("orbit.root").resolvedStyle.backgroundColor.r, Is.GreaterThan(.9f), "Closing another host must not stop this composition's clock");
        }
        finally { if (other) other.Close(); }
    }

    [UnityTest] public IEnumerator ScenePreviewCannotStealEditorParticleClock()
    {
        panelSize = new Vector2(1280,900); yield return Wait();
        // Scene previews bootstrap the native adapter even while an editor window
        // is open. Its MonoBehaviour coroutine clock does not run in Edit Mode.
        using var preview = IComposer.Create();
        ComposeFunctions.Bootstrap(preview, ComposeUnityAdapter.Instance, true, new ComposeConfig(Logging: false),
            new PreviewRoot(new VisualElement()), EmptyPreview);
        Click("nav.Motion"); yield return Wait(1.5f);
        var canvas = rootVisualElement.Q("motion.canvas"); Assert.That(canvas, Is.Not.Null);
        var before = OrbitPixels.Read(canvas); yield return Wait(.7f);
        Assert.That(OrbitPixels.Changed(before, OrbitPixels.Read(canvas)), Is.GreaterThan(100), "Scene previews must not freeze editor particle loops");
        Click("motion.pause"); yield return Wait(.2f);
        var paused = OrbitPixels.Read(canvas); yield return Wait(.4f);
        Assert.That(OrbitPixels.Changed(paused, OrbitPixels.Read(canvas)), Is.Zero);
        Click("motion.pause"); yield return Wait(.7f);
        Assert.That(OrbitPixels.Changed(paused, OrbitPixels.Read(canvas)), Is.GreaterThan(100));
    }

    [Composable] private void EmptyPreview() => ComposeFunctions.Box(ComposeFunctions.Modifier);
    private sealed class PreviewRoot(VisualElement element) : UnityReusableComposeNode(element), IReusableComposeNode
    { public new bool IsRoot => true; }
}

public sealed class OrbitSceneTests
{
    private sealed class RuntimeInputFixture : RuntimeUITestFixture { }
    [Test] public void AddToSceneRecoversIncompleteObjectAndIsIdempotentUnderInactiveDemos()
    {
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var demos = new GameObject("Demos"); SceneManager.MoveGameObjectToScene(demos, scene); demos.SetActive(false);
            var incomplete = new GameObject("ORBIT — Compose.Net Showcase"); SceneManager.MoveGameObjectToScene(incomplete, scene); incomplete.transform.SetParent(demos.transform);
            var other = new GameObject("Keep me"); SceneManager.MoveGameObjectToScene(other, scene);
            var demo = OrbitDemoScene.EnsureInScene(scene);
            Assert.That(demo.gameObject, Is.SameAs(incomplete));
            Assert.That(demo.transform.parent, Is.EqualTo(demos.transform));
            Assert.That(demo.GetComponent<PanelRenderer>().panelSettings, Is.Not.Null);
            Assert.That(demo.GetComponent<PanelRenderer>().visualTreeAsset, Is.Not.Null);
            demo.name = "Renamed ORBIT"; demo.enabled = false;
            Assert.That(OrbitDemoScene.EnsureInScene(scene), Is.SameAs(demo));
            Assert.That(demo.enabled, Is.True);
            Assert.That(demos.GetComponentsInChildren<OrbitDemo>(true).Length, Is.EqualTo(1));
            Assert.That(other.scene, Is.EqualTo(scene)); Assert.That(demos.activeSelf, Is.False);
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    [UnityTest] public IEnumerator SceneRunsInPlayModeWithRealClockAndRemounts()
    {
        yield return new EnterPlayMode();
        var inputFixture = new RuntimeInputFixture();
        inputFixture.FixtureOneTimeSetUp(); inputFixture.FixtureSetUp();
        var go = new GameObject("ORBIT runtime test"); go.SetActive(false);
        var settings = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/Settings/Panel Settings.asset"));
        var texture = new RenderTexture(1280,900,0); texture.Create();
        settings.targetTexture = texture; settings.scaleMode = PanelScaleMode.ConstantPixelSize; settings.scale = 1;
        try
        {
            VisualElement? tree = null;
            var renderer = go.AddComponent<PanelRenderer>(); renderer.panelSettings = settings;
            renderer.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Packages/Compose.Net.Unity/Runtime/Layout/ComposeView Root.uxml");
            renderer.RegisterUIReloadCallback((PanelRenderer _, VisualElement root, int version) => tree = root);
            var demo = go.AddComponent<OrbitDemo>(); go.SetActive(true);
            var deadline = Time.realtimeSinceStartup + 5;
            while (tree?.Q("nav.Projects") == null && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(tree?.Q("nav.Projects"), Is.Not.Null);
            inputFixture.SetPanelRenderer(renderer);
            inputFixture.panelSize = new Vector2(1280,900);
            var input = inputFixture.simulate; input.needsRendering = true;
            IEnumerator Settle(float duration = .8f)
            { var end = Time.realtimeSinceStartup + duration; while (Time.realtimeSinceStartup < end) { yield return null; input.FrameUpdate(); } }
            yield return Settle();
            foreach (var page in new[] { "Projects", "Activity", "Settings", "Motion", "Focus" })
            {
                input.Click(tree!.Q("nav." + page)); yield return Settle();
                Assert.That(demo.State.Route.Page.ToString(), Is.EqualTo(page));
                Assert.That(tree.Q("screen." + page).worldBound.xMax, Is.EqualTo(tree.Q("orbit.root").worldBound.xMax).Within(2));
                if (page == "Motion")
                {
                    var frame = ((IComposeAdapter)ComposeUnityAdapter.Instance).FrameClock.NextFrame(default);
                    var canvas = tree.Q("motion.canvas");
                    var draws = 0;
                    var layers = canvas.Query<VisualElement>().ToList().Where(element => element.GetType().Name == "DrawBehind").ToArray();
                    Assert.That(layers.Length, Is.GreaterThan(0));
                    foreach (var layer in layers) layer.generateVisualContent += _ => draws++;
                    yield return Settle(.7f);
                    Assert.That(frame.IsCompleted, Is.True, "The runtime adapter must deliver frames in Play Mode");
                    Assert.That(draws, Is.GreaterThan(2), "Particle motion must invalidate and regenerate the real runtime drawing");
                    input.Click(tree.Q("motion.pause")); yield return Settle(.3f);
                    var paused = draws; yield return Settle(.4f);
                    Assert.That(draws, Is.EqualTo(paused), "Pausing must stop rebuilding the particle drawing");
                    input.Click(tree.Q("motion.pause")); yield return Settle(.5f);
                    Assert.That(draws, Is.GreaterThan(paused + 2));
                }
            }
            input.Click(tree!.Q("focus.toggle")); yield return Settle(1.25f);
            Assert.That(demo.State.FocusSeconds.Value, Is.LessThan(1500));
            input.Click(tree.Q("theme.toggle")); yield return Settle();
            Assert.That(tree.Q("orbit.root").resolvedStyle.backgroundColor.r, Is.GreaterThan(.9f));
            go.SetActive(false); yield return null; go.SetActive(true); yield return Settle();
            Assert.That(tree.Q("nav.Projects"), Is.Not.Null);
            input.Click(tree.Q("nav.Projects")); yield return Settle(); Assert.That(demo.State.Route.Page.ToString(), Is.EqualTo("Projects"));
        }
        finally
        {
            inputFixture.FixtureTearDown(); inputFixture.FixtureOneTimeTearDown();
            Object.DestroyImmediate(go); Object.DestroyImmediate(settings); texture.Release(); Object.DestroyImmediate(texture);
        }
        yield return new ExitPlayMode();
    }

    [UnityTest] public IEnumerator RuntimeFramesSurviveCoroutineShutdown()
    {
        yield return new EnterPlayMode();
        using var cancellation = new System.Threading.CancellationTokenSource();
        var clock = ((IComposeAdapter)ComposeUnityAdapter.Instance).FrameClock;
        var frame = clock.NextFrame(cancellation.Token);
        // Unity stops MonoBehaviour coroutines at host/Play Mode boundaries.
        // Frame delivery must recover while the player loop is still running.
        var holders = Resources.FindObjectsOfTypeAll<ComposeInvalidatorHolder>();
        Assert.That(holders.Length, Is.GreaterThan(0));
        foreach (var holder in holders) holder.StopAllCoroutines();
        var until = Time.realtimeSinceStartup + 2;
        while (!frame.IsCompleted && Time.realtimeSinceStartup < until) yield return null;
        Assert.That(frame.IsCompletedSuccessfully, Is.True, "Runtime frames must not depend on a surviving coroutine");
        yield return new ExitPlayMode();
    }
}
