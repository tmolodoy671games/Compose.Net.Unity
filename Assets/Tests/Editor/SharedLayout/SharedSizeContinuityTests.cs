using static Compose.Net.ComposeFunctions;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Samples.Tests;

public sealed partial class SharedLayoutDemoTests
{
    [TestCase(0, 1f)][TestCase(1, 1f)][TestCase(2, 1f)]
    [TestCase(0, 1.25f)][TestCase(1, 1.5f)][TestCase(2, 2f)]
    public void SampleCardDrawingBoundsMatchTheTransitionFromFirstToLastFrame(int index, float scale)
    {
        pixelsPerPoint = scale;
        MountNestedSample(index);
        var large = E($"shared-sample.detail.{index}").worldBound;
        MountNestedSample();
        var small = E($"shared-sample.thumb.{index}").worldBound;

        for (var direction = 0; direction < 2; direction++)
        {
            var source = direction == 0 ? "thumb" : "detail";
            var target = direction == 0 ? "detail" : "thumb";
            var start = direction == 0 ? small : large;
            var end = direction == 0 ? large : small;
            _sampleSelection.Value = direction == 0 ? index : -1;
            Flush();
            var outgoing = E($"shared-sample.{source}.{index}");
            var incoming = E($"shared-sample.{target}.{index}");
            RectNear(Layer(incoming)!.worldBound, start);

            var elapsed = 0;
            foreach (var time in new[] { 0, 1, 16, 100, 250, 400, 484, 499 })
            {
                Flush(time - elapsed);
                elapsed = time;
                var bounds = Layer(incoming)!.worldBound;
                RectNear(bounds, Interpolate(start, end, EaseInOutEasing.Transform(time / 500f)));
                AssertBoundsMatchDrawing(outgoing, bounds, $"{source}: {direction}, {time}ms");
                AssertBoundsMatchDrawing(incoming, bounds, $"{target}: {direction}, {time}ms");
            }

            // Returning from the overlay must preserve the last rendered geometry.
            var before = incoming.worldBound;
            Flush(1);
            RectNear(incoming.worldBound, end);
            RectNear(incoming.worldBound, before);
            Assert.That(Layer(incoming), Is.Null);
            Assert.That(rootVisualElement.Q($"shared-sample.{source}.{index}"), Is.Null);
            Flush(16);
            RectNear(incoming.worldBound, end);
        }
    }

    [TestCase(0, 1)][TestCase(1, 250)][TestCase(2, 499)]
    [TestCase(0, 250)][TestCase(1, 499)][TestCase(2, 1)]
    public void SampleCardReversalPreservesDrawingBounds(int index, int reverseAt)
    {
        MountNestedSample();
        var origin = E($"shared-sample.thumb.{index}").worldBound;
        _sampleSelection.Value = index;
        Flush();
        Flush(reverseAt);
        var detail = E($"shared-sample.detail.{index}");
        var before = detail.worldBound;
        _sampleSelection.Value = -1;
        Flush();
        var thumb = E($"shared-sample.thumb.{index}");
        RectNear(thumb.worldBound, before);
        RectNear(detail.worldBound, before);
        Flush(250);
        AssertBoundsMatchDrawing(thumb, Layer(thumb)!.worldBound, "Returning thumb");
        AssertBoundsMatchDrawing(detail, Layer(thumb)!.worldBound, "Outgoing detail");
        Flush(249);
        before = thumb.worldBound;
        Flush(1);
        RectNear(thumb.worldBound, before);
        RectNear(thumb.worldBound, origin);
        Assert.That(Layer(thumb), Is.Null);
        Assert.That(rootVisualElement.Q($"shared-sample.detail.{index}"), Is.Null);
    }

    private static void AssertBoundsMatchDrawing(VisualElement element, UnityEngine.Rect bounds, string message)
    {
        // Assert the painted card itself, not just the correctly sized overlay wrapper.
        Assert.That(element.worldBound.x, Is.EqualTo(bounds.x).Within(1.01f), message);
        Assert.That(element.worldBound.y, Is.EqualTo(bounds.y).Within(1.01f), message);
        Assert.That(element.worldBound.width, Is.EqualTo(bounds.width).Within(1.01f), message);
        Assert.That(element.worldBound.height, Is.EqualTo(bounds.height).Within(1.01f), message);
    }
}
