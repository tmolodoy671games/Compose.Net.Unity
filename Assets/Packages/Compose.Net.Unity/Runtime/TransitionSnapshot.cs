using Compose.Net;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

public partial class UnityReusableComposeNode : ITransitionSnapshotNode
{
    private readonly struct TransitionSnapshot
    {
        internal readonly float Opacity;
        internal readonly Vector3 Translation, Scale;
        internal readonly StyleFloat RawOpacity;
        internal readonly StyleTranslate RawTranslation;
        internal readonly StyleScale RawScale;
        internal readonly StyleLength Left, Top, MaxWidth, MaxHeight;
        internal readonly float ClipWidth, ClipHeight;
        internal TransitionSnapshot(VisualElement element)
        {
            var style = element.style;
            RawOpacity = style.opacity; RawTranslation = style.translate; RawScale = style.scale;
            Left = style.left; Top = style.top;
            MaxWidth = element.parent != null ? Box.AuthoredBound(element.parent, Box.BoundProperty.MaxWidth) : new StyleLength(StyleKeyword.Null);
            MaxHeight = element.parent != null ? Box.AuthoredBound(element.parent, Box.BoundProperty.MaxHeight) : new StyleLength(StyleKeyword.Null);
            ClipWidth = MaxWidth.keyword == StyleKeyword.Undefined ? MaxWidth.value.value : element.layout.width;
            ClipHeight = MaxHeight.keyword == StyleKeyword.Undefined ? MaxHeight.value.value : element.layout.height;
            Opacity = style.opacity.keyword == StyleKeyword.Undefined ? style.opacity.value : 1;
            var translate = style.translate.value;
            Translation = new Vector3(translate.x.value, translate.y.value, translate.z);
            Scale = style.scale.keyword == StyleKeyword.Undefined ? style.scale.value.value : Vector3.one;
        }
    }
    public object CaptureTransitionState() => new TransitionSnapshot(VisualElement);
    public void RestoreTransitionState(object state)
    {
        var snapshot = (TransitionSnapshot)state;
        var style = VisualElement.style;
        style.opacity = snapshot.RawOpacity; style.translate = snapshot.RawTranslation; style.scale = snapshot.RawScale;
        style.left = snapshot.Left; style.top = snapshot.Top;
        if (VisualElement.parent != null)
        {
            VisualElement.parent.style.maxWidth = snapshot.MaxWidth;
            VisualElement.parent.style.maxHeight = snapshot.MaxHeight;
        }
    }
    public void InterpolateTransitionState(object interrupted, object start, object end, float fallbackProgress)
    {
        var from = (TransitionSnapshot)interrupted; var first = (TransitionSnapshot)start; var last = (TransitionSnapshot)end;
        var current = new TransitionSnapshot(VisualElement);
        float Blend(float initial, float a, float b, float value) => Mathf.LerpUnclamped(initial, b,
            Mathf.Abs(b - a) > .00001f ? (value - a) / (b - a) : fallbackProgress);
        Vector3 BlendVector(Vector3 initial, Vector3 a, Vector3 b, Vector3 value) => new(
            Blend(initial.x, a.x, b.x, value.x), Blend(initial.y, a.y, b.y, value.y), Blend(initial.z, a.z, b.z, value.z));
        if (from.MaxWidth.keyword == StyleKeyword.Undefined || first.MaxWidth.keyword == StyleKeyword.Undefined)
            VisualElement.parent.style.maxWidth = Blend(from.ClipWidth, first.ClipWidth, last.ClipWidth, current.ClipWidth);
        if (from.MaxHeight.keyword == StyleKeyword.Undefined || first.MaxHeight.keyword == StyleKeyword.Undefined)
            VisualElement.parent.style.maxHeight = Blend(from.ClipHeight, first.ClipHeight, last.ClipHeight, current.ClipHeight);
        if (from.Left.keyword == StyleKeyword.Undefined || first.Left.keyword == StyleKeyword.Undefined)
            VisualElement.style.left = Blend(from.Left.value.value, first.Left.value.value, last.Left.value.value, current.Left.value.value);
        if (from.Top.keyword == StyleKeyword.Undefined || first.Top.keyword == StyleKeyword.Undefined)
            VisualElement.style.top = Blend(from.Top.value.value, first.Top.value.value, last.Top.value.value, current.Top.value.value);
        VisualElement.style.opacity = Blend(from.Opacity, first.Opacity, last.Opacity, current.Opacity);
        var translation = BlendVector(from.Translation, first.Translation, last.Translation, current.Translation);
        VisualElement.style.translate = new Translate(translation.x, translation.y, translation.z);
        VisualElement.style.scale = new Scale(BlendVector(from.Scale, first.Scale, last.Scale, current.Scale));
    }
}
