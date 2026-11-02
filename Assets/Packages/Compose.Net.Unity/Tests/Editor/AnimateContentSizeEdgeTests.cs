using System;
using Compose.Net;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using static Compose.Net.ComposeFunctions;

namespace Compose.Net.Unity.Tests;

public sealed partial class AnimateContentSizeTests
{
    [TestCase(200, 50)]
    [TestCase(100, 150)]
    [TestCase(200, 150)]
    [TestCase(0, 0)]
    public void EachAxisAndZeroTargetInterpolateIndependently(float width, float height)
    {
        Create(); Resize(width, height); Frame(250);
        Assert.That(_host.layout.width, Is.EqualTo(100 + (width - 100) * 0.25f).Within(1));
        Assert.That(_host.layout.height, Is.EqualTo(50 + (height - 50) * 0.25f).Within(1));
        Frame(750);
        Assert.That(_host.layout.size, Is.EqualTo(new Vector2(width, height)));
        Assert.That(_finished.Count, Is.EqualTo(1));
    }

    [TestCase(0, 0)] [TestCase(1, 0)] [TestCase(2, 0)]
    [TestCase(0, 1)] [TestCase(1, 1)] [TestCase(2, 1)]
    [TestCase(0, 2)] [TestCase(1, 2)] [TestCase(2, 2)]
    public void EveryAlignmentAnchorsGrowthAndShrink(int x, int y)
    {
        var alignments = new[] { Alignment.TopStart, Alignment.TopCenter, Alignment.TopEnd,
            Alignment.CenterStart, Alignment.Center, Alignment.CenterEnd,
            Alignment.BottomStart, Alignment.BottomCenter, Alignment.BottomEnd };
        Create(alignment: alignments[y * 3 + x]); Resize(200, 150); Frame(500);
        Assert.That(_content.layout.x, Is.EqualTo(-25 * x).Within(1));
        Assert.That(_content.layout.y, Is.EqualTo(-25 * y).Within(1));
        Frame(500); Resize(100, 50); Frame(500);
        Assert.That(_content.layout.x, Is.EqualTo(25 * x).Within(1));
        Assert.That(_content.layout.y, Is.EqualTo(25 * y).Within(1));
    }

    [TestCase(0, 0.25f)]
    [TestCase(1, 0.015625f)]
    [TestCase(2, 0.578125f)]
    [TestCase(3, 0.125f)]
    [TestCase(4, 0.0625f)]
    public void EasingIsAppliedExactlyOnce(int mode, float expected)
    {
        var easings = new[] { LinearEasing, EaseInEasing, EaseOutEasing, EaseInOutEasing, Easing(t => t * t) };
        Create(Tween(1000, easing: easings[mode])); Resize(200, 150); Frame(250);
        Assert.That(_host.layout.width, Is.EqualTo(100 + 100 * expected).Within(1));
        Assert.That(_host.layout.height, Is.EqualTo(50 + 100 * expected).Within(1));
    }

    [Test]
    public void ZeroDurationWithDelayWaitsThenSnapsOnce()
    {
        Create(Tween(0, 500)); Resize(200, 150); Frame(499);
        Assert.That(_host.layout.width, Is.EqualTo(100)); Assert.That(_finished, Is.Empty);
        Frame(1); Assert.That(_host.layout.width, Is.EqualTo(200));
        Assert.That(_finished.Count, Is.EqualTo(1));
        Frame(1000); Assert.That(_finished.Count, Is.EqualTo(1));
    }

    [Test]
    public void OneMillisecondDurationFinishesWithoutNan()
    {
        Create(Tween(1)); Resize(200, 150); Frame(1);
        Assert.That(_host.layout.size, Is.EqualTo(new Vector2(200, 150)));
        Assert.That(_finished.Count, Is.EqualTo(1));
    }

    [Test]
    public void UnchangedSizeAndPositionChangesDoNotRestartOrNotify()
    {
        Create(); Resize(100, 50); Frame(1000); Assert.That(_finished, Is.Empty);
        Resize(200, 150); Frame(400);
        _host.style.left = 20; Resize(200, 150); Frame(600);
        Assert.That(_host.layout.width, Is.EqualTo(200)); Assert.That(_finished.Count, Is.EqualTo(1));
        Frame(1000); Assert.That(_finished.Count, Is.EqualTo(1));
    }

    [Test]
    public void TwentyRetargetsCompleteOnlyTheLastAnimation()
    {
        Create();
        for (var i = 0; i < 20; i++)
        {
            Resize(i % 2 == 0 ? 300 : 50, i % 2 == 0 ? 150 : 25); Frame(50);
            Assert.That(_finished, Is.Empty);
        }
        var current = new Size(_host.layout.width, _host.layout.height);
        Resize(180, 90); Frame(1000);
        Assert.That(_finished.Count, Is.EqualTo(1));
        Assert.That(_finished[0].initial, Is.EqualTo(current));
        Assert.That(_finished[0].target, Is.EqualTo(new Size(180, 90)));
    }

