using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.MaterialDemo.Editor;

public static class TabsDemoScene
{
    public static void InstallInSampleScene()
    {
        const string path = "Assets/Scenes/SampleScene.unity";
        var scene = SceneManager.GetSceneByPath(path);
        if (!scene.IsValid() || !scene.isLoaded)
            scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        var previous = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(scene);
        try
        {
            AddToScene();
            if (!EditorSceneManager.SaveScene(scene))
                throw new System.InvalidOperationException("Could not save " + path);
        }
        finally { if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous); }
    }

    public static void AddToScene()
    {
        var scene = SceneManager.GetActiveScene();
        var existing = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<TabsDemo>(true)).FirstOrDefault();
        if (existing) { Selection.activeGameObject = existing.gameObject; return; }
        var demos = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "Demos");
        if (!demos)
        {
            demos = new GameObject("Demos");
            Undo.RegisterCreatedObjectUndo(demos, "Add demo group");
        }
        var go = new GameObject("Tabs Lab");
        Undo.RegisterCreatedObjectUndo(go, "Add Tabs Lab");
        Undo.SetTransformParent(go.transform, demos.transform, "Group Tabs Lab");
        var renderer = Undo.AddComponent<PanelRenderer>(go);
        renderer.sortingOrder = 200;
        renderer.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/Settings/Panel Settings.asset");
        renderer.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Packages/Compose.Net.Unity/Runtime/Layout/ComposeView Root.uxml");
        Undo.AddComponent<TabsDemo>(go);
        Selection.activeGameObject = go;
        EditorSceneManager.MarkSceneDirty(scene);
    }
}
