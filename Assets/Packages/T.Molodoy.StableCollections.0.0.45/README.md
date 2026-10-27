# Stable Collections

Java-like collections for C#: mutable and persistent lists, sets, maps and two-dimensional arrays.

## Installation

```powershell
dotnet add package T.Molodoy.StableCollections --version 0.0.45
```

Targets .NET Standard 2.1. C# 14 is required to build the source project.

## Usage

```csharp
using static StableCollections.StableCollectionsFactory;

var numbers = MutableStableListOf(1, 2, 3);
var snapshot = PersistentStableListOf(1, 2, 3);
```

## Build package

```powershell
dotnet pack StableCollections/StableCollections.csproj -c Release -o artifacts
```

Licensed under the MIT license.
