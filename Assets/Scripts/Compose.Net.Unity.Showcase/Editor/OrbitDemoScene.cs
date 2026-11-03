using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Showcase.Editor;

public static class OrbitDemoScene
{
    [MenuItem("Tools/Compose.Net/Add ORBIT Demo to Scene")]
    public static void AddToScene() => Selection.activeGameObject = EnsureInScene(SceneManager.GetActiveScene()).gameObject;

    public static OrbitDemo EnsureInScene(Scene scene)
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
        var existing = roots.SelectMany(root => root.GetComponentsInChildren<OrbitDemo>(true)).FirstOrDefault();
        var go = existing ? existing.gameObject : roots.SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Select(transform => transform.gameObject).FirstOrDefault(item => item.name == "ORBIT — Compose.Net Showcase");
        if (!go)
        {
            go = new GameObject("ORBIT — Compose.Net Showcase");
            SceneManager.MoveGameObjectToScene(go, scene);
            Undo.RegisterCreatedObjectUndo(go, "Add ORBIT showcase");
        }
        Undo.RecordObject(go, "Configure ORBIT showcase");
        go.SetActive(false);
        Undo.SetTransformParent(go.transform, demos.transform, "Move ORBIT under Demos");
        var renderer = go.GetComponent<PanelRenderer>();
        if (!renderer) renderer = Undo.AddComponent<PanelRenderer>(go);
        Undo.RecordObject(renderer, "Configure ORBIT panel");
        renderer.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/Settings/Panel Settings.asset");
        renderer.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Packages/Compose.Net.Unity/Runtime/Layout/ComposeView Root.uxml");
        renderer.sortingOrder = 100;
        if (!existing) existing = Undo.AddComponent<OrbitDemo>(go);
        Undo.RecordObject(existing, "Enable ORBIT showcase"); existing.enabled = true;
        go.SetActive(true); EditorSceneManager.MarkSceneDirty(scene);
        return existing;
    }
}
