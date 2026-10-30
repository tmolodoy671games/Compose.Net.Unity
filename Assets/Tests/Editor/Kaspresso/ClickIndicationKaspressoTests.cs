using System;
using System.Linq;
using System.Collections;
using System.Reflection;
using SharpExtensions;
using System.Threading.Tasks;
using Compose.Net;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using static Compose.Net.ComposeFunctions;

public partial class ClickIndicationKaspressoTests
{
    private IMutableInteractionSource _source;
    private readonly RippleProbe _probe = new();
    private readonly IMutableState<int> _clicks = MutableStateOf(0);
    private bool _defaultSource;
    private readonly IMutableState<bool> _enabled = MutableStateOf(true);
    private bool _unwrapped;
    private bool _animated;

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    public async Task DefaultIndicationUpdatesActualDrawingCallback(bool preview, bool animated)
    {
        _unwrapped = true; _animated = animated; _clicks.Value = 0; _enabled.Value = true;
        using var driver = new UnityComposeTestDriver(useProductionClock: preview);
        driver.SetContent(Content, preview);
        await driver.Ui.TestCase().RunAsync(async ctx =>
        {
            await ctx.StepAsync("Plain Clickable updates its installed draw callback on hover", async () =>
            {
                await Send(driver, "enter");
                await driver.AdvanceAsync(350);
                await State(driver, () => Drawing(driver).HoverAlpha > .05f, "installed draw callback emits visible hover");
            });
            for (var cycle = 0; cycle < 3; cycle++)
            {
                await ctx.StepAsync("Draw press and release, cycle " + cycle, async () =>
                {
                    await Send(driver, "down");
                    await driver.AdvanceAsync(150);
                    await State(driver, () => Drawing(driver).PressAlpha > .05f && Drawing(driver).PressRadius > 1,
                        "installed draw callback emits a visible press circle");
                    await Send(driver, "up");
                    await driver.AdvanceAsync(350);
                    await State(driver, () => Drawing(driver).PressAlpha < .001f, "installed draw callback clears press");
                });
            }
            await Send(driver, "leave");
            await driver.AdvanceAsync(350);
            await State(driver, () => Drawing(driver).HoverAlpha < .001f, "installed draw callback clears hover");
        });
    }

