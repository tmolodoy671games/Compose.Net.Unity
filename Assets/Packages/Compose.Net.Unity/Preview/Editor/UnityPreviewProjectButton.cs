using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Compose.Net.Unity.Preview;

[InitializeOnLoad]
[global::Unity.Scripting.LifecycleManagement.NoAutoStaticsCleanup]
[AiGenerated]
internal static class UnityPreviewProjectButton
{
    private static readonly Dictionary<string, IReadOnlyList<UnityPreviewEntry>> Cache = new();
    static UnityPreviewProjectButton()
    {
        EditorApplication.projectWindowItemOnGUI += DrawProjectButton;
        Editor.finishedDefaultHeaderGUI += DrawInspectorButtons;
        EditorApplication.projectChanged += Cache.Clear;
    }
    internal static IReadOnlyList<UnityPreviewEntry> Find(string path)
    {
        if (Cache.TryGetValue(path, out var entries)) return entries;
        entries = Array.Empty<UnityPreviewEntry>();
        if (path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) && File.Exists(path)
            && File.ReadAllText(path).Contains("UnityPreview"))
        {
            try { entries = UnityPreviewSource.Resolve(path, 1); }
            catch (Exception) { /* Most C# files do not declare a compiled preview. */ }
        }
        Cache[path] = entries;
        return entries;
    }
    private static GUIContent Icon(string tooltip) => new(EditorGUIUtility.IconContent("PlayButton").image, tooltip);
    private static void DrawProjectButton(string guid, UnityEngine.Rect rect)
    {
        var entries = Find(AssetDatabase.GUIDToAssetPath(guid));
        if (entries.Count == 0) return;
        var button = new UnityEngine.Rect(rect.xMax - 20, rect.y, 20, Math.Min(20, rect.height));
        if (GUI.Button(button, Icon("Open Compose.Net preview"), EditorStyles.iconButton))
            UnityPreviewWindow.Open(entries[0].Id, running: true);
    }
    private static void DrawInspectorButtons(Editor editor)
    {
        if (editor.target is not MonoScript script) return;
        var entries = Find(AssetDatabase.GetAssetPath(script));
        foreach (var entry in entries)
        {
            using var row = new EditorGUILayout.HorizontalScope();
            GUILayout.Label(string.IsNullOrWhiteSpace(entry.Settings.Name) ? entry.Method.Name : entry.Settings.Name);
            if (GUILayout.Button(Icon("Open preview"), EditorStyles.iconButton, GUILayout.Width(24), GUILayout.Height(20)))
                UnityPreviewWindow.Open(entry.Id, running: true);
        }
    }
}
