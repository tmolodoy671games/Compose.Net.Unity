# Sharp Extensions

Kotlin-like extensions for C#.

## AutoGlobalUsingStatic

Mark a public class or struct with `[SharpExtensions.AutoGlobalUsingStatic]` to automatically
generate `global using static global::YourNamespace.YourClass;` in projects referencing
its assembly, as well as in the annotated type's own assembly.
The Sharp Extensions NuGet package includes the source generator;
consuming projects need C# 10 or newer. Referencing a DLL alone does not load analyzers:
install the NuGet package in the consuming project, or reference
`SharpExtensions.CodeGen` as an analyzer when using project references.

The generator writes `GlobalUsings.g.cs`, includes accessible nested types, and skips
open generic types and inaccessible types. The annotation is also inherited by derived types.

## Type aliases

```csharp
using SharpExtensions.Attributes;

[AutoGlobalUsingAs("Model")]
public class MyModel { }

[AutoExternalAlias("System.Text.StringBuilder", "Builder")]
public class ExternalAliases { }
```

The generator writes `global using Model = global::MyModel;` and
`global using Builder = global::System.Text.StringBuilder;` to `GlobalUsingAliases.g.cs`.
It scans the current assembly and globally referenced assemblies. External type
names may use `global::` and nested types may use dots or metadata `+` separators.
Missing, ambiguous, inaccessible and open generic types are skipped. Internal types
are included only when accessible from the consuming assembly. Conflicting aliases
are skipped; repeated aliases pointing to the same type are emitted once.
As with static imports, C# 10 or newer is required.

## IsExternalInit

The bundled generator supplies an internal `System.Runtime.CompilerServices.IsExternalInit`
in assemblies referencing SharpExtensions when no accessible definition already exists.
This enables `init` properties on older target frameworks and works with C# 9.
Existing definitions in the consuming assembly are preserved.

The generator also supplies an internal `CallerArgumentExpressionAttribute` when
no accessible definition exists. Its `ParameterName` property identifies the
argument whose source expression is captured by the C# 10+ compiler. Both
compatibility types are generated for SharpExtensions itself as well.

## Unity / NuGetForUnity

Install `T.Molodoy.SharpExtensions` using NuGetForUnity. CodeGen is bundled in the
same package under `analyzers/dotnet/roslyn4.3/cs`; no separate package is needed.
NuGetForUnity automatically assigns the `RoslynAnalyzer` label and disables runtime
loading of the generator. Included Unity importer metadata also disables runtime
loading before the package is processed. Keep NuGetForUnity's
`FixRoslynAnalyzerImportSettings` setting enabled.

Use a current NuGetForUnity version (4.3 or newer). Its Roslyn compatibility
selection enables this generator on Unity 2022.3.12f1 and newer. Generating global
static imports additionally requires the consuming assembly to compile with
C# 10 or newer; the generator does not change Unity's language settings and skips
generation on C# 9 and older.

Release package:

```powershell
dotnet pack "Sharp Extensions/Sharp Extensions.csproj" -c Release -o artifacts/nuget
```
