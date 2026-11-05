using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace Compose.Net.Unity.Preview;

public sealed class UnityPreviewEntry
{
    public MethodInfo Method { get; }
    public UnityPreviewAttribute Settings { get; }
    public string Id { get; }
    public string DisplayName => $"{Method.DeclaringType!.FullName}.{Method.Name} — {Settings.Name ?? "Preview"} ({Settings.Width}×{Settings.Height})";
    public string? Error { get; }

    internal UnityPreviewEntry(MethodInfo method, UnityPreviewAttribute settings, int variant)
    {
        Method = method;
        Settings = settings;
        Id = $"{method.DeclaringType!.AssemblyQualifiedName}:{method}:{variant}";
        Error = !method.IsStatic || method.GetParameters().Length != 0 || method.ReturnType != typeof(void)
            || method.ContainsGenericParameters || method.DeclaringType.ContainsGenericParameters
            ? "Preview must be a static, parameterless void method in a non-generic type."
            : !method.IsDefined(typeof(Composable), false)
                ? "Preview must also have [Composable]."
                : settings.Width <= 0 || settings.Height <= 0
                    ? "Preview width and height must be positive."
                    : null;
    }

    public Action CreateContent()
    {
        if (Error != null) throw new InvalidOperationException(Error);
        return (Action)Delegate.CreateDelegate(typeof(Action), Method);
    }
}

public static class UnityPreviewCatalog
{
    public static IReadOnlyList<UnityPreviewEntry> Discover() => TypeCache
        .GetMethodsWithAttribute<UnityPreviewAttribute>()
        .SelectMany(method => method.GetCustomAttributes<UnityPreviewAttribute>(false)
            .Select((settings, variant) => new UnityPreviewEntry(method, settings, variant)))
        .OrderBy(entry => entry.DisplayName, StringComparer.Ordinal)
        .ToArray();
}
