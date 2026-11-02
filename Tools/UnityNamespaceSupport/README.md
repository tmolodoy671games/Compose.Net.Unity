# File-scoped namespaces in Unity

Unity 7.0.0a12 compiles `namespace Example;`, but its `Unity.SourceGenerators.dll`
uses `NamespaceDeclarationSyntax` when building the MonoScript type table. That
omits file-scoped namespaces, so a compiled MonoBehaviour can appear as a missing
script in a scene. `-langversion:latest` alone does not fix this.

This tool adapts the narrow source-generator patch from
[UnityRoslynUpdater](https://github.com/DaZombieKiller/UnityRoslynUpdater/blob/da374e42e7d3fea9d58f500c8d92b5f5d48b9713/sources/UnityRoslynUpdater/UpdateOperations/PatchSourceGeneratorOperation.cs).
It replaces four references in `TypeNameHelper.GetTypeInformation` with
`BaseNamespaceDeclarationSyntax`. Both namespace syntaxes then use Unity's normal
MonoScript import pipeline. It does not rewrite project source files or update the
C# compiler. The upstream MIT license is in `THIRD-PARTY-NOTICES.txt`.

## Apply

Requires a .NET SDK 8 or newer. Close Unity before applying the patch. From the
Unity project directory, use the path to the generator of the editor you use:

```sh
dotnet run --project Tools/UnityNamespaceSupport -- --check "/Applications/Unity/Hub/Editor/7000.0.0a12/Unity.app/Contents/Resources/BuildPipeline/Unity.SourceGenerators/Unity.SourceGenerators.dll"
dotnet run --project Tools/UnityNamespaceSupport -- --apply "/Applications/Unity/Hub/Editor/7000.0.0a12/Unity.app/Contents/Resources/BuildPipeline/Unity.SourceGenerators/Unity.SourceGenerators.dll"
```

`--check` returns 0 when patched, 1 when the patch is needed, and 2 when the
assembly is unsupported or cannot be read. `--apply` is idempotent and saves the
original DLL beside it as `.file-scoped-namespace.backup`, with a SHA-256 receipt.
The installation directory must be writable. The patch applies to every project
using this editor installation; rerun the check after updating or reinstalling
Unity. Unknown generator implementations are rejected without changes.

Reopen Unity and recompile the affected assemblies (reimport their `csc.rsp` files
if needed). Keep `-langversion:latest` or a supported version >= 10 in each
assembly's `csc.rsp`. Verify with the `ComponentScriptIsRecognizedByUnityImporter`
EditMode test. `AnimateContentSizeDemo` intentionally retains its file-scoped
namespace and its original script GUID.

## Restore

```sh
dotnet run --project Tools/UnityNamespaceSupport -- --restore "/path/to/Unity.SourceGenerators.dll"
```

Restore checks both hashes and refuses to overwrite a generator changed by an
editor update. `--prepare <input.dll> <output.dll>` produces a separate patched
copy without touching the installation.

## Verification

The patch was checked with Unity 7.0.0a12 on macOS: compilation, `MonoScript.GetClass`,
and forced reimport of a file-scoped MonoBehaviour. The Unity demo tests additionally
check the imported component, its UXML, and mounting/re-enabling both `UIDocument`
and `PanelRenderer` hosts.

The CLI tests use disposable copies only. Supply the **unpatched** original DLL:

```sh
dotnet build Tools/UnityNamespaceSupport
python3 Tools/UnityNamespaceSupport/test_tool.py dotnet \
  Tools/UnityNamespaceSupport/bin/Debug/net8.0/UnityNamespaceSupport.dll \
  "/path/to/Unity.SourceGenerators.dll.file-scoped-namespace.backup"
```
