using System;
using Compose.Net;
using SharpExtensions;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Runtime.Adapters.Transitions.Exit;

internal class SlideOutVerticalTransitionImpl : IExitTransition
{
    private readonly Func<float, float> _targetOffsetY;
    private readonly AnimationSpec _animationSpec;

    public SlideOutVerticalTransitionImpl(AnimationSpec animationSpec, Func<float, float> targetOffsetY)
    {
        _targetOffsetY = targetOffsetY;
        _animationSpec = animationSpec;
    }

    public void Apply(IReusableComposeNode node, TimeSpan timeElapsed)
    {
        SlideTransitionUtils.Apply(node.VisualElement(), "Y", _animationSpec, timeElapsed,
            size => new Offset(0, _targetOffsetY(size.Height)), entering: false);
    }

    public void Revert(IReusableComposeNode node, TimeSpan timeElapsed)
    {
        SlideTransitionUtils.Revert(node.VisualElement(), "Y");
    }

    public TimeSpan TotalDuration => _animationSpec.TotalDuration;

    private bool Equals(SlideOutVerticalTransitionImpl other)
    {
        return _targetOffsetY.Equals(other._targetOffsetY) && _animationSpec.Equals(other._animationSpec);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((SlideOutVerticalTransitionImpl)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_targetOffsetY, _animationSpec);
    }
}
