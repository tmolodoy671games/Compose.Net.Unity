using System;
using System.Collections.Generic;
using System.Reflection;
using Compose.Net;
using Compose.Net.Unity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;
using static Compose.Net.ComposeFunctions;

namespace Compose.Net.Unity.Tests;

[AiGenerated]
public sealed partial class AnimateContentSizeTests : UITestFixture
{
    private VisualElement _host, _child, _content;
    private IModifier.Node _node;
    private readonly List<(Size initial, Size target)> _finished = new();

    [TearDown]
    public void DisposeModifier() { _node?.Dispose(); _node = null; _finished.Clear(); }

    private IModifierNodeElement Description(AnimationSpec spec, Alignment alignment, Action<Size, Size> listener)
    {
        var type = typeof(UnityReusableComposeNode).Assembly.GetType("Compose.Net.Unity.AnimateContentSizeModifierImpl");
        return (IModifierNodeElement)Activator.CreateInstance(type, spec, alignment, listener);
    }

    private void Create(AnimationSpec spec = default, Alignment alignment = null, bool box = false)
    {
        panelSize = new Vector2(800, 600);
        _host = box ? new Compose.Net.Unity.Box() : new VisualElement();
        _host.style.alignSelf = Align.FlexStart;
        _host.style.flexShrink = 0;
        _child = new VisualElement();
        _child.style.width = 100;
        _child.style.height = 50;
        _child.style.flexShrink = 0;
        var unityNode = new UnityReusableComposeNode(_host);
        unityNode.Insert(0, new UnityReusableComposeNode(_child));
        var modifier = Description(spec.IsUnspecified ? Tween(1000, easing: LinearEasing) : spec,
            alignment ?? Alignment.TopStart, (initial, target) => _finished.Add((initial, target)));
        _node = modifier.Create();
        typeof(IModifier.Node).GetMethod("Attach", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(_node, new[] { modifier.GetTarget(unityNode) });
        _content = _child.parent;
        rootVisualElement.Add(_host);
        Frame(0);
        Frame(0);
        Assert.That(_host.layout.width, Is.EqualTo(100).Within(0.5));
        Assert.That(_host.layout.height, Is.EqualTo(50).Within(0.5));
        Assert.That(_finished, Is.Empty, "The first layout must not animate or notify");
    }

    private void Frame(int ms) => simulate.FrameUpdateMs(ms);
    private void Resize(float width, float height) { _child.style.width = width; _child.style.height = height; Frame(0); }

    [TestCase(false)]
    [TestCase(true)]
    public void AnimatesSizeWithNativeEasingAndClips(bool box)
    {
        Create(box: box);
        Resize(200, 150);
        Frame(500);
        Assert.That(_host.layout.width, Is.EqualTo(150).Within(1));
        Assert.That(_host.layout.height, Is.EqualTo(100).Within(1));
        Assert.That(_host.resolvedStyle.overflow, Is.EqualTo(Overflow.Hidden));
        Assert.That(_finished, Is.Empty);
        Frame(500);
        Assert.That(_host.layout.width, Is.EqualTo(200).Within(0.5));
        Assert.That(_finished.Count, Is.EqualTo(1));
        Assert.That(_finished[0].initial, Is.EqualTo(new Size(100, 50)));
        Assert.That(_finished[0].target, Is.EqualTo(new Size(200, 150)));
        Resize(100, 50);
        Frame(1000);
        Assert.That(_host.layout.width, Is.EqualTo(100).Within(0.5));
        Assert.That(_finished.Count, Is.EqualTo(2));
    }

    [Test]
    public void InterruptionStartsAtCurrentSizeAndDoesNotNotifyCancelledTarget()
    {
        Create();
        Resize(300, 150);
        Frame(500);
        var current = new Size(_host.layout.width, _host.layout.height);
        Resize(50, 25);
        Assert.That(_finished, Is.Empty);
        Frame(1000);
        Assert.That(_finished.Count, Is.EqualTo(1));
        Assert.That(_finished[0].initial, Is.EqualTo(current));
        Assert.That(_finished[0].target, Is.EqualTo(new Size(50, 25)));
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void AlignsContentInsideAnimatedBounds(int mode)
    {
        Create(alignment: mode == 0 ? Alignment.TopStart : mode == 1 ? Alignment.Center : Alignment.BottomEnd);
        Resize(200, 150);
        Frame(500);
        Assert.That(_content.layout.x, Is.EqualTo(-25 * mode).Within(1));
        Assert.That(_content.layout.y, Is.EqualTo(-25 * mode).Within(1));
    }

    [Test]
    public void DelayIsNotEasedAndZeroDurationSnaps()
    {
        Create(Tween(1000, 500, LinearEasing));
        Resize(200, 150);
        Frame(400);
        Assert.That(_host.layout.width, Is.EqualTo(100).Within(1));
        Frame(600);
        Assert.That(_host.layout.width, Is.EqualTo(150).Within(1));
        Frame(500);
        Assert.That(_finished.Count, Is.EqualTo(1));
        Description(Tween(0), Alignment.TopStart, (a, b) => _finished.Add((a, b))).Update(_node);
        Resize(80, 40);
        Assert.That(_host.layout.width, Is.EqualTo(80).Within(0.5));
        Assert.That(_finished.Count, Is.EqualTo(2));
    }

    [Test]
    public void UpdatingParametersPreservesRunningAnimationAndUsesLatestListener()
    {
        Create();
        Resize(200, 150);
        Frame(500);
        var updated = 0;
        Description(Tween(3000), Alignment.Center, (_, _) => updated++).Update(_node);
        Frame(500);
        Assert.That(updated, Is.EqualTo(1));
        Assert.That(_finished, Is.Empty);
        Assert.That(_host.layout.width, Is.EqualTo(200).Within(0.5));
    }

    [Test]
    public void DisposalCancelsAndRestoresStylesAndChildren()
    {
        Create();
        Resize(200, 150);
        Frame(500);
        _node.Dispose();
        Frame(1000);
        Assert.That(_finished, Is.Empty);
        Assert.That(_host.style.width.keyword, Is.EqualTo(StyleKeyword.Null));
        Assert.That(_host.style.height.keyword, Is.EqualTo(StyleKeyword.Null));
        Assert.That(_host.resolvedStyle.overflow, Is.EqualTo(Overflow.Visible));
        Assert.That(_child.parent, Is.SameAs(_host));
        Assert.That(_content.style.position.keyword, Is.EqualTo(StyleKeyword.Null));
        Assert.That(_content.style.left.keyword, Is.EqualTo(StyleKeyword.Null));
        Assert.That(_content.style.top.keyword, Is.EqualTo(StyleKeyword.Null));
    }

    [Test]
    public void PaddingBordersAndMarginsAreCountedOnce()
    {
        Create();
        _host.style.paddingLeft = 10;
        _host.style.paddingRight = 20;
        _host.style.borderLeftWidth = 3;
        _host.style.borderRightWidth = 4;
        _content.style.borderLeftWidth = 5;
        _content.style.borderRightWidth = 6;
        _content.style.marginLeft = 7;
        _content.style.marginRight = 8;
        Resize(200, 150);
        Frame(1000);
        Assert.That(_host.layout.width, Is.EqualTo(263).Within(1));
        Assert.That(_content.layout.x, Is.EqualTo(20).Within(1));
    }
}
