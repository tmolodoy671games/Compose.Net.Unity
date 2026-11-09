using System.Collections.Generic;
using Compose.Net;
using static Compose.Net.ComposeFunctions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Tests;

public sealed partial class BoxTests
{
    [Test]
    public void GloballyPositionedDefersAttachmentAndCallbackUpdatesUntilLayout()
    {
        var widths = new List<float>();
        _box = Modifier.Size(120.Dp(), 40.Dp()).OnGloballyPositioned(c => widths.Add(c.Size.Width));
        _composer = IComposer.Create();
        Bootstrap(_composer, _adapter, false, new ComposeConfig(Logging: false), new Root(rootVisualElement), Content);
        Assert.That(widths, Is.Empty, "Attaching a modifier must not dispatch unmeasured coordinates");
        Flush();
        Assert.That(widths, Is.Not.Empty);
        Assert.That(widths[widths.Count - 1], Is.EqualTo(120).Within(.6f));

        var updated = new List<float>();
        _box = Modifier.Size(200.Dp(), 40.Dp()).OnGloballyPositioned(c => updated.Add(c.Size.Width));
        _revision.Value++;
        _adapter.Invalidator.Tick();
        Assert.That(updated, Is.Empty, "Updating the callback must not dispatch the previous layout during composition");
        Flush();
        Assert.That(updated, Is.Not.Empty);
        Assert.That(updated[0], Is.EqualTo(200).Within(.6f));
    }

    [Test]
    public void GloballyPositionedReportsMeasuredZeroSizeAndAncestorMovement()
    {
        var positions = new List<Offset>();
        _box = Modifier.Size(120.Dp(), 40.Dp());
        Add("child", 0, 0, Modifier.OnGloballyPositioned(c =>
        {
            Assert.That(c.Size.Width, Is.Zero);
            Assert.That(c.Size.Height, Is.Zero);
            positions.Add(c.PositionInRoot());
        }));
        Mount();
        for (var frame = 0; frame < 8; frame++) simulate.FrameUpdateMs(16);
        Assert.That(positions, Is.Not.Empty);
        var first = positions[positions.Count - 1];
        E("box").style.translate = new Translate(30, 20);
        for (var frame = 0; frame < 8; frame++) simulate.FrameUpdateMs(16);
        var last = positions[positions.Count - 1];
        Assert.That(last.X - first.X, Is.EqualTo(30).Within(.6f));
        Assert.That(last.Y - first.Y, Is.EqualTo(20).Within(.6f));
    }

    [Test]
    public void GloballyPositionedReportsOnlyFiniteBoundsAndTracksResize()
    {
        var widths = new List<float>();
        var positioned = Modifier.OnGloballyPositioned(coordinates =>
        {
            var bounds = coordinates.ToRelativeLayoutBounds();
            Assert.That(float.IsFinite(bounds.TopLeft.X) && float.IsFinite(bounds.TopLeft.Y) &&
                float.IsFinite(bounds.BottomRight.X) && float.IsFinite(bounds.BottomRight.Y), Is.True,
                "Unmeasured bounds must not reach state or animation targets");
            widths.Add(coordinates.Size.Width);
        });
        _box = Modifier.Size(120.Dp(), 40.Dp()).Then(positioned);
        Mount();
        for (var frame = 0; frame < 8; frame++) simulate.FrameUpdateMs(16);
        Assert.That(widths, Is.Not.Empty);
        Assert.That(widths[widths.Count - 1], Is.EqualTo(120).Within(.6f));

        _box = Modifier.Size(200.Dp(), 40.Dp()).Then(positioned);
        Update();
        for (var frame = 0; frame < 8; frame++) simulate.FrameUpdateMs(16);
        Assert.That(widths[widths.Count - 1], Is.EqualTo(200).Within(.6f));
    }

