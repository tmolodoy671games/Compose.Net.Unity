using System.Threading.Tasks;
using Compose.Net;
using NUnit.Framework;
using static Compose.Net.ComposeFunctions;

// Regression tests use UI Toolkit's actual Yoga layout and Unity transition factories.
public partial class AnimatedContentKaspressoTests
{
    private readonly IMutableState<int> _state = MutableStateOf(0);
    private float _targetWidth;
    private float _targetHeight;
    private bool _slide;

    [TestCase(160, 90, false)]
    [TestCase(40, 15, false)]
    [TestCase(160, 90, true)]
    [TestCase(40, 15, true)]
    public async Task PaddedContentMeasuresNaturallyAndSettles(float width, float height, bool slide)
    {
        using var driver = Create(width, height, slide);
        await driver.Ui.TestCase().RunAsync(async test =>
        {
            await test.StepAsync("Initial auto size includes padding", () => driver.SizeAsync("animated", 112, 46));
            await test.StepAsync("Both states retain natural size during overlap", async () =>
            {
                _state.Value = 1;
                await driver.SizeAsync("content-0", 80, 30);
                await driver.SizeAsync("content-1", width, height);
                await driver.AdvanceAsync(500);
                await driver.SizeAsync("content-0", 80, 30);
                await driver.SizeAsync("content-1", width, height);
            });
            await test.StepAsync("After exit the host reaches target and stays stable", async () =>
            {
                await driver.AdvanceAsync(2100);
                await driver.Ui.OnNodeWithTag("content-0").AssertDoesNotExistAsync();
                for (var i = 0; i < 12; i++) await driver.SizeAsync("animated", width + 32, height + 16);
            });
        });
    }

    [TestCase(160, 90, false)]
    [TestCase(40, 15, false)]
    [TestCase(160, 90, true)]
    [TestCase(40, 15, true)]
    public async Task InterruptedTransitionsDoNotCollapseOrCreateLayoutLoops(float width, float height, bool slide)
    {
        using var driver = Create(width, height, slide);
        await driver.Ui.TestCase().RunAsync(async test =>
        {
            await test.StepAsync("Interrupt an in-flight transition with a third state", async () =>
            {
                _state.Value = 1;
                await driver.AdvanceAsync(250);
                _state.Value = 0;
                await driver.AdvanceAsync(250);
                _state.Value = 2;
                await driver.SizeAsync("content-2", 200, 60);
            });
            await test.StepAsync("Only the last state remains at its natural size", async () =>
            {
                await driver.AdvanceAsync(2600);
                await driver.Ui.OnNodeWithTag("content-0").AssertDoesNotExistAsync();
                await driver.Ui.OnNodeWithTag("content-1").AssertDoesNotExistAsync();
                await driver.SizeAsync("animated", 232, 76);
            });
        });
    }

    [TestCase(160, 90, false)]
    [TestCase(40, 15, false)]
    [TestCase(160, 90, true)]
    [TestCase(40, 15, true)]
    public async Task RepeatedGrowAndShrinkDoesNotDrift(float width, float height, bool slide)
    {
        using var driver = Create(width, height, slide);
        await driver.Ui.TestCase().RunAsync(async test =>
        {
            for (var cycle = 0; cycle < 3; cycle++)
            {
                await test.StepAsync("Grow or shrink and return, cycle " + cycle, async () =>
                {
                    _state.Value = 1;
                    await driver.AdvanceAsync(2600);
                    await driver.SizeAsync("animated", width + 32, height + 16);
                    _state.Value = 0;
                    await driver.AdvanceAsync(2600);
                    await driver.SizeAsync("animated", 112, 46);
                });
            }
        });
    }

    private UnityComposeTestDriver Create(float width, float height, bool slide)
    {
        _state.Value = 0;
        _targetWidth = width; _targetHeight = height; _slide = slide;
        var driver = new UnityComposeTestDriver();
        try { driver.SetContent(Content); return driver; }
        catch { driver.Dispose(); throw; }
    }

    [Composable]
    private void Content() => Box(contentAlignment: Alignment.Center, modifier: Modifier.FillMaxSize(), content: () =>
        AnimatedContent(_state.Value,
            transitionSpec: _ => _slide
                ? SlideInVertically(Tween(1000, easing: LinearEasing), size => size)
                    .TogetherWith(SlideOutVertically(Tween(1000, easing: LinearEasing), size => -size))
                : FadeIn(Tween(1000, easing: LinearEasing)).TogetherWith(FadeOut(Tween(1000, easing: LinearEasing))),
            modifier: Modifier.TestTag("animated").Background(Color.LightBlue)
                .Padding(horizontal: 16.Dp(), vertical: 8.Dp())
                .AnimateContentSize(Tween(1000, easing: LinearEasing)),
            content: value => Spacer(Modifier.Size(
                (value == 0 ? 80 : value == 1 ? _targetWidth : 200).Dp(),
                (value == 0 ? 30 : value == 1 ? _targetHeight : 60).Dp()).TestTag("content-" + value))));
}
