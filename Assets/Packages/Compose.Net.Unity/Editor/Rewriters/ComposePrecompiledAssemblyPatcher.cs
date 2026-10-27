#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Compose.Net.Patcher;
using Compose.Net.Patcher.Diagnostics;
using Mono.Cecil;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using IOPath = System.IO.Path;

namespace Compose.Net.Unity.Editor.Rewriters;

internal static class ComposePrecompiledAssemblyPatcher
{
    internal static string[] AssetPaths() => PluginImporter.GetAllImporters()
        .Where(it => !it.isNativePlugin && IsDll(it.assetPath))
        .Select(it => it.assetPath)
        .ToArray();

    internal static bool IsDll(string path) =>
        string.Equals(IOPath.GetExtension(path), ".dll", StringComparison.OrdinalIgnoreCase);

    // UPM asset paths are virtual; the DLL can physically live in Library/PackageCache.
    internal static string PhysicalPath(string assetPath)
    {
        if (assetPath.StartsWith("Packages/", StringComparison.Ordinal))
        {
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(assetPath);
            if (package != null)
                return IOPath.Combine(package.resolvedPath, assetPath.Substring(package.assetPath.Length + 1));
        }
        return IOPath.GetFullPath(assetPath);
    }

    internal static string[] SearchDirectories(IEnumerable<string> paths) => paths
        .Concat(AssetPaths().Select(PhysicalPath))
        .Concat(CompilationPipeline.GetAssemblies(AssembliesType.Editor)
            .SelectMany(it => it.allReferences.Concat(new[] { it.outputPath })))
        .Where(it => !string.IsNullOrEmpty(it))
        .Select(IOPath.GetDirectoryName)
        .Where(it => !string.IsNullOrEmpty(it))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    internal static bool Patch(string path, string[] searchDirectories)
    {
        // Inspect metadata first: unrelated managed libraries and native plug-ins are untouched.
        using (var stream = File.OpenRead(path))
        {
            try
            {
                using var assembly = AssemblyDefinition.ReadAssembly(stream, new ReaderParameters
                {
                    ReadingMode = ReadingMode.Deferred,
                    ReadSymbols = false,
                });
                if (!ComposableMethodRewriter.CanPatch(assembly))
                    return false;
            }
            catch (BadImageFormatException)
            {
                return false;
            }
        }

        var result = ComposeAssemblyPatcher.PatchFile(path, searchDirectories);
        foreach (var diagnostic in result.Diagnostics)
        {
            var message = $"Compose Unity: {path}: {diagnostic}";
            if (diagnostic.Severity == PatchSeverity.Error)
                Debug.LogError(message);
            else if (diagnostic.Severity == PatchSeverity.Warning)
                Debug.LogWarning(message);
        }
        if (result.HasErrors)
            throw new InvalidOperationException($"Compose Unity: failed to patch '{path}'. See patcher diagnostics.");
        if (result.Modified)
            Debug.Log($"Compose Unity: patched {IOPath.GetFileName(path)} ({result.PatchedMembers} members).");
        return result.Modified;
    }
}
#endif