    [TestCase(1f), TestCase(1.25f), TestCase(1.5f), TestCase(2f)]
    public void CenteredAnimatedTextSettlesAtEveryPanelScale(float scale)
    {
        pixelsPerPoint = scale;
        panelSize = new Vector2(1920, 1080);
        rootVisualElement.style.width = 1920;
        rootVisualElement.style.height = 1080;
        _regressionText = "Short";
        _regressionFont = 64;
        _composer = IComposer.Create();
        Bootstrap(_composer, _adapter, false, new ComposeConfig(Logging: false), new Root(rootVisualElement), CenteredAnimatedText);
        var changes = 0;
        E("box").RegisterCallback<GeometryChangedEvent>(_ => changes++);
        E("sample-text").RegisterCallback<GeometryChangedEvent>(_ => changes++);
        E("sample-text").parent.RegisterCallback<GeometryChangedEvent>(_ => changes++);
        var warnings = new List<string>();
        void Log(string message, string stack, LogType type)
        { if (message.Contains("stabilize") || message.Contains("DPI setting")) warnings.Add(message); }
        Application.logMessageReceived += Log;
        try
        {
            for (var cycle = 0; cycle < 4; cycle++)
            {
                _regressionText = cycle % 2 == 0 ? "Short" : "Looooooooooooooooooooong\nLooooooooooooooooooooong\nLooooooooooooooooooooong";
                _revision.Value++;
                for (var frame = 0; frame < 60; frame++)
                { _adapter.Invalidator.Tick(); simulate.FrameUpdateMs(16); }
                AssertTextFits();
                changes = 0;
                for (var frame = 0; frame < 20; frame++) simulate.FrameUpdateMs(16);
                Assert.That(changes, Is.Zero, "Layout must remain idle after the animation has settled");
            }
            Assert.That(warnings, Is.Empty);
        }
        finally { Application.logMessageReceived -= Log; }
    }

    [Composable, BoxScope]
    private void CenteredAnimatedText()
    {
        Box(() => Box(() => Box(Children,
                Modifier.Clip(RoundedCornerShape(32.Dp())).Padding(16.Dp()).AnimateContentSize(Tween(300), alignment: Alignment.Center).TestTag("box"), Alignment.Center),
            Modifier.FillMaxSize(), Alignment.Center), Modifier.FillMaxSize(), Alignment.Center);
    }

    [Test, BoxScope]
    public void AnimatedTextCanBeDisposedAfterItsPanelIsDetached()
    {
        _regressionFont = 64;
        _regressionText = "Short";
        _box = Modifier.Clip(RoundedCornerShape(32.Dp())).Padding(16.Dp()).AnimateContentSize(Tween(300));
        Mount(); SettleText();
        _regressionText = "Longer text\nwith two lines";
        Update(); simulate.FrameUpdateMs(16);
        var warnings = new List<string>();
        void Log(string message, string stack, LogType type)
        { if (message.Contains("DPI setting")) warnings.Add(message); }
        Application.logMessageReceived += Log;
        try
        {
            E("box").RemoveFromHierarchy();
            _composer.Dispose();
            _composer = null!;
            simulate.FrameUpdateMs(500);
            Assert.That(warnings, Is.Empty, "Detached text must not access the panel's DPI");
        }
        finally { Application.logMessageReceived -= Log; }
    }

    [Test, BoxScope]
    public void DetachedAnimatedTextRemeasuresWhenReattached()
    {
        _regressionFont = 64;
        _regressionText = "Short";
        _box = Modifier.Clip(RoundedCornerShape(32.Dp())).Padding(16.Dp()).AnimateContentSize(Tween(300));
        Mount(); SettleText();
        var box = E("box");
        var parent = box.parent;
        var warnings = new List<string>();
        void Log(string message, string stack, LogType type)
        { if (message.Contains("DPI setting")) warnings.Add(message); }
        Application.logMessageReceived += Log;
        try
        {
            box.RemoveFromHierarchy();
            _regressionText = "Longer text\nwith two lines";
            _regressionFont = 96;
            Update();
            parent.Add(box);
            SettleText(); AssertTextFits();
            Assert.That(E("sample-text").layout.height, Is.GreaterThan(190));
            Assert.That(warnings, Is.Empty);
        }
        finally { Application.logMessageReceived -= Log; }
    }
}
