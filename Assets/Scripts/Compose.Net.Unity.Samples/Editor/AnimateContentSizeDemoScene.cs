using System;
using Compose.Net;
using Compose.Net.Unity.Samples.Behaviors.Content;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Samples.Editor;

[AiGenerated]
public static class AnimateContentSizeDemoScene
{
    [MenuItem("Tools/Compose.Net/Add Animate Content Size Demo to Scene")]
    public static void AddToScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded) throw new InvalidOperationException("Open a scene first.");
        var root = GameObject.Find("Samples")?.transform;
        var existing = root != null ? root.Find("AnimateContentSizeDemo") ?? root.Find("Animate Content Size Demo") : null;
        var go = existing != null ? existing.gameObject : new GameObject("AnimateContentSizeDemo");
        if (existing == null)
        {
            Undo.RegisterCreatedObjectUndo(go, "Add AnimateContentSize demo");
            go.transform.SetParent(root, false);
        }
        var document = go.GetComponent<UIDocument>() ?? Undo.AddComponent<UIDocument>(go);
        document.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/Settings/Panel Settings.asset");
        document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
            "Assets/Scripts/Compose.Net.Unity.Samples/Behaviors/Content/AnimateContentSizeDemo.uxml");
        document.sortingOrder = 10;
        if (go.GetComponent<AnimateContentSizeDemo>() == null) Undo.AddComponent<AnimateContentSizeDemo>(go);
        go.name = "AnimateContentSizeDemo";
        go.SetActive(true);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = go;
    }
}
