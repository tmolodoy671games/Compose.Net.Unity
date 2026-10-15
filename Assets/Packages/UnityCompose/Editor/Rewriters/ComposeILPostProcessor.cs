#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Compose.Net.Patcher;
using Compose.Net.Patcher.Diagnostics;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Packages.UnityCompose.Editor.Extensions;
using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.CompilationPipeline.Common.ILPostProcessing;
using UnityEditor;

namespace Packages.UnityCompose.Editor.Rewriters;

[InitializeOnLoad]
internal class ComposeILPostProcessor : ILPostProcessor
{
    // true: в консоль пишется, что сборка была пропущена и почему
    private const bool Verbose = true;

    public override ILPostProcessor GetInstance() => this;

    // Только дешёвая проверка ссылок и никаких исключений:
    // раньше любая ошибка разбора сборки молча превращалась в "не обрабатывать".
    public override bool WillProcess(ICompiledAssembly compiledAssembly) =>
        ReferencesCompose(compiledAssembly);

    public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
    {
        try
        {
            using var assembly = compiledAssembly.ToAssemblyDefinition(readSymbols: true);

            if (!ComposableMethodRewriter.CanPatch(assembly))
            {
                return Unchanged(
                    compiledAssembly,
                    Verbose
                        ? Warning($"skipped {compiledAssembly.Name}: no [RecompiledComposableCode] methods found")
                        : null);
            }

            var messages = ComposableMethodRewriter.Patch(assembly);

            using var peStream = new MemoryStream();
            using var pdbStream = new MemoryStream();
            assembly.Write(peStream, new WriterParameters
            {
                WriteSymbols = true,
                SymbolStream = pdbStream,
                SymbolWriterProvider = new PortablePdbWriterProvider(),
            });

            return new ILPostProcessResult(
                new InMemoryAssembly(peStream.ToArray(), pdbStream.ToArray()),
                messages.Diagnostics
                    .Select(it => new DiagnosticMessage
                    {
                        DiagnosticType = it.Severity switch
                        {
                            PatchSeverity.Warning => DiagnosticType.Warning,
                            PatchSeverity.Error => DiagnosticType.Error,
                            _ => DiagnosticType.Warning
                        },
                        MessageData = it.Message,
                        Line = it.Line,
                        Column = it.Column,
                        File = it.File ?? ""
                    })
                    .ToList()
            );
        }
        catch (Exception e)
        {
            // Теперь ошибка видна в консоли Unity, а не глотается
            return Unchanged(
                compiledAssembly,
                new DiagnosticMessage
                {
                    DiagnosticType = DiagnosticType.Error,
                    MessageData = $"Compose ILPP failed on {compiledAssembly.Name}: {e}",
                });
        }
    }

    private static ILPostProcessResult Unchanged(ICompiledAssembly compiledAssembly, DiagnosticMessage message)
    {
        var messages = new List<DiagnosticMessage>();
        if (message != null)
            messages.Add(message);
        return new ILPostProcessResult(compiledAssembly.InMemoryAssembly, messages);
    }

    private static DiagnosticMessage Warning(string text) => new()
    {
        DiagnosticType = DiagnosticType.Warning,
        MessageData = "Compose ILPP: " + text,
    };

    private static bool ReferencesCompose(ICompiledAssembly compiledAssembly)
    {
        foreach (var reference in compiledAssembly.References)
            if (reference.IndexOf("Compose.Net", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        return compiledAssembly.Name.IndexOf("Compose.Net", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
#endif