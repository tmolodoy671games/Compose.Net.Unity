using System.Security.Cryptography;
using System.Text.Json;
using Mono.Cecil;

// Adapted from UnityRoslynUpdater/PatchSourceGeneratorOperation.cs (MIT).
// See THIRD-PARTY-NOTICES.txt for the original license and pinned source revision.
const string oldType = "Microsoft.CodeAnalysis.CSharp.Syntax.NamespaceDeclarationSyntax";
const string newType = "Microsoft.CodeAnalysis.CSharp.Syntax.BaseNamespaceDeclarationSyntax";
if (args.Length is < 2 or > 3 || args[0] is not ("--check" or "--prepare" or "--apply" or "--restore") ||
    (args[0] == "--prepare") != (args.Length == 3))
{
    Console.Error.WriteLine("Usage: UnityNamespaceSupport --check|--apply|--restore <Unity.SourceGenerators.dll>\n" +
                            "       UnityNamespaceSupport --prepare <Unity.SourceGenerators.dll> <output.dll>");
    return 2;
}
try
{
    var path = Path.GetFullPath(args[1]);
    if (args[0] == "--prepare" && Path.GetFullPath(args[2]) == path)
        throw new ArgumentException("--prepare requires a separate output file. Use --apply for installation.");
    var original = File.ReadAllBytes(path);
    var backupPath = path + ".file-scoped-namespace.backup";
    var receiptPath = backupPath + ".json";
    if (args[0] == "--restore")
    {
        var receipt = JsonSerializer.Deserialize<Receipt>(File.ReadAllText(receiptPath))!;
        var backup = File.ReadAllBytes(backupPath);
        if (Hash(original) != receipt.PatchedSha256 || Hash(backup) != receipt.OriginalSha256)
            throw new InvalidOperationException("The generator or backup changed since patching. Refusing to overwrite it.");
        WriteAtomic(path, backup);
        File.Delete(receiptPath);
        File.Delete(backupPath);
        Console.WriteLine("Restored the original Unity source generator.");
        return 0;
    }

    using var input = new MemoryStream(original, writable: false);
    using var assembly = AssemblyDefinition.ReadAssembly(input);
    var module = assembly.MainModule;
    var helper = module.Types.SingleOrDefault(t => t.FullName == "Unity.MonoScriptGenerator.TypeNameHelper");
    var method = helper?.Methods.SingleOrDefault(m => m.Name == "GetTypeInformation");
    if (method is not { HasBody: true })
        throw new NotSupportedException("Unrecognized Unity generator: TypeNameHelper.GetTypeInformation is missing.");
    var oldLocals = method.Body.Variables.Where(v => v.VariableType.FullName == oldType).ToArray();
    var oldOperands = method.Body.Instructions.Where(i => i.Operand is TypeReference t && t.FullName == oldType).ToArray();
    var oldCalls = method.Body.Instructions.Where(i => i.Operand is MethodReference m && m.DeclaringType.FullName == oldType).ToArray();
    var count = oldLocals.Length + oldOperands.Length + oldCalls.Length;
    if (count == 0)
    {
        if (!method.Body.Instructions.Any(i => i.Operand is TypeReference t && t.FullName == newType))
            throw new NotSupportedException("Unknown generator implementation; no changes made.");
        if (args[0] == "--prepare") WriteAtomic(Path.GetFullPath(args[2]), original);
        Console.WriteLine("File-scoped namespace support is already enabled.");
        return 0;
    }
    if (args[0] == "--check")
    {
        Console.WriteLine($"File-scoped namespace support is missing ({count} references to update).");
        return 1;
    }
    // Fail closed when a future Unity version changes this method's contract.
    if (oldCalls.Any(i => i.Operand is not MethodReference { Name: "get_Name", Parameters.Count: 0 }))
        throw new NotSupportedException("Unexpected namespace member in Unity generator; no changes made.");
    var roslyn = module.AssemblyReferences.Single(a => a.Name == "Microsoft.CodeAnalysis.CSharp");
    var replacement = new TypeReference("Microsoft.CodeAnalysis.CSharp.Syntax", "BaseNamespaceDeclarationSyntax", module, roslyn);
    foreach (var local in oldLocals) local.VariableType = replacement;
    foreach (var instruction in oldOperands) instruction.Operand = replacement;
    foreach (var instruction in oldCalls)
    {
        var old = (MethodReference)instruction.Operand;
        instruction.Operand = new MethodReference(old.Name, old.ReturnType, replacement)
        {
            HasThis = old.HasThis, ExplicitThis = old.ExplicitThis, CallingConvention = old.CallingConvention
        };
    }
    using var output = new MemoryStream();
    assembly.Write(output);
    var patched = output.ToArray();
    if (args[0] == "--prepare")
    {
        var outputPath = Path.GetFullPath(args[2]);
        if (outputPath == path) throw new ArgumentException("--prepare requires a separate output file. Use --apply for installation.");
        WriteAtomic(outputPath, patched);
    }
    else
    {
        if (File.Exists(backupPath) || File.Exists(receiptPath))
            throw new IOException("A backup already exists. Inspect or restore it before patching this generator.");
        File.WriteAllBytes(backupPath, original);
        File.WriteAllText(receiptPath, JsonSerializer.Serialize(new Receipt(Hash(original), Hash(patched)), new JsonSerializerOptions { WriteIndented = true }));
        WriteAtomic(path, patched);
    }
    Console.WriteLine($"Updated {count} namespace references in Unity's MonoScript generator.");
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception.Message);
    return 2;
}

static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
static void WriteAtomic(string path, byte[] bytes)
{
    var temporary = path + ".namespace-support-" + Guid.NewGuid().ToString("N");
    try { File.WriteAllBytes(temporary, bytes); File.Move(temporary, path, overwrite: true); }
    finally { if (File.Exists(temporary)) File.Delete(temporary); }
}
internal sealed record Receipt(string OriginalSha256, string PatchedSha256);
