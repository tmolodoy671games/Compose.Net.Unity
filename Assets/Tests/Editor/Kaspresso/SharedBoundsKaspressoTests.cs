using System;
using System.Threading.Tasks;
using Compose.Net;
using NUnit.Framework;
using UnityEngine.UIElements;
using static Compose.Net.ComposeFunctions;

public partial class SharedBoundsKaspressoTests
{
    private readonly IMutableState<bool> _expanded = MutableStateOf(false);

    [TestCase(1000, 700)]
    [TestCase(640, 960)]
    public async Task SharedSampleTransformsTheSizedCardWithoutModifierWrappers(int width, int height)
    {
        _expanded.Value = false;
        using var driver = new UnityComposeTestDriver();
        driver.Root.style.width = width;
        driver.Root.style.height = height;
        driver.SetContent(SampleContent);
        void NoWrappers() => Assert.That(driver.Root.Query().Where(e => e.GetType().Name == "Wrapper").ToList(), Is.Empty);
        for (var cycle = 0; cycle < 2; cycle++)
        {
            NoWrappers();
            await driver.SizeAsync("card", 400, 120);
            await driver.Ui.OnNodeWithTag("card").ClickAsync();
            await driver.WaitForIdleAsync(default);
            foreach (var step in new[] { 100, 150, 250, 350 })
            {
                await driver.AdvanceAsync(step);
                NoWrappers();
                var incoming = driver.Root.Q("screen");
                Assert.That(incoming.layout.width, Is.EqualTo(width).Within(.6));
                Assert.That(incoming.layout.height, Is.EqualTo(height).Within(.6));
                var rect = incoming.worldBound;
                Assert.That(rect.width, Is.InRange(399f, width + 1f));
                Assert.That(rect.height, Is.InRange(119f, height + 1f));
                Assert.That(rect.xMin, Is.GreaterThanOrEqualTo(-1));
                Assert.That(rect.yMin, Is.GreaterThanOrEqualTo(-1));
            }
            var screen = driver.Root.Q("screen");
            Assert.That(screen.worldBound.width, Is.EqualTo(width).Within(.6));
            Assert.That(screen.worldBound.height, Is.EqualTo(height).Within(.6));
            var title = driver.Root.Q<TextElement>("title-screen");
            Assert.That(title.worldBound.center.x, Is.EqualTo(screen.worldBound.center.x).Within(5));
            Assert.That(title.worldBound.center.y, Is.EqualTo(screen.worldBound.center.y).Within(5));
            await driver.Ui.OnNodeWithTag("screen").ClickAsync();
            await driver.AdvanceAsync(1100);
            NoWrappers();
        }
    }

    [Composable]
    private void SampleContent() => SharedTransitionLayout(modifier: Modifier.FillMaxSize(), content: shared =>
        AnimatedContent(_expanded.Value, modifier: Modifier.FillMaxSize(), contentAlignment: Alignment.Center,
            transitionSpec: _ => FadeIn(Tween(500)).TogetherWith(FadeOut(Tween(500))),
            content: (scope, expanded) =>
            {
                var modifier = Modifier.SharedBounds(shared.RememberSharedContentState("sample-card"), scope,
                        enter: IEnterTransition.None, exit: IExitTransition.None,
                        boundsTransform: (_, _) => Tween(500))
                    .Clickable(() => _expanded.Value = !expanded);
                Box(contentAlignment: Alignment.Center,
                    modifier: (expanded ? modifier.FillMaxSize() : modifier.Size(400.Dp(), 120.Dp()).Clip(RoundedCornerShape(24.Dp())))
                        .Background(Color.DodgerBlue).TestTag(expanded ? "screen" : "card"),
                    content: () => Text(expanded ? "Screen" : "Open", color: Color.White,
                        fontSize: (expanded ? 96 : 48).Sp(), modifier: Modifier.TestTag(expanded ? "title-screen" : "title-card")));
            }));

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

