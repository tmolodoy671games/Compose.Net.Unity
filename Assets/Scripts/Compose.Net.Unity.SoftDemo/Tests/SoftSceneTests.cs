using System.Collections;
using Compose.Net.Unity.SoftDemo.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Compose.Net.Unity.SoftDemo.Tests;

public sealed class SoftSceneTests
{
    [Test] public void SceneConnectionIsCompleteAndIdempotent()
    {
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var first = SoftDemoScene.EnsureInScene(scene);
            Assert.That(SoftDemoScene.EnsureInScene(scene), Is.SameAs(first));
            Assert.That(first.transform.parent.name, Is.EqualTo("Demos"));
            Assert.That(first.GetComponent<PanelRenderer>().panelSettings, Is.Not.Null);
            Assert.That(first.GetComponent<PanelRenderer>().visualTreeAsset, Is.Not.Null);
            Assert.That(first.gameObject.activeInHierarchy, Is.True);
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    private sealed class Fixture : RuntimeUITestFixture { }

    [UnityTest] public IEnumerator SceneComponentAnimatesAcceptsClicksAndRemountsInPlayMode()
    {
        yield return new EnterPlayMode();
        var input = new Fixture(); input.FixtureOneTimeSetUp(); input.FixtureSetUp();
        var go = new GameObject("SOFT runtime validation"); go.SetActive(false);
        var settings = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/Settings/Panel Settings.asset"));
        var texture = new RenderTexture(1240, 940, 0); texture.Create();
        settings.targetTexture = texture; settings.scaleMode = PanelScaleMode.ConstantPixelSize; settings.scale = 1;
        try
        {
            VisualElement? tree = null;
            var renderer = go.AddComponent<PanelRenderer>(); renderer.panelSettings = settings;
            renderer.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Packages/Compose.Net.Unity/Runtime/Layout/ComposeView Root.uxml");
            renderer.RegisterUIReloadCallback((PanelRenderer _, VisualElement root, int version) => tree = root);
            var demo = go.AddComponent<SoftDemo>(); go.SetActive(true);
            var until = Time.realtimeSinceStartup + 5;
            while (tree?.Q("phone.1.practice") == null && Time.realtimeSinceStartup < until) yield return null;
            Assert.That(tree?.Q("phone.1.practice"), Is.Not.Null);
            input.SetPanelRenderer(renderer); input.panelSize = new Vector2(1240, 940); input.simulate.needsRendering = true;
            IEnumerator Wait(float seconds)
            { var end = Time.realtimeSinceStartup + seconds; while (Time.realtimeSinceStartup < end) { yield return null; input.simulate.FrameUpdate(); } }
            yield return Wait(.5f);
            var phase = demo.State.Phase.Value; yield return Wait(.3f); Assert.That(demo.State.Phase.Value, Is.Not.EqualTo(phase));
            input.simulate.Click(tree!.Q("phone.1.practice")); yield return Wait(1.25f);
            Assert.That(demo.State.Remaining.Value, Is.LessThan(60));
            go.SetActive(false); yield return null; go.SetActive(true); yield return Wait(.7f);
            Assert.That(tree.Q("phone.2.nav.2"), Is.Not.Null);
            input.simulate.Click(tree.Q("phone.2.mood.1")); yield return Wait(.3f);
            Assert.That(demo.State.Mood.Value, Is.EqualTo(1));
        }
        finally
        {
            input.FixtureTearDown(); input.FixtureOneTimeTearDown();
            Object.DestroyImmediate(go); Object.DestroyImmediate(settings); texture.Release(); Object.DestroyImmediate(texture);
        }
        yield return new ExitPlayMode();
    }
}
