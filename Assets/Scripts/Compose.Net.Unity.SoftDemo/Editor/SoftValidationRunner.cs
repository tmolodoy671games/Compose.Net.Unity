#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Compose.Net.Unity.SoftDemo.Editor;

// Explicit request files only: no test or window opens on ordinary project import.
[InitializeOnLoad]
[global::Unity.Scripting.LifecycleManagement.NoAutoStaticsCleanup]
internal sealed class SoftValidationRunner : ICallbacks
{
    private static TestRunnerApi? _api;
    private static readonly string DirectoryPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "compose-soft",
        System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(Application.dataPath)));
    static SoftValidationRunner() => EditorApplication.update += Poll;
    private static void Poll()
    {
        var request = System.IO.Path.Combine(DirectoryPath, "run.request");
        if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        var command = File.ReadAllText(request).Trim(); File.Delete(request);
        try
        {
            if (command == "scene")
            {
                SoftDemoScene.InstallInSampleScene();
                File.WriteAllText(System.IO.Path.Combine(DirectoryPath, "scene.txt"), "SOFT connected and saved in Assets/Scenes/SampleScene.unity");
                return;
            }
            if (command.StartsWith("open"))
            {
                SoftDemoWindow.Open();
                var window = EditorWindow.GetWindow<SoftDemoWindow>();
                window.position = new UnityEngine.Rect(70, 70, 1240, 940); window.Focus();
                File.WriteAllText(System.IO.Path.Combine(DirectoryPath, "opened.txt"), "SOFT is open"); return;
            }
            _api = ScriptableObject.CreateInstance<TestRunnerApi>(); _api.RegisterCallbacks(new SoftValidationRunner());
            _api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode,
                assemblyNames = new[] { "Compose.Net.Unity.SoftDemo.Tests" }, testNames = command.Length == 0 ? null : new[] { command } }));
        }
        catch (Exception exception) { File.WriteAllText(System.IO.Path.Combine(DirectoryPath, "error.txt"), exception.ToString()); }
    }
    public void RunStarted(ITestAdaptor testsToRun) { }
    public void RunFinished(ITestResultAdaptor result) => TestRunnerApi.SaveResultToFile(result, System.IO.Path.Combine(DirectoryPath, "ui-results.xml"));
    public void TestStarted(ITestAdaptor test) { }
    public void TestFinished(ITestResultAdaptor result) { }
}
#endif
