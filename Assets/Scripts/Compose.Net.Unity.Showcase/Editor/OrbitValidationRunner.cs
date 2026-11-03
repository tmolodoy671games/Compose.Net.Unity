#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Compose.Net.Unity.Showcase.Editor;

// Explicit request files only: no test or window opens on ordinary project import.
[InitializeOnLoad]
[global::Unity.Scripting.LifecycleManagement.NoAutoStaticsCleanup]
internal sealed class OrbitValidationRunner : ICallbacks
{
    private static TestRunnerApi? _api;
    private static readonly string DirectoryPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "compose-orbit");
    static OrbitValidationRunner() => EditorApplication.update += Poll;
    private static void Poll()
    {
        var request = System.IO.Path.Combine(DirectoryPath, "run.request");
        if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        var command = File.ReadAllText(request).Trim(); File.Delete(request);
        try
        {
            if (command.StartsWith("open"))
            {
                OrbitDemoWindow.Open();
                var window = EditorWindow.GetWindow<OrbitDemoWindow>();
                window.position = new UnityEngine.Rect(90, 70, 1280, 900); window.Focus();
                if (command.Contains("motion")) window.State.Go(Compose.Net.Showcase.OrbitPage.Motion);
                File.WriteAllText(System.IO.Path.Combine(DirectoryPath, "opened.txt"), "ORBIT is open"); return;
            }
            _api = ScriptableObject.CreateInstance<TestRunnerApi>(); _api.RegisterCallbacks(new OrbitValidationRunner());
            _api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode,
                assemblyNames = new[] { "Compose.Net.Unity.Showcase.Tests" }, testNames = command.Length == 0 ? null : new[] { command } }));
        }
        catch (Exception exception) { File.WriteAllText(System.IO.Path.Combine(DirectoryPath, "error.txt"), exception.ToString()); }
    }
    public void RunStarted(ITestAdaptor testsToRun) { }
    public void RunFinished(ITestResultAdaptor result) => TestRunnerApi.SaveResultToFile(result, System.IO.Path.Combine(DirectoryPath, "ui-results.xml"));
    public void TestStarted(ITestAdaptor test) { }
    public void TestFinished(ITestResultAdaptor result) { }
}
#endif
