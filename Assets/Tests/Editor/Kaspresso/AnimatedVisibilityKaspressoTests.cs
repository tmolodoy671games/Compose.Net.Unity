using System;
using System.Threading.Tasks;
using Compose.Net;
using NUnit.Framework;
using static Compose.Net.ComposeFunctions;

// ReSharper disable CheckNamespace

public partial class AnimatedVisibilityKaspressoTests
{
    private readonly IMutableState<bool> _visible = MutableStateOf(true);
    private readonly IMutableState<float> _height = MutableStateOf(80f);
    private bool _fade;

    [TestCase(true, false)]
    [TestCase(false, false)]
    [TestCase(true, true)]
    [TestCase(false, true)]
    public async Task StateChangesShowAndRemoveContentAfterTransition(bool initiallyVisible, bool fade)
    {
        using var driver = Create(initiallyVisible, fade);
        await driver.Ui.TestCase().RunAsync(async test =>
        {
            await test.StepAsync("Initial visibility follows Compose state", async () =>
            {
                if (initiallyVisible) await driver.SizeAsync("content", 120, 80);
                else await driver.Ui.OnNodeWithTag("content").AssertDoesNotExistAsync();
            });
            for (var cycle = 0; cycle < 2; cycle++)
            {
                await test.StepAsync("Show content", async () =>
                {
                    _visible.Value = true;
                    await driver.Ui.OnNodeWithTag("content").AssertExistsAsync();
                    await driver.AdvanceAsync(1100);
                    await driver.SizeAsync("shell", 120, 80);
                    await driver.Ui.OnNodeWithTag("content").AssertIsDisplayedAsync();
                });
                await test.StepAsync("Exit keeps content composed until completion", async () =>
                {
                    _visible.Value = false;
                    await driver.Ui.OnNodeWithTag("content").AssertExistsAsync();
                    await driver.AdvanceAsync(500);
                    await driver.SizeAsync("shell", 120, 40);
                    await driver.SizeAsync("content", 120, 80);
                    await driver.AdvanceAsync(600);
                    await driver.Ui.OnNodeWithTag("content").AssertDoesNotExistAsync();
                    await driver.Ui.OnNodeWithTag("shell").AssertDoesNotExistAsync();
                });
            }
        });
    }

    [Test]
    public async Task EnterHasCollapsedFirstLayoutAndNaturalContentSize()
    {
        using var driver = Create(false);
        _visible.Value = true;
        await driver.SizeAsync("shell", 120, 0);
        await driver.SizeAsync("content", 120, 80);
        await driver.AdvanceAsync(500);
        await driver.SizeAsync("shell", 120, 40);
        await driver.SizeAsync("content", 120, 80);
        await driver.AdvanceAsync(600);
        await driver.SizeAsync("shell", 120, 80);
    }

    [Test]
    public async Task ReversingExitKeepsContentAndRestoresItsSize()
    {
        using var driver = Create(true);
        _visible.Value = false;
        await driver.Ui.OnNodeWithTag("content").AssertExistsAsync();
        await driver.AdvanceAsync(400);
        await driver.SizeAsync("shell", 120, 48);
        _visible.Value = true;
        await driver.Ui.OnNodeWithTag("content").AssertExistsAsync();
        await driver.AdvanceAsync(1100);
        await driver.SizeAsync("shell", 120, 80);
    }

    [Test]
    public async Task ResizedContentIsUsedByNextExit()
    {
        using var driver = Create(true);
        _height.Value = 160;
        await driver.SizeAsync("content", 120, 160);
        _visible.Value = false;
        await driver.Ui.OnNodeWithTag("content").AssertExistsAsync();
        await driver.AdvanceAsync(500);
        await driver.SizeAsync("shell", 120, 80);
        await driver.SizeAsync("content", 120, 160);
        await driver.AdvanceAsync(600);
        await driver.Ui.OnNodeWithTag("content").AssertDoesNotExistAsync();
    }

    [Test]
    public async Task FadeAndExpansionShareStateDrivenProgress()
    {
        using var driver = Create(false, true);
        _visible.Value = true;
        await driver.Ui.OnNodeWithTag("content").AssertExistsAsync();
        await driver.AdvanceAsync(500);
        await driver.SizeAsync("shell", 120, 40);
        // The transition acts on AnimatedVisibility's inner container.
        await driver.Ui.OnNodeWithTag("content").AssertAsync(node =>
            Math.Abs(((UnityComposeTestDriver.Node)node).Element.parent.resolvedStyle.opacity - .5f) < .02f,
            "has a parent transition at half opacity");
        await driver.AdvanceAsync(600);
        await driver.SizeAsync("shell", 120, 80);
    }

    private UnityComposeTestDriver Create(bool visible, bool fade = false)
    {
        _visible.Value = visible; _height.Value = 80; _fade = fade;
        var driver = new UnityComposeTestDriver();
        try { driver.SetContent(Content); return driver; }
        catch { driver.Dispose(); throw; }
    }

    [Composable]
    private void Content()
    {
        var spec = new AnimationSpec(TimeSpan.FromSeconds(1), TimeSpan.Zero, new Linear());
        var enter = ExpandVertically(spec, initialHeight: _ => 0);
        var exit = ShrinkVertically(spec, targetHeight: _ => 0);
        if (_fade) { enter += FadeIn(spec); exit += FadeOut(spec); }
        AnimatedVisibility(_visible.Value,
            content: () => Spacer(Modifier.Size(120.Dp(), _height.Value.Dp()).TestTag("content")),
            enter: enter, exit: exit, modifier: Modifier.TestTag("shell"));
    }

    private sealed class Linear : IEasing { public float Transform(float fraction) => fraction; }
}
