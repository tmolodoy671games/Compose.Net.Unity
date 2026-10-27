# Compose.Net

.Net implementation of Compose.
The NuGet package includes `Compose.Net.Patcher.dll` and `Mono.Cecil.dll` as
Editor-only content for NuGetForUnity. Their `.meta` files enable only the Editor
platform, so they are excluded from Unity Player builds. They are not runtime
references of `Compose.Net` or ordinary .NET package consumers.

[Unity plug-in platform settings](https://docs.unity3d.com/Manual/PluginInspector.html)
