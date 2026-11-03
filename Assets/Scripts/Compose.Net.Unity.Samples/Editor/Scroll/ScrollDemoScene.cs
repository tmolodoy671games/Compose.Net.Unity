using System;
using System.Linq;
using Compose.Net.Unity.Samples.Behaviors.Content;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Samples.Editor;

public static class ScrollDemoScene
{
    [MenuItem("Tools/Compose.Net/Add Scroll Demo to Scene")]
    public static void AddToScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded) throw new InvalidOperationException("Open a scene first.");
        var root = GameObject.Find("Samples")?.transform;
        var existing = root?.Find("ScrollDemo") ?? scene.GetRootGameObjects().FirstOrDefault(item => item.name == "ScrollDemo")?.transform;
        var go = existing != null ? existing.gameObject : new GameObject("ScrollDemo");
        if (existing == null) { Undo.RegisterCreatedObjectUndo(go, "Add Scroll demo"); go.transform.SetParent(root, false); }
        var settings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/Settings/Panel Settings.asset");
        var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Packages/Compose.Net.Unity/Runtime/Layout/ComposeView Root.uxml");
        var renderer = go.GetComponent<PanelRenderer>();
        var document = go.GetComponent<UIDocument>();
        if (renderer != null || document == null)
        {
            renderer ??= Undo.AddComponent<PanelRenderer>(go);
            renderer.panelSettings = settings; renderer.visualTreeAsset = asset; renderer.sortingOrder = 30;
        }
        else
        {
            document.panelSettings = settings; document.visualTreeAsset = asset; document.sortingOrder = 30;
        }
        if (go.GetComponent<ScrollDemo>() == null) Undo.AddComponent<ScrollDemo>(go);
        go.SetActive(true);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); Selection.activeGameObject = go;
    }
}
