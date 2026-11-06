using UnityEditor;
using UnityEngine;
using System;
using System.Runtime.InteropServices;

namespace Compose.Net.Unity.Preview;

[AiGenerated]
internal static class UnityPreviewSizing
{
    [AiGenerated]
    internal readonly struct Size
    {
        public readonly Vector2 Logical;
        public readonly float Scale;
        public Vector2 Display => Logical * Scale;
        public Size(Vector2 logical, float scale) { Logical = logical; Scale = scale; }
    }
    internal static Size Resolve(UnityPreview settings, Vector2 viewport, Vector2 screen)
    {
        var scale = 1f;
        if (settings.Width > 0) scale = Mathf.Min(scale, Mathf.Max(1, screen.x) / settings.Width);
        if (settings.Height > 0) scale = Mathf.Min(scale, Mathf.Max(1, screen.y) / settings.Height);
        var logical = new Vector2(settings.Width > 0 ? settings.Width : Mathf.Max(1, viewport.x) / scale,
            settings.Height > 0 ? settings.Height : Mathf.Max(1, viewport.y) / scale);
        return new Size(logical, scale);
    }
    internal static UnityEngine.Rect ScreenAt(Vector2 point)
    {
#if UNITY_EDITOR_OSX
        var screens = Send(ObjectiveClass("NSScreen"), Selector("screens"));
        var count = SendCount(screens, Selector("count"));
        if (count > 0)
        {
            var primary = SendIndex(screens, Selector("objectAtIndex:"), 0);
            var primaryFrame = ReadRect(primary, "frame");
            var top = primaryFrame.y + primaryFrame.height;
            var closest = new UnityEngine.Rect();
            var distance = float.PositiveInfinity;
            for (ulong index = 0; index < count; index++)
            {
                var screen = SendIndex(screens, Selector("objectAtIndex:"), index);
                var frame = ReadRect(screen, "frame").TopLeft(top);
                var visible = ReadRect(screen, "visibleFrame").TopLeft(top);
                if (frame.Contains(point)) return visible;
                var next = (frame.center - point).sqrMagnitude;
                if (next < distance) { closest = visible; distance = next; }
            }
            return closest;
        }
#else
        try { return UnityEditorInternal.InternalEditorUtility.GetBoundsOfDesktopAtPoint(point); }
        catch (ArgumentException) { }
#endif
        var pixels = Mathf.Max(1, EditorGUIUtility.pixelsPerPoint);
        return new UnityEngine.Rect(0, 0, UnityEngine.Screen.currentResolution.width / pixels, UnityEngine.Screen.currentResolution.height / pixels);
    }
#if UNITY_EDITOR_OSX
    [AiGenerated]
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public double x, y, width, height;
        public UnityEngine.Rect TopLeft(double primaryTop) => new((float)x, (float)(primaryTop - y - height), (float)width, (float)height);
    }
    private static NativeRect ReadRect(IntPtr instance, string name)
    {
        if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
        {
            SendRectIntel(out var result, instance, Selector(name)); return result;
        }
        return SendRect(instance, Selector(name));
    }
    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_getClass")]
    private static extern IntPtr ObjectiveClass(string name);
    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "sel_registerName")]
    private static extern IntPtr Selector(string name);
    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr Send(IntPtr instance, IntPtr selector);
    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern ulong SendCount(IntPtr instance, IntPtr selector);
    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr SendIndex(IntPtr instance, IntPtr selector, ulong index);
    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern NativeRect SendRect(IntPtr instance, IntPtr selector);
    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend_stret")]
    private static extern void SendRectIntel(out NativeRect result, IntPtr instance, IntPtr selector);
#endif
    internal static Vector2 ContentLimit(UnityEngine.Rect screen) => new(Mathf.Max(1, screen.width - 16), Mathf.Max(1, screen.height - 64));
    internal static Vector2 CurrentContentLimit => ContentLimit(ScreenAt(EditorGUIUtility.GetMainWindowPosition().center));
    internal static UnityEngine.Rect Constrain(UnityEngine.Rect window, UnityEngine.Rect screen)
    {
        window.width = Mathf.Min(window.width, screen.width);
        window.height = Mathf.Min(window.height, Mathf.Max(1, screen.height - 32));
        window.x = Mathf.Clamp(window.x, screen.xMin, screen.xMax - window.width);
        window.y = Mathf.Clamp(window.y, screen.yMin + 32, screen.yMax - window.height);
        return window;
    }
}
