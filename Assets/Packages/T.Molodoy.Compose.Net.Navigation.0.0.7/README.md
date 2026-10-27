# Compose.Net

.Net implementation of Compose.

Library builds leave the generated composable implementations in the DLL without
patching it. The Unity integration applies `Compose.Net.Patcher` inside the Editor:

- Imported managed plug-ins are checked after import and on domain reload. DLLs
  with generated composable members are patched in place, together with their PDBs,
  and reimported before Play Mode. Unrelated and already patched DLLs are unchanged.
- Player builds patch the staged managed assemblies in
  `IPostBuildPlayerScriptDLLs`, before managed stripping and IL2CPP conversion.
- Scripts compiled by Unity continue to use `ComposeILPostProcessor`.

Install the patcher and Mono.Cecil as Editor-only plug-ins. These are build tools,
not runtime references of `Compose.Net`. Imported DLLs must be writable; replacing
or restoring a package causes Unity to apply the patch again. Source generation
still runs when the original library is compiled.

For a non-Unity consumer that needs a patched library directly, opt in with
`dotnet build -p:ComposePatchAfterBuild=true`, or run
`dotnet run --project Compose.Net.Patch -- path/to/library.dll`.
The .NET regression executable patches only its own output copies.

[Unity plug-in platform settings](https://docs.unity3d.com/Manual/PluginInspector.html)
