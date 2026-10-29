using System;
using Compose.Net;
using SharpExtensions;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Runtime.Adapters.Transitions;

internal static class SizeTransitionUtils
{
    public static bool DeferUntilMeasured(VisualElement element, Action apply, bool horizontal, bool vertical)
    {
        if (!float.IsNaN(element.layout.width) && !float.IsNaN(element.layout.height)) return false;
        var key = StateKey(horizontal, vertical);
        if (element.UserData().GetOrDefault(key, null) is PendingLayout previous) previous.Cancel();
        element.UserData()[key] = new PendingLayout(element, apply, key);
        return true;
    }

    public static void Apply(VisualElement element, Size full, Size other, float progress,
        bool entering, Alignment alignment, bool clip, bool horizontal, bool vertical)
    {
        var parent = element.parent;
        if (parent == null || float.IsNaN(full.Width) || float.IsNaN(full.Height)
            || entering && progress >= 1f) return;
        var key = StateKey(horizontal, vertical);
        var state = new AppliedState(element, parent);
        element.UserData()[key] = state;
        var t = entering ? progress : 1f - progress;
        var width = Mathf.Max(0, Mathf.LerpUnclamped(other.Width, full.Width, t));
        var height = Mathf.Max(0, Mathf.LerpUnclamped(other.Height, full.Height, t));
        element.style.flexShrink = 0;
        // Preserve both axes: constraining width can also change Yoga's auto height.
        element.style.width = full.Width;
        element.style.height = full.Height;
        if (horizontal)
        {
            // Keep the content at its natural size. Otherwise Yoga measures the
            // auto-sized content against the collapsed parent on the next frame.
            var style = parent.resolvedStyle;
            parent.style.maxWidth = width + style.paddingLeft + style.paddingRight
                + style.borderLeftWidth + style.borderRightWidth;
            element.style.left = (width - full.Width) * HorizontalFraction(alignment);
        }
        if (vertical)
        {
            var style = parent.resolvedStyle;
            parent.style.maxHeight = height + style.paddingTop + style.paddingBottom
                + style.borderTopWidth + style.borderBottomWidth;
            element.style.top = (height - full.Height) * VerticalFraction(alignment);
        }
        if (clip) parent.Clip().Increment();
    }

    public static void Revert(VisualElement element, bool clip, bool horizontal, bool vertical)
    {
        var data = element.UserData();
        var key = StateKey(horizontal, vertical);
        if (data.GetOrDefault(key, null) is PendingLayout pending)
        {
            pending.Cancel();
            data.Remove(key);
            return;
        }
        if (data.GetOrDefault(key, null) is not AppliedState state) return;
        data.Remove(key);
        var parent = state.Parent;
        element.style.flexShrink = state.FlexShrink;
        element.style.width = state.Width;
        element.style.height = state.Height;
        if (horizontal)
        {
            parent.style.maxWidth = state.MaxWidth;
            element.style.left = state.Left;
        }
        if (vertical)
        {
            parent.style.maxHeight = state.MaxHeight;
            element.style.top = state.Top;
        }
        if (clip) parent.Clip().Decrement();
    }

    private static string StateKey(bool horizontal, bool vertical) =>
        horizontal ? vertical ? "Compose.Net.Unity_SizeTransitionXY" : "Compose.Net.Unity_SizeTransitionX"
            : "Compose.Net.Unity_SizeTransitionY";

    private sealed class PendingLayout
    {
        private readonly VisualElement _element;
        private readonly EventCallback<GeometryChangedEvent> _callback;

        public PendingLayout(VisualElement element, Action apply, string key)
        {
            _element = element;
            _callback = evt =>
            {
                if (evt.target != element || float.IsNaN(element.layout.width) || float.IsNaN(element.layout.height)) return;
                Cancel();
                element.UserData().Remove(key);
                // Geometry changes are dispatched during layout, before painting.
                // Apply the initial size in that pass, rather than showing a full-size frame.
                apply();
            };
            element.RegisterCallback(_callback);
        }

        public void Cancel() => _element.UnregisterCallback(_callback);
    }

    private sealed class AppliedState
    {
        public readonly VisualElement Parent;
        public readonly StyleFloat FlexShrink;
        public readonly StyleLength Width, Height, MaxWidth, MaxHeight, Left, Top;

        public AppliedState(VisualElement element, VisualElement parent)
        {
            Parent = parent;
            FlexShrink = element.style.flexShrink;
            Width = element.style.width;
            Height = element.style.height;
            MaxWidth = parent.style.maxWidth;
            MaxHeight = parent.style.maxHeight;
            Left = element.style.left;
            Top = element.style.top;
        }
    }

    private static float HorizontalFraction(Alignment alignment)
        => alignment == Alignment.Left || alignment == Alignment.TopLeft || alignment == Alignment.CenterLeft || alignment == Alignment.BottomLeft ? 0f
            : alignment == Alignment.CenterHorizontally || alignment == Alignment.TopCenter || alignment == Alignment.Center || alignment == Alignment.BottomCenter ? 0.5f : 1f;

    private static float VerticalFraction(Alignment alignment)
        => alignment == Alignment.Top || alignment == Alignment.TopLeft || alignment == Alignment.TopCenter || alignment == Alignment.TopRight ? 0f
            : alignment == Alignment.CenterVertically || alignment == Alignment.CenterLeft || alignment == Alignment.Center || alignment == Alignment.CenterRight ? 0.5f : 1f;
}
