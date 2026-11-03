using Compose.Net;
using Compose.Net.Unity.Samples.Behaviors.Content;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using static Compose.Net.ComposeFunctions;

namespace Compose.Net.Unity.Samples.Tests;

public sealed partial class SharedLayoutDemoTests
{
    private IMutableState<bool> _elementExpanded = null!;

    private void MountElementSample(bool expanded, Vector2 size)
    {
        _composer.Dispose(); Flush(); panelSize = size;
        _elementExpanded = MutableStateOf(expanded); _composer = IComposer.Create();
        Bootstrap(_composer, _adapter, false, new ComposeConfig(Logging: false),
            new RootNode(rootVisualElement), ElementSampleContent);
        Flush();
    }

    [Composable] private void ElementSampleContent() => SharedElementSample.Screen(_elementExpanded);

    [TestCase(800, 600, 1f)][TestCase(1280, 720, 1f)][TestCase(800, 600, 1.25f)]
    [TestCase(600, 900, 2f)]
    public void ElementSamplePaintedBoundsStayContinuousThroughCompletion(int width, int height, float scale)
    {
        pixelsPerPoint = scale;
        var size = new Vector2(width, height);
        MountElementSample(true, size); var large = E("shared-element.expanded").worldBound;
        MountElementSample(false, size); var small = E("shared-element.collapsed").worldBound;

        for (var direction = 0; direction < 2; direction++)
        {
            var expand = direction == 0;
            var source = expand ? "collapsed" : "expanded";
            var target = expand ? "expanded" : "collapsed";
            var start = expand ? small : large;
            var end = expand ? large : small;
            _elementExpanded.Value = expand; Flush();
            var incoming = E("shared-element." + target);
            var outgoing = E("shared-element." + source);
            var elapsed = 0;
            foreach (var time in new[] { 0, 1, 16, 100, 250, 400, 484, 499 })
            {
                Flush(time - elapsed); elapsed = time;
                var bounds = Interpolate(start, end, EaseInOutEasing.Transform(time / 500f));
                RectNear(Layer(incoming)!.worldBound, bounds);
                AssertBoundsMatchDrawing(incoming, bounds, $"Incoming: {direction}, {time}ms");
                AssertBoundsMatchDrawing(outgoing, bounds, $"Outgoing: {direction}, {time}ms");
            }
            var before = incoming.worldBound;
            Flush(1);
            RectNear(incoming.worldBound, before);
            RectNear(incoming.worldBound, end);
            Assert.That(Layer(incoming), Is.Null);
            Assert.That(rootVisualElement.Q("shared-element." + source), Is.Null);
            Flush(16); RectNear(incoming.worldBound, end);
        }
    }

    [TestCase(false)][TestCase(true)]
    public void ElementSampleOutgoingContentFadesBeforeItIsRemoved(bool initiallyExpanded)
    {
        MountElementSample(initiallyExpanded, new Vector2(800, 600));
        _elementExpanded.Value = !initiallyExpanded; Flush();
        var incoming = E("shared-element." + (initiallyExpanded ? "collapsed" : "expanded"));
        var outgoing = E("shared-element." + (initiallyExpanded ? "expanded" : "collapsed"));
        Near(Alpha(incoming), 0, .01f); Near(Alpha(outgoing), 1, .01f);
        Flush(250);
        Near(Alpha(incoming), .5f, .01f); Near(Alpha(outgoing), .5f, .01f);
        Flush(249);
        Assert.That(Alpha(outgoing), Is.LessThan(.001f), "The outgoing label and square corners must disappear before teardown");
        Near(Alpha(incoming), 1, .001f);
        var before = incoming.worldBound; Flush(1);
        Near(Alpha(incoming), 1, .001f); RectNear(incoming.worldBound, before);
    }

    [TestCase(false, 250)][TestCase(true, 250)][TestCase(false, 499)][TestCase(true, 499)]
    public void ElementSampleReversalPreservesGeometryAndOpacity(bool initiallyExpanded, int reverseAt)
    {
        MountElementSample(initiallyExpanded, new Vector2(800, 600));
        var source = E("shared-element." + (initiallyExpanded ? "expanded" : "collapsed"));
        var origin = source.worldBound;
        _elementExpanded.Value = !initiallyExpanded; Flush(); Flush(reverseAt);
        var target = E("shared-element." + (initiallyExpanded ? "collapsed" : "expanded"));
        var bounds = target.worldBound; var a = Alpha(source); var b = Alpha(target);
        _elementExpanded.Value = initiallyExpanded; Flush();
        RectNear(source.worldBound, bounds); RectNear(target.worldBound, bounds);
        Near(Alpha(source), a, .01f); Near(Alpha(target), b, .01f);
        Flush(499); var before = source.worldBound; Flush(1);
        RectNear(source.worldBound, before); RectNear(source.worldBound, origin);
        Assert.That(Layer(source), Is.Null);
    }
}
