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

    [Test]
    public async Task PaddedClickableTextUsesTheTextElement()
    {
        using var driver = new UnityComposeTestDriver();
        driver.SetContent(ClickableTextContent);
        var item = driver.Root.Q<TextElement>("item");
        Assert.That(item, Is.Not.Null);
        Assert.That(driver.Root.Query().Where(e => e.GetType().Name == "Wrapper").ToList(), Is.Empty);
        var baseline = new Compose.Net.Unity.Text();
        Assert.That(item.style.paddingLeft.value.value - baseline.style.paddingLeft.value.value, Is.EqualTo(8));
        Assert.That(item.resolvedStyle.backgroundColor, Is.EqualTo(new UnityEngine.Color(70f / 255, 130f / 255, 180f / 255)));
        Assert.That(item.style.alignSelf.keyword, Is.EqualTo(StyleKeyword.Null));
        _show.Value = false;
        await driver.WaitForIdleAsync(CancellationToken.None);
        Assert.That(driver.Root.Q<TextElement>("item"), Is.SameAs(item));
        Assert.That(item.text, Is.EqualTo("enabled: false"));
    }

    [Composable]
    private void ClickableTextContent() => Column(horizontalAlignment: Alignment.CenterHorizontally,
        content: () => Text(_show.Value ? "enabled: true" : "enabled: false", color: Color.White,
            modifier: Modifier.Background(Color.SteelBlue).Padding(all: 8.Dp()).Clip()
                .Clickable(() => _show.Value = !_show.Value).TestTag("item")));

    [Test]
    public async Task FirstDragSurvivesOffsetBecomingNonzero()
    {
        _offset.Value = 0;
        using var driver = new UnityComposeTestDriver();
        driver.SetContent(DraggableContent);
        var item = driver.Root.Q("item");
        var parent = item.parent;
        var detached = 0;
        item.RegisterCallback<DetachFromPanelEvent>(_ => detached++);
        Assert.That(driver.Root.Query().Where(e => e.GetType().Name == "Wrapper").ToList(), Is.Empty);
        Assert.That(item.layout.width, Is.EqualTo(60).Within(.1));
        Assert.That(item.layout.height, Is.EqualTo(60).Within(.1));

        void Send(UnityEngine.EventType type, float x)
        {
            var native = new UnityEngine.Event { type = type, button = 0,
                mousePosition = new UnityEngine.Vector2(x, item.worldBound.center.y) };
            if (type == UnityEngine.EventType.MouseDown)
            {
                using var evt = PointerDownEvent.GetPooled(native);
                evt.target = item;
                item.SendEvent(evt);
            }
            else if (type == UnityEngine.EventType.MouseUp)
            {
                using var evt = PointerUpEvent.GetPooled(native);
                driver.Root.SendEvent(evt);
            }
            else
            {
                using var evt = PointerMoveEvent.GetPooled(native);
                driver.Root.SendEvent(evt);
            }
        }
        var start = item.worldBound.center.x;
        Send(UnityEngine.EventType.MouseDown, start);
        Send(UnityEngine.EventType.MouseDrag, start + 20);
        await driver.WaitForIdleAsync(CancellationToken.None);
        var first = _offset.Value;
        Assert.That(first, Is.GreaterThan(0));
        Assert.That(item.parent, Is.SameAs(parent));
        Assert.That(detached, Is.Zero);
        Send(UnityEngine.EventType.MouseDrag, start + 40);
        await driver.WaitForIdleAsync(CancellationToken.None);
        Assert.That(_offset.Value, Is.GreaterThan(first));
        Assert.That(detached, Is.Zero);
        Send(UnityEngine.EventType.MouseUp, start + 40);
        Assert.That(driver.Root.Query().Where(e => e.GetType().Name == "Wrapper").ToList(), Is.Empty);
    }

    [Composable]
    private void DraggableContent() => Column(content: () => Spacer(Modifier.Size(60.Dp())
        .Padding(top: 16.Dp()).Offset(x: _offset.Value.Dp()).Background(Color.MediumSeaGreen)
        .Draggable(Orientation.Horizontal, delta => _offset.Value += delta).TestTag("item")));

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
        Assert.That(driver.Root.Query().Where(e => e.GetType().Name == "Wrapper").ToList(), Is.Empty);
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
        Assert.That(driver.Root.Query().Where(e => e.GetType().Name == "Wrapper").ToList().Count, Is.EqualTo(1));
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
        Assert.That(driver.Root.Query().Where(e => e.GetType().Name == "Wrapper").ToList(), Is.Empty);
        Assert.That(driver.Root.Q("item"), Is.SameAs(content));
        Assert.That(content.resolvedStyle.translate.x, Is.EqualTo(0));
        _mode.Value = 5;
        await driver.WaitForIdleAsync(CancellationToken.None);
        await driver.SizeAsync("item", 100, 60);
        Assert.That(driver.Root.Query().Where(e => e.GetType().Name == "Wrapper").ToList().Count, Is.EqualTo(1));
        _show.Value = false;
        await driver.WaitForIdleAsync(CancellationToken.None);
        Assert.That(driver.Root.Q("item"), Is.Null);
        Assert.That(driver.Root.Query().Where(e => e.GetType().Name == "Wrapper").ToList(), Is.Empty);
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

