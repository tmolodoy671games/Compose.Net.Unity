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
        var settings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/Settings/Panel Settings.asset");
        var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
            "Assets/Scripts/Compose.Net.Unity.Samples/Behaviors/Content/AnimateContentSizeDemo.uxml");
        var renderer = go.GetComponent<PanelRenderer>();
        var document = go.GetComponent<UIDocument>();
        if (renderer != null || document == null)
        {
            renderer ??= Undo.AddComponent<PanelRenderer>(go);
            renderer.panelSettings = settings;
            renderer.visualTreeAsset = asset;
            renderer.sortingOrder = 10;
        }
        else
        {
            document.panelSettings = settings;
            document.visualTreeAsset = asset;
            document.sortingOrder = 10;
        }
        if (go.GetComponent<AnimateContentSizeDemo>() == null) Undo.AddComponent<AnimateContentSizeDemo>(go);
        go.name = "AnimateContentSizeDemo";
        go.SetActive(true);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = go;
    }
}
