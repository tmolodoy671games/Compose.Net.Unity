using Compose.Net;
using StableCollections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Runtime.Adapters.Transitions;

internal static class SizeTransitionUtils
{
    private const string NaturalSizeKey = "Compose.SizeTransition.NaturalSize";
    private sealed class NaturalSize
    {
        internal StyleLength? MinWidth, MinHeight;
    }

    private static void PreserveNaturalSize(VisualElement element, Size full, bool horizontal, bool vertical)
    {
        var data = element.UserData();
        if (data.GetOrNull(NaturalSizeKey) is not NaturalSize saved)
            data[NaturalSizeKey] = saved = new NaturalSize();
        // Flex may shrink auto-sized content to the animated clip, feeding zero back into
        // the next measurement. Keep its natural size while the parent clips it.
        if (horizontal)
        {
            saved.MinWidth ??= Box.AuthoredBound(element, Box.BoundProperty.MinWidth);
            Box.SetAuthoredBound(element, Box.BoundProperty.MinWidth, full.Width);
        }
        if (vertical)
        {
            saved.MinHeight ??= Box.AuthoredBound(element, Box.BoundProperty.MinHeight);
            Box.SetAuthoredBound(element, Box.BoundProperty.MinHeight, full.Height);
        }
    }

    public static void Apply(VisualElement element, Size full, Size other, float progress,
        bool entering, Alignment alignment, bool clip, bool horizontal, bool vertical)
    {
        var parent = element.parent;
        if (parent == null || float.IsNaN(full.Width) || float.IsNaN(full.Height)) return;
        var t = entering ? progress : 1f - progress;
        var width = Mathf.Max(0, Mathf.LerpUnclamped(other.Width, full.Width, t));
        var height = Mathf.Max(0, Mathf.LerpUnclamped(other.Height, full.Height, t));
        element.style.flexShrink = 0;
        if (t < 1) PreserveNaturalSize(element, full, horizontal, vertical);
        if (horizontal)
        {
            Box.SetAnimatedConstraints(parent, Box.AnimatedConstraints.MaxWidth, true);
            parent.style.maxWidth = width;
            element.style.left = (width - full.Width) * HorizontalFraction(alignment);
        }
        if (vertical)
        {
            Box.SetAnimatedConstraints(parent, Box.AnimatedConstraints.MaxHeight, true);
            parent.style.maxHeight = height;
            element.style.top = (height - full.Height) * VerticalFraction(alignment);
        }
        if (clip) parent.Clip().Increment();
    }

    public static void Revert(VisualElement element, bool clip, bool horizontal, bool vertical)
    {
        var parent = element.parent;
        var data = element.UserData();
        if (data.GetOrNull(NaturalSizeKey) is NaturalSize saved)
        {
            if (horizontal && saved.MinWidth is { } width)
            { Box.SetAuthoredBound(element, Box.BoundProperty.MinWidth, width); saved.MinWidth = null; }
            if (vertical && saved.MinHeight is { } height)
            { Box.SetAuthoredBound(element, Box.BoundProperty.MinHeight, height); saved.MinHeight = null; }
            if (saved.MinWidth == null && saved.MinHeight == null) data.Remove(NaturalSizeKey);
        }
        element.style.flexShrink = StyleKeyword.Null;
        if (horizontal)
        {
            if (parent != null)
            {
                parent.style.maxWidth = StyleKeyword.Null;
                Box.SetAnimatedConstraints(parent, Box.AnimatedConstraints.MaxWidth, false);
            }
            element.style.left = StyleKeyword.Null;
        }
        if (vertical)
        {
            if (parent != null)
            {
                parent.style.maxHeight = StyleKeyword.Null;
                Box.SetAnimatedConstraints(parent, Box.AnimatedConstraints.MaxHeight, false);
            }
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