    [Test]
    public void RetargetDuringDelayDoesNotVisitTheCancelledSize()
    {
        Create(Tween(1000, 500, LinearEasing)); Resize(300, 150); Frame(250);
        Resize(200, 100); Frame(500);
        Assert.That(_host.layout.size, Is.EqualTo(new Vector2(100, 50)));
        Frame(1000); Assert.That(_finished.Count, Is.EqualTo(1));
        Assert.That(_finished[0].initial, Is.EqualTo(new Size(100, 50)));
        Assert.That(_finished[0].target, Is.EqualTo(new Size(200, 100)));
    }

    [Test]
    public void LatestSpecAffectsNextAnimationOnly()
    {
        Create(); Resize(200, 150); Frame(500);
        Description(Tween(2000, easing: LinearEasing), Alignment.TopStart, (a, b) => _finished.Add((a, b))).Update(_node);
        Frame(500); Assert.That(_finished.Count, Is.EqualTo(1));
        Resize(100, 50); Frame(1000);
        Assert.That(_host.layout.width, Is.EqualTo(150).Within(1));
        Assert.That(_finished.Count, Is.EqualTo(1));
        Frame(1000); Assert.That(_finished.Count, Is.EqualTo(2));
    }

    [Test]
    public void ListenerCanBeRemovedWhileAnimating()
    {
        Create(); Resize(200, 150); Frame(500);
        Description(Tween(1000), Alignment.TopStart, null).Update(_node);
        Frame(500); Assert.That(_host.layout.width, Is.EqualTo(200)); Assert.That(_finished, Is.Empty);
    }

    [Test]
    public void CompletionMayResizeAgainWithoutReusingARecycledAnimation()
    {
        Create();
        Description(Tween(1000, easing: LinearEasing), Alignment.TopStart, (a, b) =>
        {
            _finished.Add((a, b));
            if (_finished.Count == 1) { _child.style.width = 50; _child.style.height = 25; }
        }).Update(_node);
        Resize(200, 150); Frame(1000); Frame(0); Frame(1000);
        Assert.That(_finished.Count, Is.EqualTo(2));
        Assert.That(_finished[1].initial, Is.EqualTo(new Size(200, 150)));
        Assert.That(_host.layout.size, Is.EqualTo(new Vector2(50, 25)));
    }

    [Test]
    public void CompletionMayDisposeTheModifier()
    {
        Create();
        Description(Tween(1000), Alignment.TopStart, (_, _) => _node.Dispose()).Update(_node);
        Resize(200, 150); Frame(1000); Frame(1000);
        Assert.That(_child.parent, Is.SameAs(_host));
        Assert.That(_host.style.width.keyword, Is.EqualTo(StyleKeyword.Null));
    }

    [Test]
    public void RemovingModifierKeepsAnotherClipOwner()
    {
        Create(); _host.Clip().Increment(); Resize(200, 150); Frame(500);
        _node.Dispose(); Frame(500);
        Assert.That(_host.resolvedStyle.overflow, Is.EqualTo(Overflow.Hidden));
        Assert.That(_finished, Is.Empty);
        _host.Clip().Decrement(); Frame(0);
        Assert.That(_host.resolvedStyle.overflow, Is.EqualTo(Overflow.Visible));
    }

    [Test]
    public void ParentConstraintsStillApplyToAnimatedLayout()
    {
        Create(); _host.style.maxWidth = 150; _host.style.maxHeight = 100;
        Resize(300, 200); Frame(500);
        Assert.That(_host.layout.width, Is.LessThanOrEqualTo(150));
        Assert.That(_host.layout.height, Is.LessThanOrEqualTo(100));
        Frame(500);
        Assert.That(_host.layout.size, Is.EqualTo(new Vector2(150, 100)));
        Assert.That(_finished.Count, Is.EqualTo(1));
    }

    [Test]
    public void DynamicChildrenCanBeRemovedAndReinserted()
    {
        Create(); _child.RemoveFromHierarchy(); Frame(0); Frame(1000);
        Assert.That(_host.layout.size, Is.EqualTo(Vector2.zero));
        _content.Add(_child); Frame(0); Frame(1000);
        Assert.That(_host.layout.size, Is.EqualTo(new Vector2(100, 50)));
        Assert.That(_finished.Count, Is.EqualTo(2));
    }

    [Test]
    public void UnrelatedNodesDoNotShareAnimationState()
    {
        Create();
        var host = _host; var child = _child; var node = _node;
        try
        {
            Create(); Resize(300, 150);
            child.style.width = 200; child.style.height = 100; Frame(0); Frame(500);
            Assert.That(host.layout.width, Is.EqualTo(150).Within(1));
            Assert.That(_host.layout.width, Is.EqualTo(200).Within(1));
            node.Dispose(); Frame(500);
            Assert.That(_host.layout.width, Is.EqualTo(300));
            Assert.That(_finished.Count, Is.EqualTo(1));
        }
        finally { node.Dispose(); }
    }
}
