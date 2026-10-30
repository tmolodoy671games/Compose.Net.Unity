using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Compose.Net;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using static Compose.Net.ComposeFunctions;

public partial class ScrollKaspressoTests
{
    private IScrollState _state;
    private bool _horizontal, _reverse, _enabled;
    private readonly FlingProbe _fling = new();
    private readonly IMutableState<bool> _show = MutableStateOf(true);

    [TestCase(false, false, true)]
    [TestCase(true, false, true)]
    [TestCase(false, true, true)]
    [TestCase(true, true, true)]
    [TestCase(false, false, false)]
    [TestCase(true, false, false)]
    public async Task ScrollMovesRealContent(bool horizontal, bool reverse, bool enabled)
    {
        _horizontal = horizontal; _reverse = reverse; _enabled = enabled; _show.Value = true; _fling.Calls = 0;
        using var driver = new UnityComposeTestDriver();
        driver.SetContent(Content);
        await driver.WaitForIdleAsync(CancellationToken.None);
        var viewport = driver.Root.Q<VisualElement>("scroll");
        var content = viewport.Children().Single();
        Assert.That(_state.MaxValue, Is.GreaterThan(200), "content must exceed viewport");
        Assert.That(_state.ViewportSize, Is.EqualTo(100).Within(1));
        Assert.That(Position(content), Is.EqualTo(reverse ? 20 - _state.MaxValue : -20).Within(1));
        _state.ScrollTo(40);
        // No composition tick: the state must move the existing content immediately.
        Assert.That(Position(content), Is.EqualTo(reverse ? 40 - _state.MaxValue : -40).Within(1));
        var before = _state.Value;
        using (var wheel = WheelEvent.GetPooled(new Event { type = EventType.ScrollWheel, delta = new Vector2(0, reverse ? -1 : 1) }))
        {
            wheel.target = viewport;
            viewport.SendEvent(wheel);
        }
        await driver.AdvanceAsync(1000);
        Assert.That(_state.Value, enabled ? Is.GreaterThan(before) : Is.EqualTo(before));
        Assert.That(Position(content), Is.EqualTo(reverse ? _state.Value - _state.MaxValue : -_state.Value).Within(1));
        _state.ScrollTo(100);
        var start = viewport.worldBound.center;
        var end = start + (_horizontal ? Vector2.right : Vector2.up) * (reverse ? 25 : -25);
        using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = start }))
        {
            down.target = viewport; viewport.SendEvent(down);
        }
        using (var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseDrag, button = 0, mousePosition = end }))
        {
            move.target = viewport; viewport.SendEvent(move);
        }
        using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = end }))
        {
            up.target = viewport; viewport.SendEvent(up);
        }
        Assert.That(_state.Value, Is.EqualTo(enabled ? 130 : 100).Within(1), "drag delta plus custom fling");
        Assert.That(_fling.Calls, Is.EqualTo(enabled ? 1 : 0), "custom fling receives drag release");
        _show.Value = false;
        await driver.WaitForIdleAsync(CancellationToken.None);
        _state.ScrollTo(0); // detached subscriptions must be removed
    }

    private sealed class FlingProbe : IFlingBehavior
    {
        public int Calls;
        public Task<float> PerformFling(IScrollScope scope, float velocity, CancellationToken cancellationToken = default)
        {
            Calls++;
            scope.ScrollBy(5);
            return Task.FromResult(0f);
        }
    }

    private float Position(VisualElement content) => _horizontal
        ? content.resolvedStyle.translate.x : content.resolvedStyle.translate.y;

    [Composable]
    private void Content()
    {
        _state = RememberScrollState(initial: 20);
        if (!_show.Value) return;
        var modifier = Modifier.Size(100.Dp()).TestTag("scroll");
        modifier = _horizontal ? modifier.HorizontalScroll(_state, enabled: _enabled, flingBehavior: _fling, reverseScrolling: _reverse)
            : modifier.VerticalScroll(_state, enabled: _enabled, flingBehavior: _fling, reverseScrolling: _reverse);
        if (_horizontal) Row(modifier: modifier, content: Items);
        else Column(modifier: modifier, content: Items);
    }

    [Composable]
    private void Items()
    {
        for (var i = 0; i < 5; i++) Box(modifier: Modifier.Size(80.Dp()), content: () => { });
    }
}
