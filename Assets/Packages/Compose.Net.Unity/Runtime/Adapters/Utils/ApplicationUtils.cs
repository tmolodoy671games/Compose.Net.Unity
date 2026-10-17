// ReSharper disable RedundantUsingDirective

using UnityEditor;
using UnityEngine;

namespace Compose.Net.Unity.Runtime.Adapters.Utils;

internal static class ApplicationUtils
{
    public static bool IsPlaying
    {
        get
        {
#if UNITY_EDITOR
            return EditorApplication.isPlaying && !BuildPipeline.isBuildingPlayer;
#else
            return Application.isPlaying;
#endif
        }
    }
    
#if UNITY_EDITOR
    public static bool IsQuitting =>
        EditorApplication.isPlayingOrWillChangePlaymode &&
        !EditorApplication.isPlaying;
#else
    public static bool IsQuitting =>
        Application.isPlaying && !Application.isPlaying;
#endif
}