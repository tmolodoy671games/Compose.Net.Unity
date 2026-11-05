using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.SoftDemo.Editor;

public static class SoftDemoScene
{
    // Explicit entry point for the installer/validation runner; safe to call again.
    public static void InstallInSampleScene()
    {
        const string path = "Assets/Scenes/SampleScene.unity";
        var scene = SceneManager.GetSceneByPath(path);
        if (!scene.IsValid() || !scene.isLoaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        Selection.activeGameObject = EnsureInScene(scene).gameObject;
        if (!EditorSceneManager.SaveScene(scene)) throw new System.InvalidOperationException("Could not save " + path);
    }

    public static void AddToScene() => Selection.activeGameObject = EnsureInScene(SceneManager.GetActiveScene()).gameObject;

    public static SoftDemo EnsureInScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded) throw new System.InvalidOperationException("Open a scene first.");
        var roots = scene.GetRootGameObjects();
        var demos = roots.FirstOrDefault(root => root.name == "Demos");
        if (!demos)
        {
            demos = new GameObject("Demos");
            SceneManager.MoveGameObjectToScene(demos, scene);
            Undo.RegisterCreatedObjectUndo(demos, "Add demo group");
        }
        var existing = roots.SelectMany(root => root.GetComponentsInChildren<SoftDemo>(true)).FirstOrDefault();
        var go = existing ? existing.gameObject : roots.SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Select(transform => transform.gameObject).FirstOrDefault(item => item.name == "SOFT — Moments");
        if (!go)
        {
            go = new GameObject("SOFT — Moments");
            SceneManager.MoveGameObjectToScene(go, scene);
            Undo.RegisterCreatedObjectUndo(go, "Add SOFT demo");
        }
        Undo.RecordObject(go, "Configure SOFT demo");
        go.SetActive(false);
        Undo.SetTransformParent(go.transform, demos.transform, "Move SOFT under Demos");
        var renderer = go.GetComponent<PanelRenderer>();
        if (!renderer) renderer = Undo.AddComponent<PanelRenderer>(go);
        Undo.RecordObject(renderer, "Configure SOFT panel");
        renderer.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/Settings/Panel Settings.asset");
        renderer.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Packages/Compose.Net.Unity/Runtime/Layout/ComposeView Root.uxml");
        renderer.sortingOrder = 200;
        if (!existing) existing = Undo.AddComponent<SoftDemo>(go);
        Undo.RecordObject(existing, "Enable SOFT demo"); existing.enabled = true;
        go.SetActive(true); EditorSceneManager.MarkSceneDirty(scene);
        return existing;
    }
}
