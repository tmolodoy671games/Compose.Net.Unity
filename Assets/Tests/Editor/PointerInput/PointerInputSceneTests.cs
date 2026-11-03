using System.Collections;
using Compose.Net.Unity.Samples.Behaviors.Content;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Tests;

public sealed class PointerInputSceneTests
{
    [UnityTest] public IEnumerator UIDocumentMountsAndRemountsGallery() => Host(false, false);
    [UnityTest] public IEnumerator PanelRendererMountsAndRemountsGallery() => Host(true, false);
    [UnityTest] public IEnumerator DisablingOnlyDemoComponentReleasesAndRestoresGallery() => Host(true, true);

    private static IEnumerator Host(bool nativeRenderer, bool componentOnly)
    {
        var go = new GameObject("PointerInputSceneTest") { hideFlags = HideFlags.HideAndDontSave };
        go.SetActive(false);
        try
        {
            var settings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/Settings/Panel Settings.asset");
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Packages/Compose.Net.Unity/Runtime/Layout/ComposeView Root.uxml");
            Assert.That(settings, Is.Not.Null); Assert.That(asset, Is.Not.Null);
            VisualElement? tree = null; UIDocument? document = null;
            if (nativeRenderer)
            {
                var renderer = go.AddComponent<PanelRenderer>(); renderer.panelSettings = settings; renderer.visualTreeAsset = asset;
                renderer.RegisterUIReloadCallback((PanelRenderer _, VisualElement root, int version) => tree = root);
            }
            else
            {
                document = go.AddComponent<UIDocument>(); document.panelSettings = settings; document.visualTreeAsset = asset;
            }
            var demo = go.AddComponent<PointerInputDemo>();
            for (var attempt = 0; attempt < 2; attempt++)
            {
                go.SetActive(true); demo.enabled = true;
                for (var frame = 0; frame < 60; frame++)
                {
                    EditorApplication.QueuePlayerLoopUpdate(); yield return null;
                    ComposeInvalidatorHolder.ComposeInvalidator.Tick();
                    if (document != null) tree = document.rootVisualElement;
                    if (tree?.Q("pointer-demo") != null) break;
                }
                Assert.That(tree?.Q("pointer-demo"), Is.Not.Null, "Scene component must mount its gallery");
                foreach (var id in PointerInputDemoState.Ids) Assert.That(tree!.Q("pointer." + id), Is.Not.Null, id);
                if (componentOnly) demo.enabled = false; else go.SetActive(false);
                yield return null;
            }
        }
        finally { Object.DestroyImmediate(go); }
    }
}
