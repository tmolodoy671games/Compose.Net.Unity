using System;
using Compose.Net;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Runtime.Adapters.Transitions.Exit;

internal class ScaleOutExitTransitionImpl : IExitTransition
{
    private readonly float _targetScale;
    private readonly AnimationSpec _animationSpec;

    public ScaleOutExitTransitionImpl(AnimationSpec animationSpec, float targetScale)
    {
        _targetScale = targetScale;
        _animationSpec = animationSpec;
    }

    public void Apply(IReusableComposeNode node, TimeSpan timeElapsed)
    {
        var progress = _animationSpec.GetProgress(timeElapsed);
        node.VisualElement().style.scale = Vector2.one * Mathf.LerpUnclamped(1, _targetScale, progress);
    }

    public void Revert(IReusableComposeNode node, TimeSpan timeElapsed)
    {
        node.VisualElement().style.scale = StyleKeyword.None;
    }

    public TimeSpan TotalDuration => _animationSpec.TotalDuration;

    private bool Equals(ScaleOutExitTransitionImpl other)
    {
        return _targetScale.Equals(other._targetScale) && _animationSpec.Equals(other._animationSpec);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((ScaleOutExitTransitionImpl)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_targetScale, _animationSpec);
    }
}