#if UNITY_EDITOR
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;
using SharpExtensions;
using global::Unity.CompilationPipeline.Common.ILPostProcessing;

namespace Compose.Net.Unity.Editor.Extensions;

internal static class CompiledAssemblyExtensions
{
    public static AssemblyDefinition ToAssemblyDefinition(
        this ICompiledAssembly compiledAssembly,
        bool readSymbols = false
    )
    {
        var resolver = new DefaultAssemblyResolver();
        foreach (var reference in compiledAssembly.References)
        {
            var directory = System.IO.Path.GetDirectoryName(reference);
            if (!string.IsNullOrEmpty(directory))
                resolver.AddSearchDirectory(directory);
        }

        var parameters = new ReaderParameters
        {
            AssemblyResolver = resolver,
            ReadingMode = ReadingMode.Immediate,
            InMemory = true,
        };

        var pdb = compiledAssembly.InMemoryAssembly.PdbData;
        if (readSymbols && pdb != null && pdb.Length > 0)
        {
            parameters.ReadSymbols = true;
            parameters.SymbolReaderProvider = new PortablePdbReaderProvider();
            parameters.SymbolStream = new MemoryStream(pdb);
            parameters.ThrowIfSymbolsAreNotMatching = false;
        }

        return AssemblyDefinition.ReadAssembly(
            new MemoryStream(compiledAssembly.InMemoryAssembly.PeData),
            parameters);
    }
}
#endif