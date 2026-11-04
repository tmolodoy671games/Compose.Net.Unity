using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Compose.Net.Unity.MaterialDemo.Editor;

// Only an explicit, project-scoped request saves a scene. Ordinary imports do nothing.
[InitializeOnLoad]
[global::Unity.Scripting.LifecycleManagement.NoAutoStaticsCleanup]
internal static class MaterialSceneRequest
{
    private static readonly string Folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "compose-materials",
        System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(Application.dataPath)));
    static MaterialSceneRequest() => EditorApplication.update += Poll;
    private static void Poll()
    {
        var request = System.IO.Path.Combine(Folder, "scene.request");
        if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(request);
        try
        {
            MaterialDemoScene.InstallInSampleScene();
            File.WriteAllText(System.IO.Path.Combine(Folder, "scene.result"), "Saved Assets/Scenes/SampleScene.unity / Demos / Material Lab");
        }
        catch (Exception exception) { File.WriteAllText(System.IO.Path.Combine(Folder, "scene.result"), exception.ToString()); }
    }
}
