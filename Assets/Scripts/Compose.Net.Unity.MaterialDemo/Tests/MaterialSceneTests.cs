using Compose.Net.Unity.MaterialDemo.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.MaterialDemo.Tests;

public sealed class MaterialSceneTests
{
    [Test] public void SceneInstallationIsIdempotentAndUsesExistingPanelAssets()
    {
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var first = MaterialDemoScene.EnsureInScene(scene);
            var second = MaterialDemoScene.EnsureInScene(scene);
            Assert.That(second, Is.SameAs(first));
            Assert.That(first.transform.parent.name, Is.EqualTo("Demos"));
            Assert.That(first.gameObject.activeSelf && first.enabled, Is.True);
            var panel = first.GetComponent<PanelRenderer>();
            Assert.That(panel.panelSettings, Is.Not.Null);
            Assert.That(panel.visualTreeAsset, Is.Not.Null);
            Assert.That(panel.sortingOrder, Is.EqualTo(210));
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }
}
