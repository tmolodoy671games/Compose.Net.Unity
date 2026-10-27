using System;
using Compose.Net;
using SharpExtensions;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Runtime.Adapters.Transitions.Exit;

internal class SlideOutVerticalTransitionImpl : IExitTransition, IUnityStyleTransition
{
    public IModifier CreateModifier(TimeSpan timeElapsed) => new UnityTransitionModifier(this, timeElapsed);
    public void UpdateStyle(UnityStyleModifierNode node, TimeSpan timeElapsed)
    {
        var progress = _animationSpec.GetProgress(timeElapsed);
        var size = node.Element.LayoutCoordinates().Size;
        var offset = new Offset(0, _targetOffsetY(size.Height));
        var value = Offset.LerpUnclamped(Offset.Zero, offset, progress);
        node.Set<StyleTranslate>(node.Styles.Translate, new Translate(value.X, value.Y));
    }

    private readonly Func<float, float> _targetOffsetY;
    private readonly AnimationSpec _animationSpec;

    public SlideOutVerticalTransitionImpl(AnimationSpec animationSpec, Func<float, float> targetOffsetY)
    {
        _targetOffsetY = targetOffsetY;
        _animationSpec = animationSpec;
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