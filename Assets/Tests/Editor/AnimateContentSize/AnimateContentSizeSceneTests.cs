using System.Collections;
using Compose.Net.Unity.Samples.Behaviors.Content;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Samples.Tests;

[AiGenerated]
public sealed class AnimateContentSizeSceneTests
{
    private const string DemoAsset = "Assets/Scripts/Compose.Net.Unity.Samples/Behaviors/Content/AnimateContentSizeDemo";

    [Test]
    public void ComponentScriptIsRecognizedByUnityImporter()
    {
        var script = AssetDatabase.LoadAssetAtPath<MonoScript>(DemoAsset + ".cs");
        Assert.That(script, Is.Not.Null);
        Assert.That(script.GetClass(), Is.EqualTo(typeof(AnimateContentSizeDemo)),
            "The compiled type must also be resolved by MonoImporter, otherwise scene components become Missing Script.");
    }

    [Test]
    public void FileScopedDemoScriptSurvivesReimport()
    {
        AssetDatabase.ImportAsset(DemoAsset + ".cs", ImportAssetOptions.ForceUpdate);
        Assert.That(AssetDatabase.LoadAssetAtPath<MonoScript>(DemoAsset + ".cs").GetClass(),
            Is.EqualTo(typeof(AnimateContentSizeDemo)));
    }

    [Test]
    public void FileScopedEditorWindowScriptIsRecognized()
    {
        var script = AssetDatabase.LoadAssetAtPath<MonoScript>(
            "Assets/Scripts/Compose.Net.Unity.Samples/Editor/AnimateContentSizeDemoWindow.cs");
        Assert.That(script.GetClass(), Is.Not.Null);
        Assert.That(script.GetClass().IsSubclassOf(typeof(EditorWindow)), Is.True);
    }

    [Test]
    public void FileScopedScriptableObjectSurvivesAssetSerialization()
    {
        var script = AssetDatabase.LoadAssetAtPath<MonoScript>(
            "Assets/Tests/Editor/AnimateContentSize/FileScopedNamespaceAsset.cs");
        Assert.That(script.GetClass(), Is.EqualTo(typeof(FileScoped.FileScopedNamespaceAsset)));
        var path = AssetDatabase.GenerateUniqueAssetPath("Assets/AnimateContentSizeNamespaceTest.asset");
        try
        {
            var asset = ScriptableObject.CreateInstance<FileScoped.FileScopedNamespaceAsset>();
            asset.Value = 37;
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            Resources.UnloadAsset(asset);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var restored = AssetDatabase.LoadAssetAtPath<FileScoped.FileScopedNamespaceAsset>(path);
            Assert.That(restored, Is.Not.Null);
            Assert.That(restored.Value, Is.EqualTo(37));
            Assert.That(MonoScript.FromScriptableObject(restored), Is.EqualTo(script));
        }
        finally { AssetDatabase.DeleteAsset(path); }
    }

    [Test]
    public void SceneUxmlContainsNativeScrollViewAndComposeView()
    {
        var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(DemoAsset + ".uxml");
        Assert.That(asset, Is.Not.Null);
        var tree = asset.CloneTree();
        var scroll = tree.Q<ScrollView>("animate-size-scroll");
        Assert.That(scroll, Is.Not.Null);
        var view = scroll.Q<ComposeView>("compose-view");
        Assert.That(view, Is.Not.Null);
        view.Dispose();
    }

    [UnityTest]
    public IEnumerator UIDocumentMountsDemoAndRemountsAfterDisable() => VerifySceneHost(false);

    [UnityTest]
    public IEnumerator PanelRendererMountsDemoAndRemountsAfterDisable() => VerifySceneHost(true);

    [UnityTest]
    public IEnumerator PanelRendererRemountsWhenOnlyComposeComponentIsDisabled() => VerifySceneHost(true, true);

    private IEnumerator VerifySceneHost(bool usePanelRenderer, bool componentOnly = false)
    {
        var go = new GameObject("AnimateContentSizeSceneTest") { hideFlags = HideFlags.HideAndDontSave };
        go.SetActive(false);
        try
        {
            var settings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/Settings/Panel Settings.asset");
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(DemoAsset + ".uxml");
            Assert.That(settings, Is.Not.Null);
            Assert.That(asset, Is.Not.Null);
            VisualElement? tree = null;
            UIDocument? document = null;
            if (usePanelRenderer)
            {
                var renderer = go.AddComponent<PanelRenderer>();
                renderer.panelSettings = settings;
                renderer.visualTreeAsset = asset;
                renderer.RegisterUIReloadCallback((PanelRenderer _, VisualElement root, int version) => tree = root);
            }
            else
            {
                document = go.AddComponent<UIDocument>();
                document.panelSettings = settings;
                document.visualTreeAsset = asset;
            }
            var demo = go.AddComponent<AnimateContentSizeDemo>();
            for (var attempt = 0; attempt < 2; attempt++)
            {
                go.SetActive(true);
                demo.enabled = true;
                // PanelRenderer attaches its tree on a player-loop update, not synchronously
                // from SetActive. EditMode tests must request that loop even with Game hidden.
                for (var frame = 0; frame < 60; frame++)
                {
                    EditorApplication.QueuePlayerLoopUpdate();
                    yield return null;
                    ComposeInvalidatorHolder.ComposeInvalidator.Tick();
                    if (document != null) tree = document.rootVisualElement;
                    if (tree?.Q("animate-size-demo") != null) break;
                }
                Assert.That(tree, Is.Not.Null, $"UI host must attach its tree (enable {attempt + 1}).");
                Assert.That(tree!.Q("animate-size-demo"), Is.Not.Null, $"Scene component must mount its content (enable {attempt + 1}).");
                var state = new AnimateContentSizeDemoState();
                foreach (var item in state.Cases)
                    Assert.That(tree!.Q("animate." + item.Id + ".target"), Is.Not.Null, item.Id);
                if (componentOnly) demo.enabled = false;
                else go.SetActive(false);
                yield return null;
            }
        }
        finally { Object.DestroyImmediate(go); }
    }
}
