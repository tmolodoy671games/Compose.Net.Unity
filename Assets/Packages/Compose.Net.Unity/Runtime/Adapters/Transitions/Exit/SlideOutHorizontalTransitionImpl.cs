using System;
using Compose.Net;
using SharpExtensions;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Runtime.Adapters.Transitions.Exit;

internal class SlideOutHorizontalTransitionImpl : IExitTransition
{
    private readonly Func<float, float> _targetOffsetX;
    private readonly AnimationSpec _animationSpec;

    public SlideOutHorizontalTransitionImpl(AnimationSpec animationSpec, Func<float, float> targetOffsetX)
    {
        _targetOffsetX = targetOffsetX;
        _animationSpec = animationSpec;
    }

    public void Apply(IReusableComposeNode node, TimeSpan timeElapsed)
    {
        var progress = _animationSpec.GetProgress(timeElapsed);
        var element = node.VisualElement();
        var targetOffsetX = _targetOffsetX(element.LayoutCoordinates().Size.Width);
        var targetOffset = Mathf.LerpUnclamped(0f, targetOffsetX, progress);
        element.style.translate = new Vector2(targetOffset, 0f);
    }

    public void Revert(IReusableComposeNode node, TimeSpan timeElapsed)
    {
        node.VisualElement().style.translate = StyleKeyword.None;
    }

    public TimeSpan TotalDuration => _animationSpec.TotalDuration;

    private bool Equals(SlideOutHorizontalTransitionImpl other)
    {
        return _targetOffsetX.Equals(other._targetOffsetX) && _animationSpec.Equals(other._animationSpec);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((SlideOutHorizontalTransitionImpl)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_targetOffsetX, _animationSpec);
    }
}