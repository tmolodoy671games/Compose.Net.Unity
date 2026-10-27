using Compose.Net;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Runtime.Adapters.Transitions;

internal static class SizeTransitionUtils
{
    public static void Apply(VisualElement element, Size full, Size other, float progress,
        bool entering, Alignment alignment, bool clip, bool horizontal, bool vertical)
    {
        var parent = element.parent;
        if (parent == null || float.IsNaN(full.Width) || float.IsNaN(full.Height)) return;
        var t = entering ? progress : 1f - progress;
        var width = Mathf.Max(0, Mathf.LerpUnclamped(other.Width, full.Width, t));
        var height = Mathf.Max(0, Mathf.LerpUnclamped(other.Height, full.Height, t));
        element.style.flexShrink = 0;
        if (horizontal)
        {
            parent.style.maxWidth = width;
            element.style.left = (width - full.Width) * HorizontalFraction(alignment);
        }
        if (vertical)
        {
            parent.style.maxHeight = height;
            element.style.top = (height - full.Height) * VerticalFraction(alignment);
        }
        if (clip) parent.Clip().Increment();
    }

    public static void Revert(VisualElement element, bool clip, bool horizontal, bool vertical)
    {
        var parent = element.parent;
        element.style.flexShrink = StyleKeyword.Null;
        if (horizontal)
        {
            if (parent != null) parent.style.maxWidth = StyleKeyword.Null;
            element.style.left = StyleKeyword.Null;
        }
        if (vertical)
        {
            if (parent != null) parent.style.maxHeight = StyleKeyword.Null;
            element.style.top = StyleKeyword.Null;
        }
        if (clip && parent != null) parent.Clip().Decrement();
    }

    private static float HorizontalFraction(Alignment alignment)
        => alignment == Alignment.Left || alignment == Alignment.TopLeft || alignment == Alignment.CenterLeft || alignment == Alignment.BottomLeft ? 0f
            : alignment == Alignment.CenterHorizontally || alignment == Alignment.TopCenter || alignment == Alignment.Center || alignment == Alignment.BottomCenter ? 0.5f : 1f;

    private static float VerticalFraction(Alignment alignment)
        => alignment == Alignment.Top || alignment == Alignment.TopLeft || alignment == Alignment.TopCenter || alignment == Alignment.TopRight ? 0f
            : alignment == Alignment.CenterVertically || alignment == Alignment.CenterLeft || alignment == Alignment.Center || alignment == Alignment.CenterRight ? 0.5f : 1f;
}
