using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Compose.Net;
using NUnit.Framework;
using UnityEngine.UIElements;
using static Compose.Net.ComposeFunctions;

// ReSharper disable CheckNamespace

public partial class AnimateContentSizeKaspressoTests
{
    private readonly IMutableState<float> _width = MutableStateOf(80f);
    private readonly IMutableState<float> _height = MutableStateOf(30f);
    private readonly IMutableState<int> _count = MutableStateOf(1);
    private string _layout;
    private int _duration;
    private int _delay;
    private Alignment _alignment = Alignment.TopStart;
    private readonly List<(Size Initial, Size Target)> _finished = new();

    [Test]
    public async Task ListenerReportsInterruptedStartAndOnlyCompletedTarget()
    {
        using var driver = Create("Column");
        Assert.That(_finished, Is.Empty);
        _width.Value = 160;
        await driver.SizeAsync("host", 80, 30);
        await driver.AdvanceAsync(500);
        _width.Value = 200;
        await driver.SizeAsync("host", 120, 30);
        Assert.That(_finished, Is.Empty);
        await driver.AdvanceAsync(1001);
        Assert.That(_finished.Count, Is.EqualTo(1));
        Assert.That(_finished[0].Initial.Width, Is.EqualTo(120).Within(.6f));
        Assert.That(_finished[0].Target, Is.EqualTo(new Size(200, 30)));
    }

    [TestCase(0f)]
    [TestCase(.5f)]
    [TestCase(1f)]
    public async Task ContentUsesAlignmentWithinAnimatedBounds(float fraction)
    {
        using var driver = Create("Column", alignment: fraction == 0 ? Alignment.TopStart
            : fraction == .5f ? Alignment.Center : Alignment.BottomEnd);
        _width.Value = 160;
        _height.Value = 90;
        await driver.SizeAsync("host", 80, 30);
        await driver.AdvanceAsync(500);
        var host = driver.Root.Q("host");
        var child = driver.Root.Q("child-0");
        Assert.That(child.worldBound.x - host.worldBound.x, Is.EqualTo(-40 * fraction).Within(.6f));
        Assert.That(child.worldBound.y - host.worldBound.y, Is.EqualTo(-30 * fraction).Within(.6f));
        Assert.That(host.resolvedStyle.overflow, Is.EqualTo(Overflow.Hidden));
    }

    [Test]
    public async Task SnapNotifiesOnceAndDisposalDoesNotCompleteAnAnimation()
    {
        using (var driver = Create("Column", duration: 0))
        {
            _width.Value = 160;
            await driver.SizeAsync("host", 160, 30);
            Assert.That(_finished.Count, Is.EqualTo(1));
            Assert.That(_finished[0].Initial, Is.EqualTo(new Size(80, 30)));
            Assert.That(_finished[0].Target, Is.EqualTo(new Size(160, 30)));
        }
        using (var driver = Create("Column"))
        {
            _width.Value = 160;
            await driver.SizeAsync("host", 80, 30);
            await driver.AdvanceAsync(500);
        }
        Assert.That(_finished, Is.Empty);
    }

    [TestCase("Column", 160, 30)]
    [TestCase("Row", 80, 90)]
    [TestCase("Box", 160, 90)]
    [TestCase("Column", 40, 15)]
    public async Task StateResizeInterpolatesAndReachesTarget(string layout, float width, float height)
    {
        using var driver = Create(layout);
        await driver.Ui.TestCase().RunAsync(async test =>
        {
            await test.StepAsync("Initial content is measured without animation", () => driver.SizeAsync("host", 80, 30));
            await test.StepAsync("Resize through Compose state", async () =>
            {
                _width.Value = width;
                _height.Value = height;
                await driver.SizeAsync("child-0", width, height);
                await driver.SizeAsync("host", 80, 30);
            });
            await test.StepAsync("Host interpolates halfway", async () =>
            {
                await driver.AdvanceAsync(500);
                await driver.SizeAsync("host", (80 + width) / 2, (30 + height) / 2);
            });
            await test.StepAsync("Host reaches the new content size", async () =>
            {
                await driver.AdvanceAsync(501);
                await driver.SizeAsync("host", width, height);
            });
        });
    }

    [TestCase("Column", 80, 90)]
    [TestCase("Row", 240, 30)]
    [TestCase("Box", 80, 30)]
    public async Task AddingAndRemovingComposedChildrenUpdatesHost(string layout, float width, float height)
    {
        using var driver = Create(layout);
        await driver.Ui.TestCase().RunAsync(async test =>
        {
            await test.StepAsync("Add keyed children through state", async () =>
            {
                _count.Value = 3;
                await driver.Ui.OnNodeWithTag("child-2").AssertExistsAsync();
                await driver.AdvanceAsync(1001);
                await driver.SizeAsync("host", width, height);
            });
            await test.StepAsync("Remove children through state", async () =>
            {
                _count.Value = 1;
                await driver.Ui.OnNodeWithTag("child-2").AssertDoesNotExistAsync();
                await driver.AdvanceAsync(1001);
                await driver.SizeAsync("host", 80, 30);
            });
        });
    }

    [Test]
    public async Task RetargetingPreservesDisplayedSize()
    {
        using var driver = Create("Column");
        _width.Value = 160;
        await driver.SizeAsync("host", 80, 30);
        await driver.AdvanceAsync(500);
        await driver.SizeAsync("host", 120, 30);
        _width.Value = 200;
        await driver.SizeAsync("host", 120, 30);
        await driver.AdvanceAsync(500);
        await driver.SizeAsync("host", 160, 30);
        await driver.AdvanceAsync(501);
        await driver.SizeAsync("host", 200, 30);
    }

    [TestCase(0, 0)]
    [TestCase(1000, 200)]
    public async Task AnimationSpecControlsStateDrivenResize(int duration, int delay)
    {
        using var driver = Create("Column", duration, delay);
        _width.Value = 160;
        await driver.Ui.OnNodeWithTag("child-0").AssertExistsAsync();
        if (delay > 0)
        {
            await driver.AdvanceAsync(100);
            await driver.SizeAsync("host", 80, 30);
        }
        await driver.AdvanceAsync(duration + delay + 1);
        await driver.SizeAsync("host", 160, 30);
    }

    private UnityComposeTestDriver Create(string layout, int duration = 1000, int delay = 0, Alignment alignment = null)
    {
        _width.Value = 80; _height.Value = 30; _count.Value = 1;
        _layout = layout; _duration = duration; _delay = delay;
        _alignment = alignment ?? Alignment.TopStart;
        _finished.Clear();
        var driver = new UnityComposeTestDriver();
        try { driver.SetContent(Content); return driver; }
        catch { driver.Dispose(); throw; }
    }

    [Composable]
    private void Content()
    {
        var modifier = Modifier.TestTag("host").AnimateContentSize(new AnimationSpec(
            TimeSpan.FromMilliseconds(_duration), TimeSpan.FromMilliseconds(_delay), new Linear()),
            alignment: _alignment, finishedListener: (initial, target) => _finished.Add((initial, target)));
        if (_layout == "Row") Row(modifier: modifier, content: Children);
        else if (_layout == "Box") Box(modifier: modifier, content: Children);
        else Column(modifier: modifier, content: Children);
    }

    [Composable]
    private void Children()
    {
        for (var index = 0; index < _count.Value; index++)
        {
            var key = index;
            Key(key, () => Spacer(Modifier.Size(_width.Value.Dp(), _height.Value.Dp()).TestTag("child-" + key)));
        }
    }

    private sealed class Linear : IEasing { public float Transform(float fraction) => fraction; }
}
