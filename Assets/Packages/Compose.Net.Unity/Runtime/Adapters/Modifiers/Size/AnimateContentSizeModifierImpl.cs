// ReSharper disable CheckNamespace

using System;
using Compose.Net;
using SharpExtensions;
using StableCollections;
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
        private AnimationSpec _animationSpec = animationSpec;
        private EventCallback<GeometryChangedEvent>? _callback;

        internal void Update(AnimationSpec next)
        {
            if (_animationSpec == next)
                return;
            if (ContentContainer != null)
            {
                RevertFromContainer(UnityNode!, Element!, ContentContainer);
                _animationSpec = next;
                ApplyToContainer(UnityNode!, Element!, ContentContainer);
            }
            else
                _animationSpec = next;
        }

        protected override void ApplyToContainer(
            UnityReusableComposeNode node,
            VisualElement element,
            VisualElement contentContainer
        )
        {
            var key = new ReferenceKey(this);
            _callback = e =>
            {
                var content = e.VisualElement();
                var targetWidth = content.resolvedStyle.width
                                  + content.resolvedStyle.marginLeft
                                  + content.resolvedStyle.marginRight
                                  + content.resolvedStyle.borderLeftWidth
                                  + content.resolvedStyle.borderRightWidth
                                  + content.parent.resolvedStyle.paddingLeft
                                  + content.parent.resolvedStyle.paddingRight;
                var targetHeight = content.resolvedStyle.height
                                   + content.resolvedStyle.marginTop
                                   + content.resolvedStyle.marginBottom
                                   + content.resolvedStyle.borderTopWidth
                                   + content.resolvedStyle.borderBottomWidth
                                   + content.parent.resolvedStyle.paddingTop
                                   + content.parent.resolvedStyle.paddingBottom;
                var targetSize = new Vector2(targetWidth, targetHeight);
                var previousRecord = content.UserData().GetOrNull(key)?.CastToOrNull<AnimationRecord>();
                if (previousRecord != null && previousRecord.TargetSize == targetSize)
                    return;
                // The first measurement establishes the size; only later changes animate.
                // Set both dimensions before taking the content out of the layout flow.
                if (previousRecord == null)
                {
                    content.parent.style.width = targetWidth;
                    content.parent.style.height = targetHeight;
                    content.style.position = Position.Absolute;
                    content.UserData()[key] = new AnimationRecord(null, targetSize);
                    return;
                }
                previousRecord.Animation?.Stop();
                var initialWidth = content.parent.resolvedStyle.width;
                var initialHeight = content.parent.resolvedStyle.height;
                content.UserData()[key] = new AnimationRecord(
                    TargetSize: targetSize,
                    Animation: content.parent.experimental.animation.Start(
                        0,
                        1,
                        _animationSpec.TotalDuration.TotalMilliseconds.ToFloat().ToInt(),
                        (_, progress) =>
                        {
                            progress = _animationSpec.GetProgress(_animationSpec.TotalDuration * progress);
                            content.parent.style.width = Mathf.LerpUnclamped(
                                initialWidth,
                                targetWidth,
                                progress
                            );

                            content.parent.style.height = Mathf.LerpUnclamped(
                                initialHeight,
                                targetHeight,
                                progress
                            );
                        }
                    ).KeepAlive()
                );
            };
            contentContainer.style.flexShrink = 0;
            contentContainer.RegisterCallback(_callback);
        }

        protected override void RevertFromContainer(
            UnityReusableComposeNode node,
            VisualElement element,
            VisualElement contentContainer
        )
        {
            contentContainer.style.flexShrink = 1;
            if (_callback != null)
                contentContainer.UnregisterCallback(_callback);
            _callback = null;
        }

        private record AnimationRecord(
            ValueAnimation<float>? Animation,
            Vector2 TargetSize
        );
    }
}
