using System;
using System.Linq;
using Compose.Net.Unity.Samples.Behaviors.Content;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Samples.Editor;

public static class InteractionDemoScene
{
    [MenuItem("Tools/Compose.Net/Add Interaction Demo to Scene")]
    public static void AddToScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded) throw new InvalidOperationException("Open a scene first.");
        var root = GameObject.Find("Samples")?.transform;
        var existing = root?.Find("InteractionDemo") ?? scene.GetRootGameObjects().FirstOrDefault(item => item.name == "InteractionDemo")?.transform;
        var go = existing != null ? existing.gameObject : new GameObject("InteractionDemo");
        if (existing == null) { Undo.RegisterCreatedObjectUndo(go, "Add Interaction demo"); go.transform.SetParent(root, false); }
        var settings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/Settings/Panel Settings.asset");
        var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Packages/Compose.Net.Unity/Runtime/Layout/ComposeView Root.uxml");
        var renderer = go.GetComponent<PanelRenderer>();
        var document = go.GetComponent<UIDocument>();
        if (renderer != null || document == null)
        {
            renderer ??= Undo.AddComponent<PanelRenderer>(go);
            renderer.panelSettings = settings; renderer.visualTreeAsset = asset; renderer.sortingOrder = 40;
        }
        else
        {
            document.panelSettings = settings; document.visualTreeAsset = asset; document.sortingOrder = 40;
        }
        if (go.GetComponent<InteractionDemo>() == null) Undo.AddComponent<InteractionDemo>(go);
        go.SetActive(true);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); Selection.activeGameObject = go;
    }
}
