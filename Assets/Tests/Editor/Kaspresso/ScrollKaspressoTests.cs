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
    private bool _explicitEffect;
    private IOverscrollEffect _effect;
    private bool _horizontal, _reverse, _enabled;
    private readonly FlingProbe _fling = new();
    private readonly IMutableState<bool> _show = MutableStateOf(true);
    private ILazyListState _lazyState;
    private int _clicks, _alignment;

    [TestCase(false)]
    [TestCase(true)]
    public async Task DragStartingOnClickableItemScrollsWithoutClicking(bool horizontal)
    {
        _horizontal = horizontal; _reverse = false; _clicks = 0;
        using var driver = new UnityComposeTestDriver();
        driver.SetContent(LazyContent);
        await driver.WaitForIdleAsync(CancellationToken.None);
        var viewport = driver.Root.Q<VisualElement>("lazy");
        var item = driver.Root.Q<VisualElement>("lazy-item-0");
        var start = item.worldBound.center;
        SendPointer(item, EventType.MouseDown, start);
        SendPointer(item, EventType.MouseDrag, start + (horizontal ? Vector2.left : Vector2.down) * 25);
        Assert.That(_lazyState.Value, Is.GreaterThan(20), "a clickable child must not swallow the scroll gesture");
        SendPointer(viewport, EventType.MouseUp, start);
        Assert.That(_clicks, Is.Zero, "a drag must cancel the pending click");
        _lazyState.ScrollTo(0);
        start = item.worldBound.center;
        SendPointer(item, EventType.MouseDown, start);
        SendPointer(item, EventType.MouseDrag, start + Vector2.one);
        Assert.That(_lazyState.Value, Is.Zero, "a small pointer movement must remain a tap");
        SendPointer(item, EventType.MouseUp, start);
        Assert.That(_clicks, Is.EqualTo(1), "a tap must still click after a cancelled press");
    }

    [TestCase(false, 0)]
    [TestCase(false, 1)]
    [TestCase(false, 2)]
    [TestCase(true, 0)]
    [TestCase(true, 1)]
    [TestCase(true, 2)]
    public async Task LazyColumnPassesAvailableWidthToItems(bool reverse, int alignment)
    {
        _horizontal = false; _reverse = reverse; _alignment = alignment;
        using var driver = new UnityComposeTestDriver();
        driver.SetContent(LazyContent);
        await driver.WaitForIdleAsync(CancellationToken.None);
        var viewport = driver.Root.Q<VisualElement>("lazy");
        var filled = driver.Root.Q<VisualElement>("lazy-item-0");
        var narrow = driver.Root.Q<VisualElement>("lazy-item-1");
        Assert.That(viewport.layout.width, Is.EqualTo(300).Within(1), "LazyColumn fills its parent's width");
        Assert.That(filled.layout.width, Is.EqualTo(280).Within(1), "item fill excludes content padding");
        Assert.That(narrow.layout.width, Is.EqualTo(40).Within(1), "fixed width items retain their width");
        var expectedLeft = alignment == 2 ? 250 : alignment == 1 ? 130 : 10;
        Assert.That(narrow.worldBound.xMin - viewport.worldBound.xMin, Is.EqualTo(expectedLeft).Within(1),
            "wrappers preserve horizontal alignment");
        _lazyState.ScrollToItem(1);
        Assert.That(_lazyState.Value, Is.EqualTo(80).Within(1), "item scrolling accounts for content padding");
    }

    private static void SendPointer(VisualElement target, EventType type, Vector2 position)
    {
        var input = new Event { type = type, button = 0, mousePosition = position };
        if (type == EventType.MouseDown)
        {
            using var evt = PointerDownEvent.GetPooled(input);
            evt.target = target; target.SendEvent(evt);
        }
        else if (type == EventType.MouseUp)
        {
            using var evt = PointerUpEvent.GetPooled(input);
            evt.target = target; target.SendEvent(evt);
        }
        else
        {
            using var evt = PointerMoveEvent.GetPooled(input);
            evt.target = target; target.SendEvent(evt);
        }
    }

    [Composable]
    private void LazyContent()
    {
        _lazyState = RememberLazyListState();
        Column(modifier: Modifier.Width(300.Dp()), content: () =>
        {
            if (_horizontal)
                LazyRow(state: _lazyState, modifier: Modifier.FillMaxWidth().Height(100.Dp()).TestTag("lazy"),
                    flingBehavior: _fling, content: scope => scope.Items(5, index =>
                        Box(modifier: Modifier.Width(80.Dp()).FillMaxHeight().TestTag("lazy-item-" + index)
                            .Clickable(() => _clicks++), content: () => { })));
            else
                LazyColumn(state: _lazyState, modifier: Modifier.FillMaxWidth().Height(100.Dp()).TestTag("lazy"),
                    reverseLayout: _reverse, flingBehavior: _fling,
                    contentPadding: new PaddingValues(Top: 7.Dp(), Bottom: 13.Dp(), Left: 10.Dp(), Right: 10.Dp()),
                    horizontalAlignment: _alignment == 2 ? Alignment.Right : _alignment == 1 ? Alignment.CenterHorizontally : Alignment.Left,
                    content: scope => scope.Items(5, index =>
                        Box(modifier: (index == 0 ? Modifier.FillMaxWidth() : Modifier.Width(40.Dp()))
                            .Height(80.Dp()).TestTag("lazy-item-" + index).Clickable(() => _clicks++), content: () => { })));
        });
    }

    [TestCase(false, false, true)]
    [TestCase(true, false, true)]
    [TestCase(false, true, true)]
    [TestCase(true, true, true)]
    [TestCase(false, false, false)]
    [TestCase(true, false, false)]
    public async Task ScrollMovesRealContent(bool horizontal, bool reverse, bool enabled)
    {
        _horizontal = horizontal; _reverse = reverse; _enabled = enabled; _show.Value = true; _fling.Calls = 0;
        _fling.SuppressScroll = false;
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

    [TestCase(false, false, 0)]
    [TestCase(true, false, 0)]
    [TestCase(false, true, 0)]
    [TestCase(true, true, 0)]
    [TestCase(false, false, 1)]
    [TestCase(true, false, 1)]
    [TestCase(false, false, 2)]
    [TestCase(true, false, 2)]
    public async Task OverscrollAtBoundary(bool horizontal, bool reverse, int mode)
    {
        _horizontal = horizontal; _reverse = reverse; _enabled = true; _show.Value = true;
        _explicitEffect = mode != 0;
        _effect = mode == 2 ? null : new ElasticOverscrollFactory().CreateOverscrollEffect();
        _fling.SuppressScroll = true;
        using var driver = new UnityComposeTestDriver();
        driver.SetContent(Content);
        await driver.WaitForIdleAsync(CancellationToken.None);
        _state.ScrollTo(0);
        var viewport = driver.Root.Q<VisualElement>("scroll");
        var content = viewport.Children().Single();
        var resting = Position(content);
        var start = viewport.worldBound.center;
        var end = start + (horizontal ? Vector2.right : Vector2.up) * (reverse ? -60 : 60);
        using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = start }))
        { down.target = viewport; viewport.SendEvent(down); }
        using (var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseDrag, button = 0, mousePosition = end }))
        { move.target = viewport; viewport.SendEvent(move); }
        Assert.That(_state.Value, Is.EqualTo(0), "overscroll must not change bounded state");
        if (mode == 2) Assert.That(Position(content), Is.EqualTo(resting).Within(.01f), "null disables overscroll");
        else
        {
            var displacement = (Position(content) - resting) * (reverse ? -1 : 1);
            Assert.That(displacement, Is.GreaterThan(0).And.LessThan(60), "elastic displacement follows drag with resistance");
        }
        using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = end }))
        { up.target = viewport; viewport.SendEvent(up); }
        await driver.AdvanceAsync(1000);
        Assert.That(Position(content), Is.EqualTo(resting).Within(.01f), "release returns content to its boundary");
        Assert.That(_state.Value, Is.EqualTo(0));
    }
    private sealed class FlingProbe : IFlingBehavior
    {
        public int Calls;
        public bool SuppressScroll;
        public Task<float> PerformFling(IScrollScope scope, float velocity, CancellationToken cancellationToken = default)
        {
            Calls++;
            if (!SuppressScroll) scope.ScrollBy(5);
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
        if (_explicitEffect)
        {
            modifier = Modifier.Size(100.Dp()).TestTag("scroll");
            modifier = _horizontal
                ? modifier.HorizontalScroll(_state, _effect, enabled: _enabled, flingBehavior: _fling, reverseScrolling: _reverse)
                : modifier.VerticalScroll(_state, _effect, enabled: _enabled, flingBehavior: _fling, reverseScrolling: _reverse);
        }
        if (_horizontal) Row(modifier: modifier, content: Items);
        else Column(modifier: modifier, content: Items);
    }

    [Composable]
    private void Items()
    {
        for (var i = 0; i < 5; i++) Box(modifier: Modifier.Size(80.Dp()), content: () => { });
    }
}
