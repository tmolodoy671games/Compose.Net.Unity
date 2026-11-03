using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

[InitializeOnLoad]
[Unity.Scripting.LifecycleManagement.NoAutoStaticsCleanup]
internal sealed class PagerValidationRunner : ICallbacks
{
    private static TestRunnerApi api;
    static PagerValidationRunner() => EditorApplication.update += Poll;
    private static void Poll()
    {
        const string request = "/private/tmp/compose-pager/run.request";
        if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (!SessionState.GetBool("PagerTestsRefreshed", false))
        {
            SessionState.SetBool("PagerTestsRefreshed", true);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            return;
        }
        SessionState.SetBool("PagerTestsRefreshed", false);
        var filter = File.ReadAllText(request).Trim(); File.Delete(request);
        if (filter == "scene")
        {
            EditorApplication.ExecuteMenuItem("Tools/Compose.Net/Add Pager Demo to Scene");
            File.WriteAllText("/private/tmp/compose-pager/scene.done", "ok");
            return;
        }
        api = ScriptableObject.CreateInstance<TestRunnerApi>();
        api.RegisterCallbacks(new PagerValidationRunner());
        api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode,
            assemblyNames = new[] { "Compose.Net.Unity.Pager.Tests" },
            testNames = filter.Length == 0 ? null : new[] { filter } }));
    }
    public void RunStarted(ITestAdaptor testsToRun) { }
    public void RunFinished(ITestResultAdaptor result) => TestRunnerApi.SaveResultToFile(result, "/private/tmp/compose-pager/ui-results.xml");
    public void TestStarted(ITestAdaptor test) { }
    public void TestFinished(ITestResultAdaptor result) { }
}