    private static RippleDrawRecorder Drawing(UnityComposeTestDriver driver)
    {
        var button = driver.Root.Q<VisualElement>("button");
        var layer = button.Children().Single(e => e.GetType().Name == "DrawOn");
        var node = layer.generateVisualContent.GetInvocationList().Select(d => d.Target)
            .Single(target => target is Compose.Net.Unity.UnityReusableComposeNode);
        var callbacks = (IEnumerable)node.GetType().GetField("_drawOnTopContent", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(node);
        var callback = callbacks.Cast<Delegate>().Single();
        var draw = callback.Target.GetType().GetProperty("OnDraw")?.GetValue(callback.Target) as Delegate
            ?? callback.Target.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Select(field => field.GetValue(callback.Target)).OfType<Action<IDrawScope>>().Single();
        var recorder = new RippleDrawRecorder();
        recorder.DrawSize = new Size(layer.layout.width, layer.layout.height);
        ((Action<IDrawScope>)draw)(recorder);
        return recorder;
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task MovingMouseWithoutButtonDoesNotStartPress(bool defaultSource)
    {
        using var driver = Create(defaultSource);
        await driver.Ui.TestCase().RunAsync(async ctx => await ctx.StepAsync("Move without pressing", async () =>
        {
            await Send(driver, "move");
            await driver.AdvanceAsync(150);
            await State(driver, () => !_probe.Pressed && Float("PressAlpha") == 0, "no press on mouse move");
        }));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task PressHasVisibleRippleWhileHeldAndFadesAfterRelease(bool defaultSource)
    {
        using var driver = Create(defaultSource);
        await driver.Ui.TestCase().RunAsync(async ctx =>
        {
            await ctx.StepAsync("Hold primary button", async () =>
            {
                await Send(driver, "down");
                await driver.AdvanceAsync(150);
                await State(driver, () => _probe.Pressed && Float("PressAlpha") > .05f && Float("PressScale") > .05f,
                    "held press produces a visible ripple");
            });
            await ctx.StepAsync("Release completes click and clears press", async () =>
            {
                await Send(driver, "up");
                await driver.AdvanceAsync(350);
                await State(driver, () => !_probe.Pressed && _clicks.Value == 1 && Float("PressAlpha") < .001f,
                    "press fades after release");
            });
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task HoverClearsAfterClickAndPointerLeaves(bool defaultSource)
    {
        using var driver = Create(defaultSource);
        await driver.Ui.TestCase().RunAsync(async ctx => await ctx.StepAsync("Enter, click, leave", async () =>
        {
            await Send(driver, "enter");
            await driver.AdvanceAsync(350);
            await State(driver, () => _probe.Hovered && Float("HoverAlpha") > 0, "hover before click");
            await Send(driver, "down");
            await driver.AdvanceAsync(150);
            await Send(driver, "up");
            await Send(driver, "leave");
            await driver.AdvanceAsync(350);
            await State(driver, () => !_probe.Hovered && !_probe.Pressed && Float("HoverAlpha") < .001f && Float("PressAlpha") < .001f,
                "no stuck hover or press after leave");
        }));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task IndicationUpdatesKeepDrawingLayerAttached(bool defaultSource)
    {
        using var driver = Create(defaultSource);
        var button = driver.Root.Q<VisualElement>("button");
        var layer = button.Children().Single(e => e.GetType().Name == "DrawOn");
        var detached = 0;
        layer.RegisterCallback<DetachFromPanelEvent>(_ => detached++);
        await driver.Ui.TestCase().RunAsync(async ctx => await ctx.StepAsync("Animate hover without remounting overlay", async () =>
        {
            await Send(driver, "enter");
            await driver.AdvanceAsync(150);
            await State(driver, () => detached == 0, "drawing layer is kept attached across indication frames");
        }));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task PointerCancellationClearsRippleWithoutClick(bool defaultSource)
    {
        using var driver = Create(defaultSource);
        await driver.Ui.TestCase().RunAsync(async ctx => await ctx.StepAsync("Cancel an active press", async () =>
        {
            await Send(driver, "down");
            await driver.AdvanceAsync(150);
            await Send(driver, "cancel");
            await driver.AdvanceAsync(350);
            await State(driver, () => !_probe.Pressed && _clicks.Value == 0 && Float("PressAlpha") < .001f,
                "cancel clears press without firing click");
        }));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task DisablingDuringPressClearsInteractions(bool defaultSource)
    {
        using var driver = Create(defaultSource);
        await driver.Ui.TestCase().RunAsync(async ctx => await ctx.StepAsync("Disable a hovered and pressed element", async () =>
        {
            await Send(driver, "enter");
            await Send(driver, "down");
            await driver.AdvanceAsync(150);
            _enabled.Value = false;
            await driver.WaitForIdleAsync(default);
            for (var i = 0; i < 4; i++) await Task.Yield();
            await driver.AdvanceAsync(350);
            await State(driver, () => !_probe.Pressed && !_probe.Hovered && _clicks.Value == 0 && Float("PressAlpha") < .001f,
                "disabling clears press and hover");
        }));
    }

    private UnityComposeTestDriver Create(bool defaultSource)
    {
        _unwrapped = false; _animated = false;
        _source = MutableInteractionSource(); _clicks.Value = 0; _enabled.Value = true; _defaultSource = defaultSource;
        var driver = new UnityComposeTestDriver();
        try { driver.SetContent(Content); return driver; }
        catch { driver.Dispose(); throw; }
    }

    private Task State(UnityComposeTestDriver driver, Func<bool> predicate, string expectation) =>
        driver.Ui.OnNodeWithTag("button").AssertAsync(_ => predicate(), expectation);
    private float Float(string name) => (float)_probe.Latest.GetType().GetProperty(name).GetValue(_probe.Latest);

    private async Task Send(UnityComposeTestDriver driver, string kind)
    {
        await driver.Ui.OnNodeWithTag("button").ActAsync(node =>
        {
            var element = ((UnityComposeTestDriver.Node)node).Element;
            var position = element.worldBound.center;
            if (kind == "down")
            {
                using var evt = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = position });
                evt.target = element; element.SendEvent(evt);
            }
            else if (kind == "up")
            {
                using var evt = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = position });
                evt.target = element; element.SendEvent(evt);
            }
            else
            {
                using var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = position });
                if (kind == "move") { move.target = element; element.SendEvent(move); }
                else if (kind == "cancel")
                {
                    using var evt = PointerCancelEvent.GetPooled(move); evt.target = element; element.SendEvent(evt);
                }
                else if (kind == "enter")
                {
                    using var evt = PointerEnterEvent.GetPooled(move); evt.target = element; element.SendEvent(evt);
                }
                else
                {
                    using var evt = PointerLeaveEvent.GetPooled(move); evt.target = element; element.SendEvent(evt);
                }
            }
        });
        // SharedFlow and pointer readers resume through UnitySynchronizationContext.
        for (var i = 0; i < 4; i++) await Task.Yield();
        await driver.WaitForIdleAsync(default);
    }

    [Composable]
    private void Content()
    {
        if (_unwrapped)
        {
            var modifier = Modifier.TestTag("button").Clickable(() => _clicks.Value++)
                .Background(Compose.Net.Color.LightBlue).Padding(horizontal: 16.Dp(), vertical: 8.Dp())
                .Clip(RoundedCornerShape(8.Dp()));
            if (_animated)
                AnimatedContent(_clicks.Value, content: value => Spacer(Modifier.Size((80 + value * 10).Dp(), 30.Dp())),
                    modifier: modifier, transitionSpec: _ => FadeIn().TogetherWith(FadeOut()));
            else
                Box(content: () => Spacer(Modifier.Size(80.Dp(), 30.Dp())), modifier: modifier);
            return;
        }
        _probe.Inner = LocalIndication.Current;
        CompositionLocalProvider(LocalIndication.Provides(_probe), () =>
        {
            var modifier = Modifier.TestTag("button").Size(120.Dp(), 60.Dp());
            modifier = _defaultSource
                ? modifier.Clickable(() => _clicks.Value++, enabled: _enabled.Value)
                : modifier.Clickable(() => _clicks.Value++, _probe, _source, enabled: _enabled.Value);
            Box(content: () => Spacer(Modifier.Size(80.Dp(), 30.Dp()).TestTag("child-" + _clicks.Value)),
                modifier: modifier.Background(Compose.Net.Color.LightBlue));
        });
    }
}

// Records commands from the callback actually installed in DrawOn, without adding
// composable state reads or wrapping the indication under test.
public sealed class RippleDrawRecorder : IDrawScope
{
    internal Size DrawSize;
    internal float HoverAlpha;
    internal float PressAlpha;
    internal float PressRadius;
    public Size Size => DrawSize;
    public Offset Center => new(DrawSize.Width / 2, DrawSize.Height / 2);

    public void DrawLine(Compose.Net.Color color, Offset start, Offset end, float strokeWidth = 1, StrokeCap strokeCap = StrokeCap.Butt, float alpha = 1f) {  }
    public void DrawLine(IBrush brush, Offset start, Offset end, float strokeWidth = 1, StrokeCap strokeCap = StrokeCap.Butt, float alpha = 1f) {  }
    public void DrawRect(Compose.Net.Color color, Offset topLeft = default, Size size = default, float alpha = 1f, DrawStyle? style = null) { HoverAlpha = alpha; }
    public void DrawRect(IBrush brush, Offset topLeft = default, Size size = default, float alpha = 1f, DrawStyle? style = null) { HoverAlpha = alpha; }
    public void DrawRoundRect(Compose.Net.Color color, Offset topLeft = default, Size size = default, float cornerRadius = 0f, float alpha = 1f, DrawStyle? style = null) {  }
    public void DrawRoundRect(IBrush brush, Offset topLeft = default, Size size = default, float cornerRadius = 0f, float alpha = 1f, DrawStyle? style = null) {  }
    public void DrawShape(Compose.Net.Color color, Offset topLeft = default, Size size = default, Shape shape = default, float alpha = 1f, DrawStyle? style = null) {  }
    public void DrawShape(IBrush brush, Offset topLeft = default, Size size = default, Shape shape = default, float alpha = 1f, DrawStyle? style = null) {  }
    public void DrawCircle(Compose.Net.Color color, Optional<float> radius = default, Optional<Offset> center = default, float alpha = 1f, DrawStyle? style = null) { PressAlpha = alpha; PressRadius = radius.GetOrDefault(0); }
    public void DrawCircle(IBrush brush, Optional<float> radius = default, Optional<Offset> center = default, float alpha = 1f, DrawStyle? style = null) { PressAlpha = alpha; PressRadius = radius.GetOrDefault(0); }
    public void DrawOval(Compose.Net.Color color, Offset topLeft = default, Size size = default, float alpha = 1f, DrawStyle? style = null) {  }
    public void DrawOval(IBrush brush, Offset topLeft = default, Size size = default, float alpha = 1f, DrawStyle? style = null) {  }
    public void DrawArc(Compose.Net.Color color, float startAngle, float sweepAngle, bool useCenter, Offset topLeft = default, Size size = default, float alpha = 1f, DrawStyle? style = null) {  }
    public void DrawArc(IBrush brush, float startAngle, float sweepAngle, bool useCenter, Offset topLeft = default, Size size = default, float alpha = 1f, DrawStyle? style = null) {  }
    public void DrawPath(IPath path, Compose.Net.Color color, float alpha = 1f, DrawStyle? style = null) {  }
    public void DrawPath(IPath path, IBrush brush, float alpha = 1f, DrawStyle? style = null) {  }
    public void DrawImage(IImageBitmap image, Offset topLeft = default, float alpha = 1, Size size = default, DrawStyle? style = null) {  }
    public void DrawPoints(StableCollections.IStableList<Offset> points, PointMode pointMode, Compose.Net.Color color, float strokeWidth = 0.01f, StrokeCap strokeCap = StrokeCap.Butt, float alpha = 1f) {  }
    public void DrawPoints(StableCollections.IStableList<Offset> points, PointMode pointMode, IBrush brush, float strokeWidth = 0.01f, StrokeCap strokeCap = StrokeCap.Butt, float alpha = 1f) {  }
}

internal partial class RippleProbe : IIndication
{
    internal IIndicationInstance Latest;
    internal IIndication Inner;
    internal bool Hovered;
    internal bool Pressed;
    [Composable]
    public IIndicationInstance RememberUpdatedInstance(IInteractionSource interactionSource)
    {
        Hovered = interactionSource.CollectIsHoveredAsState().Value;
        Pressed = interactionSource.CollectIsPressedAsState().Value;
        Latest = Inner.RememberUpdatedInstance(interactionSource);
        return Latest;
    }
}
