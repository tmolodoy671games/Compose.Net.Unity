using System;
using System.Threading.Tasks;
using Compose.Net;
using NUnit.Framework;
using UnityEngine.UIElements;
using static Compose.Net.ComposeFunctions;

public partial class IncomingContentKaspressoTests
{
    private readonly IMutableState<int> _state = MutableStateOf(0);
    private string _mode;
    private bool _animateSize;

    [TestCase("vertical", false)]
    [TestCase("horizontal", false)]
    [TestCase("both", false)]
    [TestCase("fade", false)]
    [TestCase("vertical", true)]
    [TestCase("horizontal", true)]
    [TestCase("both", true)]
    [TestCase("fade", true)]
    public async Task IncomingTransitionHasInitialIntermediateAndFinalFrame(string mode, bool animateSize)
    {
        _state.Value = 0; _mode = mode; _animateSize = animateSize;
        using var driver = new UnityComposeTestDriver();
        driver.SetContent(Content);
        await driver.Ui.TestCase().RunAsync(async test =>
        {
            _state.Value = 1;
            await test.StepAsync("Incoming content starts at its enter value after first layout", () => Check(driver, 0));
            await test.StepAsync("Incoming content interpolates halfway", async () =>
            {
                await driver.AdvanceAsync(500);
                await Check(driver, .5f);
            });
            await test.StepAsync("Incoming content reaches rest position", async () =>
            {
                await driver.AdvanceAsync(500);
                await Check(driver, 1);
            });
        });
    }

    private Task Check(UnityComposeTestDriver driver, float progress) => driver.Ui.OnNodeWithTag("incoming-1").AssertAsync(node =>
    {
        var element = ((UnityComposeTestDriver.Node)node).Element;
        var item = element.parent;
        if (_mode == "fade")
            return Math.Abs(item.resolvedStyle.opacity - progress) < .02f;
        var host = driver.Root.Q<VisualElement>("transition-host");
        var x = element.worldBound.x - host.worldBound.x - host.resolvedStyle.paddingLeft;
        var y = element.worldBound.y - host.worldBound.y - host.resolvedStyle.paddingTop;
        var expectedX = _mode == "vertical" ? 0 : 80 * (1 - progress);
        var expectedY = _mode == "horizontal" ? 0 : 30 * (1 - progress);
        if (Math.Abs(x - expectedX) > .6f || Math.Abs(y - expectedY) > .6f || float.IsNaN(x) || float.IsNaN(y))
            throw new Compose.Net.Kaspresso.KaspressoAssertionException($"Incoming {_mode}: expected ({expectedX}, {expectedY}), actual ({x}, {y})");
        return true;
    }, "has enter progress " + progress);

    [Composable]
    private void Content() => Box(contentAlignment: Alignment.Center, modifier: Modifier.FillMaxSize(), content: () =>
        AnimatedContent(_state.Value, content: value => Spacer(Modifier.Size(80.Dp(), 30.Dp()).TestTag("incoming-" + value)),
            transitionSpec: _ => Transform(), modifier: _animateSize
                ? Modifier.TestTag("transition-host").AnimateContentSize(Tween(1000, easing: LinearEasing))
                : Modifier.TestTag("transition-host")));

    private ContentTransform Transform()
    {
        var tween = Tween(1000, easing: LinearEasing);
        return _mode switch
        {
            "vertical" => SlideInVertically(tween, size => size).TogetherWith(SlideOutVertically(tween, size => -size)),
            "horizontal" => SlideInHorizontally(tween, size => size).TogetherWith(SlideOutHorizontally(tween, size => -size)),
            "both" => SlideIn(tween, size => new Offset(size.Width, size.Height))
                .TogetherWith(SlideOut(tween, size => new Offset(-size.Width, -size.Height))),
            _ => FadeIn(tween).TogetherWith(FadeOut(tween))
        };
    }
}
