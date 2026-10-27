#if UNITY_EDITOR
using System;
using System.IO;
using Mono.Cecil;
using UnityEditor;
using UnityEngine;

namespace Compose.Net.Unity.Editor.Utils;

internal static class AssemblyDefinitionFactory
{
    public static AssemblyDefinition? Create(FileInfo file)
    {
        var resolver = new DefaultAssemblyResolver();

        AddAssemblySearchDirectories(resolver, file);
        try
        {
            return AssemblyDefinition.ReadAssembly(file.FullName);
        }
        catch (AssemblyResolutionException)
        {
            return null;
        }
        catch (BadImageFormatException)
        {
            return null;
        }
    }

    private static void AddAssemblySearchDirectories(
        DefaultAssemblyResolver resolver,
        FileInfo assembly
    )
    {
        // Directory containing the assembly itself.
        resolver.AddSearchDirectory(assembly.Directory!.FullName);

        // Unity managed assemblies.
        resolver.AddSearchDirectory(
            System.IO.Path.Combine(
                EditorApplication.applicationContentsPath,
                "Managed"
            )
        );

        // Unity modules.
        resolver.AddSearchDirectory(
            System.IO.Path.Combine(
                EditorApplication.applicationContentsPath,
                "Managed",
                "UnityEngine"
            )
        );

        // Packages / project assemblies which may be next to the target assembly.
        resolver.AddSearchDirectory(
            System.IO.Path.GetDirectoryName(typeof(EditorApplication).Assembly.GetLoadedAssemblyPath())!);
    }
}
#endif