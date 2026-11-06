using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEditor;
using UnityEditor.Compilation;
using IOPath = System.IO.Path;

namespace Compose.Net.Unity.Preview;

[AiGenerated]
internal static class UnityPreviewSource
{
    // Use the compiled source map rather than guessing C# method/type names from text.
    internal static IReadOnlyList<UnityPreviewEntry> Resolve(string sourceFile, int line)
    {
        var source = IOPath.GetFullPath(sourceFile);
        var matches = new List<(UnityPreviewEntry entry, int start, int end)>();
        foreach (var group in UnityPreviewCatalog.Discover().GroupBy(entry => entry.Method.DeclaringType!.Assembly.GetName().Name))
        {
            try
            {
                var location = CompilationPipeline.GetAssemblies().FirstOrDefault(assembly => assembly.name == group.Key)?.outputPath;
                if (string.IsNullOrEmpty(location) || !System.IO.File.Exists(location))
                    location = AssetDatabase.FindAssets(group.Key).Select(AssetDatabase.GUIDToAssetPath)
                        .FirstOrDefault(path => IOPath.GetFileName(path) == group.Key + ".dll");
                if (string.IsNullOrEmpty(location) || !System.IO.File.Exists(location)) continue;
                using var assembly = AssemblyDefinition.ReadAssembly(location,
                    new ReaderParameters { ReadSymbols = true, InMemory = true });
                foreach (var entry in group)
                {
                    if (assembly.MainModule.LookupToken(entry.Method.MetadataToken) is not MethodDefinition method) continue;
                    var points = method.DebugInformation.SequencePoints
                        .Where(point => !point.IsHidden && SamePath(point.Document.Url, source)).ToArray();
                    if (points.Length != 0)
                        matches.Add((entry, points.Min(point => point.StartLine), points.Max(point => point.EndLine)));
                }
            }
            catch (SymbolsNotFoundException) { }
        }
        if (matches.Count == 0)
            throw new InvalidOperationException("No compiled [UnityPreview] from this file was found. Save and compile the file; debug symbols must be available.");
        var selected = matches.OrderBy(match => line >= match.start && line <= match.end ? 0
            : match.start >= line ? match.start - line : 1_000_000 + line - match.end).First();
        return matches.Where(match => match.entry.Method == selected.entry.Method).Select(match => match.entry).ToArray();
    }

    private static bool SamePath(string left, string right) => string.Equals(IOPath.GetFullPath(left), right,
        System.IO.Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
}
