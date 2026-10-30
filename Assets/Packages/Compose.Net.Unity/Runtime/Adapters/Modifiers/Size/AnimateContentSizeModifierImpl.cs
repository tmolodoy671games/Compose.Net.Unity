// ReSharper disable CheckNamespace

using System;
using Compose.Net;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace Compose.Net.Unity;

internal record AnimateContentSizeModifierImpl(
    AnimationSpec AnimationSpec,
    Alignment Alignment,
    Action<Size, Size>? FinishedListener
) : ContentContainerUnityModifier<AnimateContentSizeModifierImpl.AnimateContentSizeNode>
{
    public override AnimateContentSizeNode Create() => new(AnimationSpec, Alignment, FinishedListener);
    public override void Update(AnimateContentSizeNode node) => node.Update(AnimationSpec, Alignment, FinishedListener);

    internal sealed class AnimateContentSizeNode(AnimationSpec animationSpec, Alignment alignment, Action<Size, Size>? finishedListener) : ContentContainerNodeBase
    {
        private AnimationSpec _animationSpec = animationSpec.GetOrDefault();
        private Alignment _alignment = alignment;
        private Action<Size, Size>? _finishedListener = finishedListener;
        private VisualElement? _content;
        private StyleLength _originalLeft;
        private StyleLength _originalTop;
        private StyleEnum<Overflow> _originalOverflow;
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

        internal void Update(AnimationSpec next, Alignment nextAlignment, Action<Size, Size>? listener)
        {
            // Parameter changes affect subsequent animations without restarting the current one.
            _animationSpec = next.GetOrDefault();
            _alignment = nextAlignment;
            _finishedListener = listener;
            if (Element != null && _targetSize is { } target)
                AlignContent(Element, target);
        }

        protected override void ApplyToContainer(
            UnityReusableComposeNode node,
            VisualElement element,
            VisualElement contentContainer
        )
        {
            _content = contentContainer;
            _originalLeft = contentContainer.style.left;
            _originalTop = contentContainer.style.top;
            _originalOverflow = element.style.overflow;
            element.style.overflow = Overflow.Hidden;
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
                AlignContent(element, target);
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
            var initial = new Vector2(element.resolvedStyle.width, element.resolvedStyle.height);
            if (spec.TotalDuration <= TimeSpan.Zero)
            {
                SetSize(element, target);
                NotifyFinished(initial, target);
                return;
            }
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
            ).Ease(progress => progress).OnCompleted(() => NotifyFinished(initial, target)).KeepAlive();
        }

        private void NotifyFinished(Vector2 initial, Vector2 target) =>
            _finishedListener?.Invoke(new Size(initial.x, initial.y), new Size(target.x, target.y));

        private void AlignContent(VisualElement element, Vector2 target)
        {
            if (_content == null) return;
            var horizontal = _alignment == Alignment.TopStart || _alignment == Alignment.CenterStart || _alignment == Alignment.BottomStart ? 0f
                : _alignment == Alignment.TopCenter || _alignment == Alignment.Center || _alignment == Alignment.BottomCenter ? .5f : 1f;
            var vertical = _alignment == Alignment.TopStart || _alignment == Alignment.TopCenter || _alignment == Alignment.TopEnd ? 0f
                : _alignment == Alignment.CenterStart || _alignment == Alignment.Center || _alignment == Alignment.CenterEnd ? .5f : 1f;
            _content.style.left = element.resolvedStyle.paddingLeft + (element.resolvedStyle.width - target.x) * horizontal;
            _content.style.top = element.resolvedStyle.paddingTop + (element.resolvedStyle.height - target.y) * vertical;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private void SetSize(VisualElement element, Vector2 size)
        {
            element.style.width = size.x;
            element.style.height = size.y;
            if (_targetSize is { } target) AlignContent(element, target);
        }

        private void StopAnimation()
        {
            if (_animation == null)
                return;
            // Stop also invokes completion in UI Toolkit; interrupted animations must stay silent.
            _animation.onAnimationCompleted = null;
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
            _content = null;
            element.style.overflow = _originalOverflow;
            contentContainer.style.left = _originalLeft;
            contentContainer.style.top = _originalTop;
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
