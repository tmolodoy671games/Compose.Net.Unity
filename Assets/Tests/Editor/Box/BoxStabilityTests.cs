using System.Collections.Generic;
using Compose.Net;
using static Compose.Net.ComposeFunctions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Tests;

public sealed partial class BoxTests
{
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
