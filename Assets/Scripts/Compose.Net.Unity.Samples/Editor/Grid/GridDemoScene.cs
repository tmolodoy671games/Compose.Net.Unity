using System;
using System.Linq;
using Compose.Net.Unity.Samples.Behaviors.Content;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Samples.Editor;

public static class GridDemoScene
{
    [MenuItem("Tools/Compose.Net/Add Grid Demo to Scene")]
    public static void AddToScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded) throw new InvalidOperationException("Open a scene first.");
        var roots = scene.GetRootGameObjects();
        // Include inactive demo groups and reuse an existing component anywhere in the scene.
        var root = (roots.FirstOrDefault(item => item.name == "Demos") ??
                    roots.FirstOrDefault(item => item.name == "Samples"))?.transform;
        var existing = roots.SelectMany(item => item.GetComponentsInChildren<GridDemo>(true))
            .FirstOrDefault()?.transform;
        var go = existing != null ? existing.gameObject : new GameObject("GridDemo");
        if (existing == null) { Undo.RegisterCreatedObjectUndo(go, "Add Grid demo"); go.transform.SetParent(root, false); }
        var settings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/Settings/Panel Settings.asset");
        var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Packages/Compose.Net.Unity/Runtime/Layout/ComposeView Root.uxml");
        var renderer = go.GetComponent<PanelRenderer>();
        var document = go.GetComponent<UIDocument>();
        if (renderer != null || document == null)
        {
            renderer ??= Undo.AddComponent<PanelRenderer>(go);
            renderer.panelSettings = settings; renderer.visualTreeAsset = asset; renderer.sortingOrder = 60;
        }
        else
        {
            document.panelSettings = settings; document.visualTreeAsset = asset; document.sortingOrder = 60;
        }
        if (go.GetComponent<GridDemo>() == null) Undo.AddComponent<GridDemo>(go);
        go.SetActive(true);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); Selection.activeGameObject = go;
    }
}
