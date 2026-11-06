using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
[InitializeOnLoad]
[Unity.Scripting.LifecycleManagement.NoAutoStaticsCleanup]
internal sealed class ComposePreviewValidationRunner : ICallbacks
{
    private static TestRunnerApi api;
    static ComposePreviewValidationRunner() => EditorApplication.update += Poll;
    private static void Poll()
    {
        const string request = "/private/tmp/compose-preview-live.request";
        if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        File.Delete(request);
        api = ScriptableObject.CreateInstance<TestRunnerApi>();
        api.RegisterCallbacks(new ComposePreviewValidationRunner());
        api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode,
            assemblyNames = new[] { "Compose.Net.Unity.Preview.Tests" } }));
    }
    public void RunStarted(ITestAdaptor testsToRun) { }
    public void RunFinished(ITestResultAdaptor result) => TestRunnerApi.SaveResultToFile(result, "/private/tmp/compose-preview-performance-results.xml");
    public void TestStarted(ITestAdaptor test) { }
    public void TestFinished(ITestResultAdaptor result) { }
}
