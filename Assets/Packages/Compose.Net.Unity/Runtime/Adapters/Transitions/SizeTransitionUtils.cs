using Compose.Net;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Runtime.Adapters.Transitions;

internal static class SizeTransitionUtils
{
    internal static void UpdateStyle(UnityStyleModifierNode node, Size full, Size other, float progress,
        bool entering, Alignment alignment, bool clip, bool horizontal, bool vertical)
    {
        var parent = node.ParentStyles;
        if (parent == null || float.IsNaN(full.Width) || float.IsNaN(full.Height)) return;
        var t = entering ? progress : 1f - progress;
        var width = Mathf.Max(0, Mathf.LerpUnclamped(other.Width, full.Width, t));
        var height = Mathf.Max(0, Mathf.LerpUnclamped(other.Height, full.Height, t));
        node.Set<StyleFloat>(node.Styles.FlexShrink, 0);
        if (horizontal)
        {
            node.Set<StyleLength>(parent.MaxWidth, width);
            node.Set<StyleLength>(node.Styles.Left, (width - full.Width) * HorizontalFraction(alignment));
        }
        if (vertical)
        {
            node.Set<StyleLength>(parent.MaxHeight, height);
            node.Set<StyleLength>(node.Styles.Top, (height - full.Height) * VerticalFraction(alignment));
        }
        if (clip) node.Set<StyleEnum<Overflow>>(parent.Overflow, Overflow.Hidden);
    }





    private static float HorizontalFraction(Alignment alignment)
        => alignment == Alignment.Left || alignment == Alignment.TopLeft || alignment == Alignment.CenterLeft || alignment == Alignment.BottomLeft ? 0f
            : alignment == Alignment.CenterHorizontally || alignment == Alignment.TopCenter || alignment == Alignment.Center || alignment == Alignment.BottomCenter ? 0.5f : 1f;

    private static float VerticalFraction(Alignment alignment)
        => alignment == Alignment.Top || alignment == Alignment.TopLeft || alignment == Alignment.TopCenter || alignment == Alignment.TopRight ? 0f
            : alignment == Alignment.CenterVertically || alignment == Alignment.CenterLeft || alignment == Alignment.Center || alignment == Alignment.CenterRight ? 0.5f : 1f;
}
