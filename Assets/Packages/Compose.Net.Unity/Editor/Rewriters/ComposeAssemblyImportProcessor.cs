#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Compose.Net.Unity.Editor.Rewriters;

// Unity 6 does not invoke OnPreprocessAsset for DLLs. Patch after import, then
// reimport changed images so the next domain uses the patched assemblies.
[InitializeOnLoad]
#if UNITY_6000_7_OR_NEWER
[global::Unity.Scripting.LifecycleManagement.NoAutoStaticsCleanup]
#endif
internal sealed class ComposeAssemblyImportProcessor : AssetPostprocessor
{
    private static bool processing;

    static ComposeAssemblyImportProcessor()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPostprocessAllAssets(
        string[] importedAssets, string[] deletedAssets,
        string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
    {
        if (processing || EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        var paths = didDomainReload
            ? ComposePrecompiledAssemblyPatcher.AssetPaths()
            : importedAssets.Concat(movedAssets)
                .Where(ComposePrecompiledAssemblyPatcher.IsDll)
                .Where(it => AssetImporter.GetAtPath(it) is PluginImporter { isNativePlugin: false })
                .ToArray();
        PatchAndReimport(paths, out _);
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode)
            return;
        var success = PatchAndReimport(ComposePrecompiledAssemblyPatcher.AssetPaths(), out var changed);
        if (success && !changed)
            return;
        EditorApplication.isPlaying = false;
        if (changed)
            Debug.Log("Compose Unity: libraries patched. Enter Play Mode again after the assembly reload finishes.");
    }

    private static bool PatchAndReimport(string[] assetPaths, out bool changed)
    {
        changed = false;
        if (assetPaths.Length == 0 || processing)
            return true;
        processing = true;
        var modified = new List<string>();
        var success = true;
        try
        {
            var directories = ComposePrecompiledAssemblyPatcher.SearchDirectories(
                assetPaths.Select(ComposePrecompiledAssemblyPatcher.PhysicalPath));
            foreach (var path in assetPaths.Distinct())
            {
                try
                {
                    if (ComposePrecompiledAssemblyPatcher.Patch(
                            ComposePrecompiledAssemblyPatcher.PhysicalPath(path), directories))
                        modified.Add(path);
                }
                catch (Exception exception)
                {
                    success = false;
                    Debug.LogError($"Compose Unity: {path}: {exception.Message}");
                }
            }
            changed = modified.Count > 0;
            foreach (var path in modified)
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return success;
        }
        catch (Exception exception)
        {
            Debug.LogError($"Compose Unity: could not prepare imported assemblies: {exception.Message}");
            return false;
        }
        finally
        {
            processing = false;
        }
    }
}
#endif
