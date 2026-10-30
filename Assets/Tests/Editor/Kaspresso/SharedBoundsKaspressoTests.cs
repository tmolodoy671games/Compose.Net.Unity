using System;
using System.Threading.Tasks;
using Compose.Net;
using NUnit.Framework;
using static Compose.Net.ComposeFunctions;

public partial class SharedBoundsKaspressoTests
{
    private readonly IMutableState<bool> _expanded = MutableStateOf(false);

    [Test]
    public async Task InterruptedExpansionReturnsToCard()
    {
        _expanded.Value = false;
        using var driver = new UnityComposeTestDriver();
        driver.SetContent(Content);
        await driver.Ui.OnNodeWithTag("card").ClickAsync();
        await driver.SizeAsync("screen", 1000, 1000);
        await driver.AdvanceAsync(200);
        _expanded.Value = false;
        await driver.AdvanceAsync(1100);
        await driver.SizeAsync("card", 400, 120);
        await driver.Ui.OnNodeWithTag("screen").AssertDoesNotExistAsync();
        await driver.Ui.OnNodeWithTag("card").ClickAsync();
        await driver.AdvanceAsync(1100);
        await driver.SizeAsync("screen", 1000, 1000);
    }

    [TestCase(1000, 700, false)]
    [TestCase(640, 960, true)]
    public async Task CardExpandsToViewportAndCollapsesRepeatedly(int width, int height, bool preview)
    {
        _expanded.Value = false;
        using var driver = new UnityComposeTestDriver();
        driver.Root.style.width = width;
        driver.Root.style.height = height;
        driver.SetContent(Content, preview);
        for (var cycle = 0; cycle < 2; cycle++)
        {
            await driver.SizeAsync("card", 400, 120);
            await driver.Ui.OnNodeWithTag("card").ClickAsync();
            await driver.SizeAsync("screen", width, height);
            await driver.AdvanceAsync(1100);
            await driver.Ui.OnNodeWithTag("card").AssertDoesNotExistAsync();
            await driver.Ui.OnNodeWithTag("screen").AssertAsync(node =>
            {
                var rect = ((UnityComposeTestDriver.Node)node).Element.worldBound;
                return Math.Abs(rect.x) < .6f && Math.Abs(rect.y) < .6f &&
                       Math.Abs(rect.width - width) < .6f && Math.Abs(rect.height - height) < .6f;
            }, "covers the entire viewport");
            await driver.Ui.OnNodeWithTag("screen").ClickAsync();
            await driver.AdvanceAsync(1100);
            await driver.Ui.OnNodeWithTag("screen").AssertDoesNotExistAsync();
        }
    }

    [Composable]
    private void Content() => SharedTransitionLayout(modifier: Modifier.FillMaxSize(), content: shared =>
        AnimatedContent(_expanded.Value,
            modifier: Modifier.FillMaxSize(), contentAlignment: Alignment.Center,
            transitionSpec: _ => FadeIn(Tween(500)).TogetherWith(FadeOut(Tween(500))),
            content: (scope, expanded) =>
            {
                var modifier = Modifier.SharedBounds(shared.RememberSharedContentState("card"), scope,
                    enter: IEnterTransition.None, exit: IExitTransition.None,
                    boundsTransform: (_, _) => Tween(500));
                Box(content: () => { }, contentAlignment: Alignment.Center,
                    modifier: (expanded ? modifier.FillMaxSize() : modifier.Size(400.Dp(), 120.Dp()))
                        .TestTag(expanded ? "screen" : "card")
                        .Background(Color.DodgerBlue)
                        .Clickable(() => _expanded.Value = !expanded, indication: null, interactionSource: null));
            }));
}

public partial class AnimatedContentBoundsKaspressoTests
{
    private readonly IMutableState<int> _state = MutableStateOf(0);
    private string _sizing;

    [TestCase("fill", 968, 684)]
    [TestCase("fixed", 568, 384)]
    [TestCase("width", 968, 30)]
    [TestCase("height", 80, 684)]
    public async Task WrappersFillBoundedAxesDuringOverlapAndAfterResize(string sizing, float width, float height)
    {
        _sizing = sizing;
        _state.Value = 0;
        using var driver = new UnityComposeTestDriver();
        driver.Root.style.height = 700;
        driver.SetContent(Content);
        await WrapperSize(driver, 0, width, height);
        _state.Value = 1;
        await WrapperSize(driver, 0, width, height);
        await WrapperSize(driver, 1, width, height);
        await driver.AdvanceAsync(250);
        await WrapperSize(driver, 0, width, height);
        await WrapperSize(driver, 1, width, height);
        await driver.AdvanceAsync(850);
        await driver.Ui.OnNodeWithTag("content-0").AssertDoesNotExistAsync();
        driver.Root.style.width = 640;
        driver.Root.style.height = 960;
        await WrapperSize(driver, 1, sizing is "fill" or "width" ? 608 : width,
            sizing is "fill" or "height" ? 944 : height);
    }

    private static Task WrapperSize(UnityComposeTestDriver driver, int state, float width, float height) =>
        driver.Ui.OnNodeWithTag("content-" + state).AssertAsync(node =>
        {
            var wrapper = ((UnityComposeTestDriver.Node)node).Element.parent;
            return Math.Abs(wrapper.layout.width - width) < .6f && Math.Abs(wrapper.layout.height - height) < .6f;
        }, $"transition wrapper fills available content area {width} x {height}");

    [Composable]
    private void Content() => Box(modifier: Modifier.FillMaxSize(), content: () =>
        AnimatedContent(_state.Value, content: value => Spacer(Modifier.Size(80.Dp(), 30.Dp()).TestTag("content-" + value)),
            transitionSpec: _ => FadeIn(Tween(500)).TogetherWith(FadeOut(Tween(500))),
            contentAlignment: Alignment.Center,
            modifier: (_sizing == "fixed" ? Modifier.Size(600.Dp(), 400.Dp()) :
                _sizing == "width" ? Modifier.FillMaxWidth() :
                _sizing == "height" ? Modifier.FillMaxHeight() : Modifier.FillMaxSize())
                .Padding(horizontal: 16.Dp(), vertical: 8.Dp())));
}

