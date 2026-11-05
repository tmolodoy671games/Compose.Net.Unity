using System;
using System.Linq;
using Compose.Net.Unity.Samples.Behaviors.Content;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Samples.Editor;

public static class BoxDemoScene
{
    public static void AddToScene()
    {
        var scene = SceneManager.GetActiveScene();
        var go = EnsureInScene(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!string.IsNullOrEmpty(scene.path)) EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = go;
    }

    public static GameObject EnsureInScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded) throw new InvalidOperationException("Open a scene first.");
        var roots = scene.GetRootGameObjects();
        var samples = roots.FirstOrDefault(root => root.name == "Samples")?.transform;
        var existing = roots.SelectMany(root => root.GetComponentsInChildren<BoxDemo>(true)).FirstOrDefault()?.transform
            ?? samples?.Find("BoxDemo") ?? roots.FirstOrDefault(root => root.name == "BoxDemo")?.transform;
        var go = existing != null ? existing.gameObject : new GameObject("BoxDemo");
        if (existing == null)
        {
            Undo.RegisterCreatedObjectUndo(go, "Add Box demo");
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.SetParent(samples, false);
        }
        var settings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/Settings/Panel Settings.asset");
        var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Packages/Compose.Net.Unity/Runtime/Layout/ComposeView Root.uxml");
        var renderer = go.GetComponent<PanelRenderer>();
        var document = go.GetComponent<UIDocument>();
        if (renderer != null || document == null)
        {
            if (renderer == null) renderer = Undo.AddComponent<PanelRenderer>(go);
            Undo.RecordObject(renderer,"Configure Box demo");
            renderer.panelSettings = settings; renderer.visualTreeAsset = asset; renderer.sortingOrder = 40;
        }
        else
        {
            Undo.RecordObject(document,"Configure Box demo");
            document.panelSettings = settings; document.visualTreeAsset = asset; document.sortingOrder = 40;
        }
        if (go.GetComponent<BoxDemo>() == null) Undo.AddComponent<BoxDemo>(go);
        go.SetActive(true);
        return go;
    }
}
