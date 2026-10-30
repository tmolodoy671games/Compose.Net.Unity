using System;
using Compose.Net;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Runtime.Adapters.Transitions.Exit;

internal sealed class SlideOutExitTransitionImpl : IExitTransition
{
    private readonly Func<Size, Offset> _targetOffset;
    private readonly AnimationSpec _animationSpec;

    public SlideOutExitTransitionImpl(AnimationSpec animationSpec, Func<Size, Offset> targetOffset)
    {
        _targetOffset = targetOffset;
        _animationSpec = animationSpec;
    }

    public void Apply(IReusableComposeNode node, TimeSpan timeElapsed)
    {
        SlideTransitionUtils.Apply(node.VisualElement(), "XY", _animationSpec, timeElapsed, _targetOffset, entering: false);
    }

    public void Revert(IReusableComposeNode node, TimeSpan timeElapsed)
    {
        SlideTransitionUtils.Revert(node.VisualElement(), "XY");
    }

    public TimeSpan TotalDuration => _animationSpec.TotalDuration;

    private bool Equals(SlideOutExitTransitionImpl other)
    {
        return _targetOffset.Equals(other._targetOffset) && _animationSpec.Equals(other._animationSpec);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((SlideOutExitTransitionImpl)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_targetOffset, _animationSpec);
    }
}
