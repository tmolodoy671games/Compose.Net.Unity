// ReSharper disable CheckNamespace

using System;
using Compose.Net;
using Compose.Net.Unity.Runtime.Adapters.Extensions;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace Compose.Net.Unity;

[AiGenerated]
internal record AnimateContentSizeModifierImpl(
    AnimationSpec AnimationSpec,
    Alignment Alignment,
    Action<Size, Size>? FinishedListener
) : ContentContainerUnityModifier<AnimateContentSizeModifierImpl.AnimateContentSizeNode>
{
    public override AnimateContentSizeNode Create() => new(AnimationSpec, Alignment, FinishedListener);

    public override void Update(AnimateContentSizeNode node) => node.Update(AnimationSpec, Alignment, FinishedListener);

    internal sealed class AnimateContentSizeNode(
        AnimationSpec animationSpec,
        Alignment alignment,
        Action<Size, Size>? finishedListener
    ) : ContentContainerNodeBase
    {
        private AnimationSpec _animationSpec = animationSpec;
        private Alignment _alignment = alignment;
        private Action<Size, Size>? _finishedListener = finishedListener;
        private ValueAnimation<Vector2>? _animation;
        private Vector2? _targetSize;
        private StyleLength _width, _height, _left, _top;
        private StyleEnum<Position> _position;
        private StyleFloat _flexShrink;
        private StyleTranslate _translate;
        private IVisualElementScheduledItem? _measurement;

        internal void Update(AnimationSpec spec, Alignment nextAlignment, Action<Size, Size>? listener)
        {
            // Parameter updates must not restart a running animation.
            _animationSpec = spec;
            _alignment = nextAlignment;
            _finishedListener = listener;
            AlignContent();
        }

        protected override void ApplyToContainer(
            UnityReusableComposeNode node,
            VisualElement element,
            VisualElement contentContainer
        )
        {
            _width = element.style.width;
            _height = element.style.height;
            Box.SetAnimatedConstraints(element, Box.AnimatedConstraints.Width | Box.AnimatedConstraints.Height, true);
            _left = contentContainer.style.left;
            _top = contentContainer.style.top;
            _position = contentContainer.style.position;
            _flexShrink = contentContainer.style.flexShrink;
            _translate = contentContainer.style.translate;
            contentContainer.style.flexShrink = 0;
            contentContainer.RegisterCallback<GeometryChangedEvent>(OnContentGeometryChanged);
            element.RegisterCallback<GeometryChangedEvent>(OnElementGeometryChanged);
            element.Clip().Increment();
        }

        private void OnContentGeometryChanged(GeometryChangedEvent evt)
        {
            if (evt.target != ContentContainer || Element == null || ContentContainer == null)
                return;

            if (evt.oldRect.size != evt.newRect.size)
            {
                // Coalesce measurements after layout. Size/position writes during this event
                // otherwise restart the same layout pass, especially on fractional-DPI panels.
                if (_measurement == null) _measurement = Element.schedule.Execute(MeasureContent);
                else _measurement.ExecuteLater(0);
            }
            AlignContent();
        }

        private void MeasureContent()
        {
            if (Element?.panel == null || ContentContainer?.panel == null) return;

            var content = ContentContainer.resolvedStyle;
            var outer = Element.resolvedStyle;
            // UI Toolkit layout sizes already include the content's padding and borders.
            var target = ContentContainer.layout.size + new Vector2(
                content.marginLeft + content.marginRight + outer.paddingLeft + outer.paddingRight
                + outer.borderLeftWidth + outer.borderRightWidth,
                content.marginTop + content.marginBottom + outer.paddingTop + outer.paddingBottom
                + outer.borderTopWidth + outer.borderBottomWidth
            );
            if (!float.IsFinite(target.x) || !float.IsFinite(target.y))
                return;

            if (_targetSize == null)
            {
                // The first measurement establishes the size without an enter animation.
                _targetSize = target;
                Element.style.width = target.x;
                Element.style.height = target.y;
                ContentContainer.style.position = Position.Absolute;
                ContentContainer.style.left = 0;
                ContentContainer.style.top = 0;
            }
            // Pixel alignment at fractional DPI introduces small float errors when the
            // centered parent moves. They must not keep restarting the native animation.
            else if (!Mathf.Approximately(_targetSize.Value.x, target.x)
                || !Mathf.Approximately(_targetSize.Value.y, target.y))
            {
                AnimateTo(target);
            }
            AlignContent();
        }

        private void AnimateTo(Vector2 target)
        {
            StopAnimation();
            var element = Element!;
            var initial = new Vector2(element.resolvedStyle.width, element.resolvedStyle.height);
            _targetSize = target;
            var spec = _animationSpec;
            if (spec.TotalDuration <= TimeSpan.Zero)
            {
                element.style.width = target.x;
                element.style.height = target.y;
                _finishedListener?.Invoke(ToSize(initial), ToSize(target));
                return;
            }

            // UI Toolkit owns interpolation, frame updates, and recycling. Ease also accounts
            // for the spec's delay; using Start's default OutQuad here would apply easing twice.
            _animation = element.experimental.animation.Size(target, (int)Math.Min(int.MaxValue, spec.TotalDuration.TotalMilliseconds))
                .Ease(progress => spec.Duration <= TimeSpan.Zero
                    ? (progress < 1f ? 0f : 1f)
                    : spec.GetProgress(spec.TotalDuration * progress))
                .OnCompleted(() =>
                {
                    _animation = null;
                    _finishedListener?.Invoke(ToSize(initial), ToSize(target));
                });
            _animation.from = initial;
        }

        private void OnElementGeometryChanged(GeometryChangedEvent evt)
        {
            if (evt.target == Element)
                AlignContent();
        }

        private void AlignContent()
        {
            if (_targetSize == null || Element == null || ContentContainer == null)
                return;
            var outer = Element.resolvedStyle;
            var content = ContentContainer.resolvedStyle;
            var horizontal = _alignment.ToAlign() switch
            {
                Align.Center => 0.5f,
                Align.FlexEnd => 1f,
                _ => 0f
            };
            var vertical = _alignment.ToJustify() switch
            {
                Justify.Center => 0.5f,
                Justify.FlexEnd => 1f,
                _ => 0f
            };
            // Translation affects rendering/picking without changing the measured content size.
            ContentContainer.style.translate = new Translate(
                outer.paddingLeft + (Element.contentRect.width - content.width - content.marginLeft - content.marginRight) * horizontal,
                outer.paddingTop + (Element.contentRect.height - content.height - content.marginTop - content.marginBottom) * vertical);
        }

        private void StopAnimation()
        {
            var animation = _animation;
            _animation = null;
            if (animation == null)
                return;
            // Stop invokes OnCompleted too. An interruption is not a successful completion.
            animation.onAnimationCompleted = null;
            animation.Stop();
        }

        protected override void RevertFromContainer(
            UnityReusableComposeNode node,
            VisualElement element,
            VisualElement contentContainer
        )
        {
            StopAnimation();
            _measurement?.Pause();
            _measurement = null;
            contentContainer.UnregisterCallback<GeometryChangedEvent>(OnContentGeometryChanged);
            element.UnregisterCallback<GeometryChangedEvent>(OnElementGeometryChanged);
            element.style.width = _width;
            element.style.height = _height;
            Box.SetAnimatedConstraints(element, Box.AnimatedConstraints.Width | Box.AnimatedConstraints.Height, false);
            contentContainer.style.left = _left;
            contentContainer.style.top = _top;
            contentContainer.style.position = _position;
            contentContainer.style.flexShrink = _flexShrink;
            contentContainer.style.translate = _translate;
            element.Clip().Decrement();
            _targetSize = null;
        }

        private static Size ToSize(Vector2 size) => new(size.x, size.y);
    }
}
