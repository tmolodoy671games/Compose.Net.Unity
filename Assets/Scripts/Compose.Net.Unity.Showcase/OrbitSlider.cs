using System;
using Compose.Net;
using Compose.Net.Showcase;
using UnityEngine;
using UnityEngine.UIElements;
using static Compose.Net.ComposeFunctions;

namespace Compose.Net.Unity.Showcase;

public sealed partial class OrbitUnityHost
{
    [Composable] public void Slider(float value, Action<float> onChange, string tag, IModifier modifier, OrbitPalette palette, Compose.Net.Color accent)
    {
        ReusableComposeNode(nodeFactory: () => new SliderNode(), modifier: modifier.TestTag(tag), initializer: node =>
        {
            var slider = (SliderNode)node;
            slider.Changed = onChange;
            slider.Field.SetValueWithoutNotify(Mathf.Clamp01(value));
            slider.Fill.style.width = Length.Percent(Mathf.Clamp01(value) * 100);
            slider.Track.style.backgroundColor = ToUnity(palette.Line);
            slider.Fill.style.backgroundColor = ToUnity(accent);
            slider.Thumb.style.backgroundColor = ToUnity(accent);
            slider.Thumb.style.borderTopColor = slider.Thumb.style.borderBottomColor =
                slider.Thumb.style.borderLeftColor = slider.Thumb.style.borderRightColor = ToUnity(palette.Surface);
            slider.Field.tooltip = "Material texture opacity: " + Mathf.RoundToInt(value * 100) + "%";
        });
    }

    private sealed class SliderNode : UnityReusableComposeNode
    {
        internal readonly UnityEngine.UIElements.Slider Field;
        internal readonly VisualElement Track, Thumb, Fill;
        internal Action<float> Changed = _ => { };
        internal SliderNode() : this(new UnityEngine.UIElements.Slider(0, 1)) { }
        private SliderNode(UnityEngine.UIElements.Slider field) : base(field)
        {
            Field = field;
            field.pickingMode = UnityEngine.UIElements.PickingMode.Position;
            field.focusable = true;
            field.pageSize = .1f;
            field.style.marginLeft = field.style.marginRight = field.style.marginTop = field.style.marginBottom = 0;
            Track = field.Q(className: "unity-base-slider__tracker");
            Thumb = field.Q(className: "unity-base-slider__dragger");
            Track.style.height = 5;
            Track.style.borderTopLeftRadius = Track.style.borderTopRightRadius =
                Track.style.borderBottomLeftRadius = Track.style.borderBottomRightRadius = 3;
            Track.style.overflow = Overflow.Hidden;
            Fill = new VisualElement { pickingMode = UnityEngine.UIElements.PickingMode.Ignore };
            Fill.style.position = Position.Absolute;
            Fill.style.top = Fill.style.bottom = Fill.style.left = 0;
            Track.Add(Fill);
            Thumb.style.width = Thumb.style.height = 18;
            Thumb.style.borderTopLeftRadius = Thumb.style.borderTopRightRadius =
                Thumb.style.borderBottomLeftRadius = Thumb.style.borderBottomRightRadius = 9;
            Thumb.style.borderTopWidth = Thumb.style.borderBottomWidth =
                Thumb.style.borderLeftWidth = Thumb.style.borderRightWidth = 2;
            field.RegisterValueChangedCallback(evt =>
            {
                var next = Mathf.Clamp01(evt.newValue);
                Fill.style.width = Length.Percent(next * 100);
                Changed(next);
            });
        }
    }
}
