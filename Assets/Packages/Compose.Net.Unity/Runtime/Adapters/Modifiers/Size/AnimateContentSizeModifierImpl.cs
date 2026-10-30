// ReSharper disable CheckNamespace

using System;
using Compose.Net;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace Compose.Net.Unity;

internal record AnimateContentSizeModifierImpl(
    AnimationSpec AnimationSpec
) : ContentContainerUnityModifier<AnimateContentSizeModifierImpl.AnimateContentSizeNode>
{
    public override AnimateContentSizeNode Create() => new(AnimationSpec);
    public override void Update(AnimateContentSizeNode node) => node.Update(AnimationSpec);

    internal sealed class AnimateContentSizeNode(AnimationSpec animationSpec) : ContentContainerNodeBase
    {
        private AnimationSpec _animationSpec = animationSpec.GetOrDefault();
        private EventCallback<GeometryChangedEvent>? _callback;
        private ValueAnimation<float>? _animation;
        private IVisualElementScheduledItem? _initialLayout;
        private Vector2? _targetSize;
        private bool _initializing;
        private StyleLength _originalWidth;
        private StyleLength _originalHeight;
        private StyleEnum<Position> _originalPosition;
        private StyleFloat _originalShrink;
        private StyleEnum<FlexDirection> _originalDirection;
        private StyleEnum<Align> _originalAlign;
        private StyleEnum<Justify> _originalJustify;

        internal void Update(AnimationSpec next)
        {
            next = next.GetOrDefault();
            if (_animationSpec == next)
                return;
            _animationSpec = next;
            if (_animation is { isRunning: true } && Element != null && _targetSize is { } target)
                Animate(Element, target);
        }

        protected override void ApplyToContainer(
            UnityReusableComposeNode node,
            VisualElement element,
            VisualElement contentContainer
        )
        {
            _originalWidth = element.style.width;
            _originalHeight = element.style.height;
            _originalPosition = contentContainer.style.position;
            _originalShrink = contentContainer.style.flexShrink;
            _originalDirection = contentContainer.style.flexDirection;
            _originalAlign = contentContainer.style.alignItems;
            _originalJustify = contentContainer.style.justifyContent;
            _targetSize = null;
            _initializing = true;

            // Preserve Row/Column layout for every child; Box has its own stacking layout.
            if (element is not Box)
            {
                contentContainer.style.flexDirection = element.style.flexDirection;
                contentContainer.style.alignItems = element.style.alignItems;
                contentContainer.style.justifyContent = element.style.justifyContent;
            }
            contentContainer.style.flexShrink = 0;

            _callback = _ =>
            {
                // Compose applies the node initializer after attaching modifiers. In a Row,
                // flexDirection may therefore change after the content container was acquired.
                if (element is not Box)
                {
                    contentContainer.style.flexDirection = element.style.flexDirection;
                    contentContainer.style.alignItems = element.style.alignItems;
                    contentContainer.style.justifyContent = element.style.justifyContent;
                }
                var contentStyle = contentContainer.resolvedStyle;
                var hostStyle = element.resolvedStyle;
                // layout.size already includes the content container's own borders.
                var target = new Vector2(
                    contentContainer.layout.width + contentStyle.marginLeft + contentStyle.marginRight
                    + hostStyle.paddingLeft + hostStyle.paddingRight
                    + hostStyle.borderLeftWidth + hostStyle.borderRightWidth,
                    contentContainer.layout.height + contentStyle.marginTop + contentStyle.marginBottom
                    + hostStyle.paddingTop + hostStyle.paddingBottom
                    + hostStyle.borderTopWidth + hostStyle.borderBottomWidth
                );
                if (!IsFinite(target.x) || !IsFinite(target.y))
                    return;
                if (_targetSize == target)
                    return;
                _targetSize = target;

                if (_initializing)
                {
                    SetSize(element, target);
                    contentContainer.style.position = Position.Absolute;
                    // Box may need several layout passes to stack multiple children.
                    // All measurements before the first paint establish the initial size.
                    _initialLayout ??= element.schedule.Execute(() =>
                    {
                        _initializing = false;
                        _initialLayout = null;
                    });
                }
                else
                    Animate(element, target);
            };
            contentContainer.RegisterCallback(_callback);
            element.RegisterCallback(_callback);
        }

        private void Animate(VisualElement element, Vector2 target)
        {
            StopAnimation();
            var spec = _animationSpec;
            if (spec.TotalDuration <= TimeSpan.Zero)
            {
                SetSize(element, target);
                return;
            }
            var initial = new Vector2(element.resolvedStyle.width, element.resolvedStyle.height);
            _animation = element.experimental.animation.Start(
                0f, 1f, (int)Math.Min(int.MaxValue, Math.Max(1, spec.TotalDuration.TotalMilliseconds)),
                (_, fraction) =>
                {
                    var elapsed = spec.TotalDuration * fraction;
                    var progress = spec.Duration <= TimeSpan.Zero
                        ? (elapsed >= spec.Delay ? 1f : 0f)
                        : spec.GetProgress(elapsed);
                    SetSize(element, Vector2.LerpUnclamped(initial, target, progress));
                }
            ).Ease(progress => progress).KeepAlive();
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private static void SetSize(VisualElement element, Vector2 size)
        {
            element.style.width = size.x;
            element.style.height = size.y;
        }

        private void StopAnimation()
        {
            if (_animation == null)
                return;
            _animation.Stop();
            _animation.Recycle();
            _animation = null;
        }

        protected override void RevertFromContainer(
            UnityReusableComposeNode node,
            VisualElement element,
            VisualElement contentContainer
        )
        {
            if (_callback != null)
            {
                contentContainer.UnregisterCallback(_callback);
                element.UnregisterCallback(_callback);
            }
            _callback = null;
            _initialLayout?.Pause();
            _initialLayout = null;
            StopAnimation();
            _targetSize = null;
            element.style.width = _originalWidth;
            element.style.height = _originalHeight;
            contentContainer.style.position = _originalPosition;
            contentContainer.style.flexShrink = _originalShrink;
            contentContainer.style.flexDirection = _originalDirection;
            contentContainer.style.alignItems = _originalAlign;
            contentContainer.style.justifyContent = _originalJustify;
        }
    }
}
