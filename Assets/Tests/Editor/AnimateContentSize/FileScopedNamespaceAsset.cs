using UnityEngine;

namespace Compose.Net.Unity.Samples.Tests.FileScoped;

// This file deliberately uses a file-scoped namespace to exercise Unity's importer.
public sealed class FileScopedNamespaceAsset : ScriptableObject
{
    public int Value;
}
