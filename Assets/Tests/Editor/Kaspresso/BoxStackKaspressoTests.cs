using System.Threading;
using System.Threading.Tasks;
using Compose.Net;
using Compose.Net.Unity;
using NUnit.Framework;
using UnityEngine.UIElements;
using static Compose.Net.ComposeFunctions;

public partial class BoxStackKaspressoTests
{
    private readonly IMutableState<float> _height = MutableStateOf(60f);
    private readonly IMutableState<bool> _show = MutableStateOf(true);
    private float _bias;

    [TestCase(0f)]
    [TestCase(.5f)]
    [TestCase(1f)]
    public async Task RealChildrenDetermineStackSizeAndKeepTheirMargins(float bias)
    {
        _height.Value = 60;
        _show.Value = true;
        _bias = bias;
        using var driver = new UnityComposeTestDriver();
        driver.SetContent(Content);
        await driver.SizeAsync("box", 80, 70);
        var box = driver.Root.Q("box");
        var second = driver.Root.Q("second");
        Assert.That(box.childCount, Is.EqualTo(2), "Box must contain only its composable children");
        Check(driver, 70, 70);
        _height.Value = 15;
        await driver.WaitForIdleAsync(CancellationToken.None);
        await driver.SizeAsync("box", 80, 30);
        Check(driver, 30, 25);
        ((ILayout)box).Untrack(second);
        Assert.That(second.style.marginBottom.value.value, Is.EqualTo(5).Within(.1), "Untracking restores the child's margin");
        ((ILayout)box).Track(second);
        _show.Value = false;
        await driver.WaitForIdleAsync(CancellationToken.None);
        await driver.SizeAsync("box", 80, 30);
        Assert.That(box.childCount, Is.EqualTo(1));
        Assert.That(second.style.marginBottom.value.value, Is.EqualTo(0).Within(.1), "Disposal also removes the padding modifier");
        _show.Value = true;
        _height.Value = 90;
        await driver.WaitForIdleAsync(CancellationToken.None);
        await driver.SizeAsync("box", 80, 100);
        Check(driver, 100, 100);
        Assert.That(box.childCount, Is.EqualTo(2));
    }

    private void Check(UnityComposeTestDriver driver, float height, float secondHeight)
    {
        var box = driver.Root.Q("box");
        var first = driver.Root.Q("first");
        var second = driver.Root.Q("second");
        Assert.That(first.worldBound.y - box.worldBound.y, Is.EqualTo((height - 30) * _bias + 7).Within(.6));
        Assert.That(second.worldBound.y - box.worldBound.y, Is.EqualTo((height - secondHeight) * _bias + 5).Within(.6));
    }

    [Composable]
    private void Content() => Column(horizontalAlignment: Alignment.Start, content: () =>
        Box(contentAlignment: _bias == 0 ? Alignment.TopLeft : _bias == .5f ? Alignment.Center : Alignment.BottomRight,
            modifier: Modifier.TestTag("box"), content: () =>
            {
                Spacer(Modifier.Size(80.Dp(), 30.Dp()).Offset(y: 7.Dp()).TestTag("first"));
                if (_show.Value)
                    Spacer(Modifier.Padding(horizontal: 3.Dp(), vertical: 5.Dp()).Size(40.Dp(), _height.Value.Dp()).TestTag("second"));
            }));
}
