using Compose.Net;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal static class RoundedCorners
{
    // UI Toolkit clamps X/Y independently, producing elliptical caps for a large radius.
    // Drawing and clipping must use the same circular radii in local coordinates.
    internal static Vector4 Resolve(Shape shape, Vector2 size)
    {
        var limit = Mathf.Max(0, Mathf.Min(size.x, size.y) / 2);
        var density = UnityDensity.Current;
        return new Vector4(Mathf.Clamp(shape.TopLeft.ToPx(density), 0, limit),
            Mathf.Clamp(shape.TopRight.ToPx(density), 0, limit),
            Mathf.Clamp(shape.BottomLeft.ToPx(density), 0, limit),
            Mathf.Clamp(shape.BottomRight.ToPx(density), 0, limit));
    }
    internal static void Apply(VisualElement element, Shape shape)
    {
        Apply(element, shape, element.layout.size);
    }

    // Animated overlay bounds are known before the native layout pass catches up.
    internal static void Apply(VisualElement element, Shape shape, Vector2 size)
    {
        if (!float.IsFinite(size.x) || !float.IsFinite(size.y)) return;
        Apply(element, Resolve(shape, size));
    }

    internal static void Apply(VisualElement element, Vector4 radii)
    {
        element.style.borderTopLeftRadius = radii.x;
        element.style.borderTopRightRadius = radii.y;
        element.style.borderBottomLeftRadius = radii.z;
        element.style.borderBottomRightRadius = radii.w;
    }
}
