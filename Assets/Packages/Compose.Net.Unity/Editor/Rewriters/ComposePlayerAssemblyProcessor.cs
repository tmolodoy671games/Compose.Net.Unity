#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Compose.Net.Unity.Editor.Rewriters;

// Patch Unity's staged copies before stripping and IL2CPP, including imported DLLs
// which are not passed through ILPostProcessor.
internal sealed class ComposePlayerAssemblyProcessor : IPostBuildPlayerScriptDLLs
{
    public int callbackOrder => 0;

    public void OnPostBuildPlayerScriptDLLs(BuildReport report)
    {
        var assemblies = report.GetFiles()
            .Select(it => it.path)
            .Where(ComposePrecompiledAssemblyPatcher.IsDll)
            .Where(File.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var directories = ComposePrecompiledAssemblyPatcher.SearchDirectories(assemblies);
        foreach (var path in assemblies)
        {
            try
            {
                ComposePrecompiledAssemblyPatcher.Patch(path, directories);
            }
            catch (Exception exception)
            {
                throw new BuildFailedException($"Compose Unity: {path}: {exception.Message}");
            }
        }
    }
}
#endif
