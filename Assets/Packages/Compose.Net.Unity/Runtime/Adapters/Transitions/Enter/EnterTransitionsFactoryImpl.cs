using System;
using Compose.Net;
using SharpExtensions;

namespace Compose.Net.Unity.Runtime.Adapters.Transitions.Enter;

internal class EnterTransitionsFactoryImpl : IEnterTransitionsFactory
{
    public IEnterTransition FadeIn(AnimationSpec animationSpec, float initialAlpha)
    {
        return new FadeInEnterTransitionImpl(animationSpec, initialAlpha);
    }

    public IEnterTransition ScaleIn(AnimationSpec animationSpec, float initialScale)
    {
        return new ScaleInEnterTransitionImpl(animationSpec, initialScale);
    }

    public IEnterTransition SlideIn(AnimationSpec animationSpec, Func<Size, Offset> initialOffset)
    {
        return new SlideInEnterTransitionImpl(animationSpec, initialOffset);
    }

    public IEnterTransition SlideInHorizontally(
        AnimationSpec animationSpec,
        Func<float, float> initialOffsetX
    )
    {
        return new SlideInHorizontallyTransitionImpl(animationSpec, initialOffsetX);
    }

    public IEnterTransition SlideInVertically(
        AnimationSpec animationSpec,
        Func<float, float> initialOffsetY
    )
    {
        return new SlideInVerticallyTransitionImpl(animationSpec, initialOffsetY);
    }

    public IEnterTransition ExpandHorizontally(
        AnimationSpec animationSpec,
        bool clip,
        Func<float, float> initialWidth
    )
    {
        return new ExpandHorizontallyEnterTransitionImpl(animationSpec, clip, initialWidth);
    }

    public IEnterTransition ExpandVertically(
        AnimationSpec animationSpec,
        bool clip,
        Func<float, float> initialHeight
    )
    {
        return new ExpandVerticallyEnterTransitionImpl(animationSpec, clip, initialHeight);
    }

    public IEnterTransition ExpandIn(
        AnimationSpec animationSpec,
        bool clip,
        Func<Size, Size> initialSize
    )
    {
        return new ExpandEnterTransitionImpl(animationSpec, clip, initialSize);
    }
}