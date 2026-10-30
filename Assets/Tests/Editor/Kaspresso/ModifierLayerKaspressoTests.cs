using System.Threading;
using System.Threading.Tasks;
using Compose.Net;
using NUnit.Framework;
using UnityEngine.UIElements;
using static Compose.Net.ComposeFunctions;

public partial class ModifierLayerKaspressoTests
{
    private readonly IMutableState<int> _mode = MutableStateOf(0);
    private readonly IMutableState<float> _offset = MutableStateOf(10f);
    private readonly IMutableState<bool> _show = MutableStateOf(true);

    [SetUp]
    public void Reset() { _mode.Value = 0; _offset.Value = 10; _show.Value = true; }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(11)]
    public void SimpleChainsDoNotCreateWrappers(int mode)
    {
        _mode.Value = mode;
        using var driver = new UnityComposeTestDriver();
        driver.SetContent(SimpleContent);
        Assert.That(driver.Root.Query(className: "compose-modifier-wrapper").ToList(), Is.Empty);
        var item = driver.Root.Q("item");
        if (mode == 2)
            Assert.That(item.resolvedStyle.translate.x, Is.EqualTo(30).Within(.1));
        if (mode == 3)
            Assert.That(item.layout.width, Is.EqualTo(100).Within(.1));
    }

    [Test]
    public async Task BackgroundBeforeOffsetKeepsItsBoundsAndReusesWrapper()
    {
        _mode.Value = 4;
        using var driver = new UnityComposeTestDriver();
        driver.SetContent(Content);
        var wrapper = driver.Root.Q("item");
        Assert.That(driver.Root.Query(className: "compose-modifier-wrapper").ToList().Count, Is.EqualTo(1));
        Assert.That(wrapper.resolvedStyle.translate.x, Is.EqualTo(0));
        Assert.That(wrapper.layout.width, Is.EqualTo(80).Within(.1));
        Assert.That(wrapper.layout.height, Is.EqualTo(40).Within(.1));
        Assert.That(wrapper[0].resolvedStyle.translate.x, Is.EqualTo(10));
        _offset.Value = 25;
        await driver.WaitForIdleAsync(CancellationToken.None);
        Assert.That(driver.Root.Q("item"), Is.SameAs(wrapper));
        Assert.That(wrapper[0].resolvedStyle.translate.x, Is.EqualTo(25));
        Assert.That(wrapper[0].layout.width, Is.EqualTo(80).Within(.1));
    }

    [Test]
    public async Task PaddingSeparatesBackgroundBoundsAndCanBeRemovedAndRestored()
    {
        _mode.Value = 5;
        using var driver = new UnityComposeTestDriver();
        driver.SetContent(Content);
        await driver.SizeAsync("item", 100, 60);
        var wrapper = driver.Root.Q("item");
        var content = wrapper[0];
        Assert.That(wrapper.resolvedStyle.paddingLeft, Is.EqualTo(10));
        Assert.That(content.layout.width, Is.EqualTo(80).Within(.1));
        Assert.That(content.layout.height, Is.EqualTo(40).Within(.1));
        Assert.That(wrapper.resolvedStyle.backgroundColor.r, Is.EqualTo(1));
        Assert.That(content.resolvedStyle.backgroundColor.b, Is.EqualTo(1));
        _mode.Value = 0;
        await driver.WaitForIdleAsync(CancellationToken.None);
        Assert.That(driver.Root.Query(className: "compose-modifier-wrapper").ToList(), Is.Empty);
        Assert.That(driver.Root.Q("item"), Is.SameAs(content));
        Assert.That(content.resolvedStyle.translate.x, Is.EqualTo(0));
        _mode.Value = 5;
        await driver.WaitForIdleAsync(CancellationToken.None);
        await driver.SizeAsync("item", 100, 60);
        Assert.That(driver.Root.Query(className: "compose-modifier-wrapper").ToList().Count, Is.EqualTo(1));
        _show.Value = false;
        await driver.WaitForIdleAsync(CancellationToken.None);
        Assert.That(driver.Root.Q("item"), Is.Null);
        Assert.That(driver.Root.Query(className: "compose-modifier-wrapper").ToList(), Is.Empty);
    }

    [Test]
    public async Task RepeatedOffsetUpdatesAndRemovalPreserveSum()
    {
        _mode.Value = 6;
        using var driver = new UnityComposeTestDriver();
        driver.SetContent(Content);
        var item = driver.Root.Q("item");
        Assert.That(item.resolvedStyle.translate.x, Is.EqualTo(30));
        _offset.Value = 25;
        await driver.WaitForIdleAsync(CancellationToken.None);
        Assert.That(item.resolvedStyle.translate.x, Is.EqualTo(45));
        _mode.Value = 0;
        await driver.WaitForIdleAsync(CancellationToken.None);
        Assert.That(item.resolvedStyle.translate.x, Is.EqualTo(0));
    }

    [TestCase(9)]
    [TestCase(10)]
    public void FixedOuterSizeConstrainsTheInnerLayer(int mode)
    {
        _mode.Value = mode;
        using var driver = new UnityComposeTestDriver();
        driver.SetContent(Content);
        var wrapper = driver.Root.Q("item");
        Assert.That(wrapper.layout.width, Is.EqualTo(100).Within(.1));
        Assert.That(wrapper.layout.height, Is.EqualTo(100).Within(.1));
        Assert.That(wrapper[0].layout.width, Is.EqualTo(80).Within(.1));
        Assert.That(wrapper[0].layout.height, Is.EqualTo(80).Within(.1));
        Assert.That(driver.Root.Q("sibling").layout.y, Is.EqualTo(100).Within(.1));
    }

    [Test]
    public async Task BoxKeepsAlignmentWhenAWrapperIsAddedAndRemoved()
    {
        _mode.Value = 5;
        using var driver = new UnityComposeTestDriver();
        driver.SetContent(BoxContent);
        void Check(float x, float y)
        {
            var host = driver.Root.Q("box");
            var item = driver.Root.Q("item");
            Assert.That(item.worldBound.x - host.worldBound.x, Is.EqualTo(x).Within(.6));
            Assert.That(item.worldBound.y - host.worldBound.y, Is.EqualTo(y).Within(.6));
        }
        Check(50, 70);
        _mode.Value = 0;
        await driver.WaitForIdleAsync(CancellationToken.None);
        Check(60, 80);
        _mode.Value = 5;
        await driver.WaitForIdleAsync(CancellationToken.None);
        Check(50, 70);
    }

    [Composable]
    private void BoxContent() => Box(contentAlignment: Alignment.Center,
        modifier: Modifier.Size(200.Dp()).TestTag("box"),
        content: () => Spacer(Chain(_mode.Value).TestTag("item")));

    [Composable]
    private void SimpleContent() => Spacer(Chain(_mode.Value).TestTag("item"));

    [Composable]
    private void Content() => Column(content: () =>
    {
        if (_show.Value) Spacer(Chain(_mode.Value).TestTag("item"));
        Spacer(Modifier.Size(10.Dp()).TestTag("sibling"));
    });

    private IModifier Chain(int mode) => mode switch
    {
        0 => Modifier.Size(80.Dp(), 40.Dp()),
        1 => Modifier.Padding(10.Dp()).Size(80.Dp(), 40.Dp()).Background(Color.RedColor),
        2 => Modifier.Offset(10.Dp()).Offset(20.Dp()).Size(80.Dp()),
        3 => Modifier.Size(100.Dp()).Size(50.Dp()),
        4 => Modifier.Background(Color.RedColor).Offset(_offset.Value.Dp()).Size(80.Dp(), 40.Dp()),
        5 => Modifier.Background(Color.RedColor).Padding(10.Dp()).Background(Color.BlueColor).Size(80.Dp(), 40.Dp()),
        7 => Modifier.Size(80.Dp()).Offset(10.Dp()),
        8 => Modifier.Background(Color.RedColor).Padding(0.Dp()).Background(Color.BlueColor),
        9 => Modifier.Size(100.Dp()).Background(Color.RedColor).Padding(10.Dp()).Background(Color.BlueColor).Size(50.Dp()),
        10 => Modifier.Size(100.Dp()).Background(Color.RedColor).Padding(10.Dp()).Background(Color.BlueColor),
        11 => Modifier.Size(100.Dp()).Padding(10.Dp()).Size(50.Dp()),
        _ => Modifier.Offset(_offset.Value.Dp()).Offset(20.Dp()).Size(80.Dp(), 40.Dp()),
    };
}

