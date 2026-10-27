using System;
using Compose.Net;
using SharpExtensions;

namespace Compose.Net.Unity.Runtime.Adapters.Transitions.Exit;

internal class ExitTransitionsFactoryImpl : IExitTransitionsFactory
{
    public IExitTransition FadeOut(AnimationSpec animationSpec, float targetAlpha)
    {
        return new FadeOutExitTransitionImpl(animationSpec, targetAlpha);
    }

    public IExitTransition ScaleOut(AnimationSpec animationSpec, float targetScale, Net.TransformOrigin transformOrigin)
    {
        return new ScaleOutExitTransitionImpl(animationSpec, targetScale, transformOrigin);
    }

    public IExitTransition SlideOut(AnimationSpec animationSpec, Func<Size, Offset> targetOffset)
    {
        return new SlideOutExitTransitionImpl(animationSpec, targetOffset);
    }

    public IExitTransition SlideOutHorizontally(
        AnimationSpec animationSpec,
        Func<float, float> targetOffsetX
    )
    {
        return new SlideOutHorizontalTransitionImpl(animationSpec, targetOffsetX);
    }

    public IExitTransition SlideOutVertically(AnimationSpec animationSpec, Func<float, float> targetOffsetY)
    {
        return new SlideOutVerticalTransitionImpl(animationSpec, targetOffsetY);
    }

    public IExitTransition ShrinkHorizontally(
        AnimationSpec animationSpec,
        Alignment.Horizontal shrinkTowards,
        bool clip,
        Func<float, float> targetWidth
    )
    {
        return new ShrinkHorizontallyExitTransitionImpl(animationSpec, shrinkTowards, clip, targetWidth);
    }

    public IExitTransition ShrinkVertically(
        AnimationSpec animationSpec,
        Alignment.Vertical shrinkTowards,
        bool clip,
        Func<float, float> targetHeight
    )
    {
        return new ShrinkVerticallyExitTransitionImpl(animationSpec, shrinkTowards, clip, targetHeight);
    }

    public IExitTransition ShrinkOut(
        AnimationSpec animationSpec,
        Alignment shrinkTowards,
        bool clip,
        Func<Size, Size> targetSize
    )
    {
        return new ShrinkOutExitTransitionImpl(animationSpec, shrinkTowards, clip, targetSize);
    }
}