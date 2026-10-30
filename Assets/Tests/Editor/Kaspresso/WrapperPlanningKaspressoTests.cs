using System.Collections.Generic;
using System.Threading.Tasks;
using Compose.Net;
using NUnit.Framework;
using UnityEngine.UIElements;
using static Compose.Net.ComposeFunctions;

public partial class WrapperPlanningKaspressoTests
{
    private int _scenario;
    private readonly IMutableState<float> _offset = MutableStateOf(0f);
    private readonly IMutableState<bool> _show = MutableStateOf(true);

    private static readonly string[] Names =
    {
        "Size", "PaddingSizeBackground", "SizePaddingBackground", "PaddedClickableTextChain",
        "PaddedDraggable", "BackgroundOffset", "TwoBackgroundsAcrossPadding", "OffsetBackground",
        "ZeroPadding", "AdjacentPadding", "AdjacentOffsets", "RepeatedSize", "ConstrainedInnerSize",
        "PaddedPointerInput", "ClipBetweenBackgrounds", "OffsetBetweenBackgrounds", "TwoOffsetsBetweenBackgrounds",
        "ThreeBackgrounds", "OuterMarginAndInnerPadding", "OffsetPaddingBackground", "FixedOuterBackground",
        "PaddedForegroundDrawing", "PaddedClickable", "AlphaBeforeOffset", "BackgroundPaddingSize"
    };

    public static IEnumerable<TestCaseData> Cases()
    {
        for (var scenario = 0; scenario < Names.Length; scenario++)
        foreach (var initialOffset in new[] { 0f, 12f })
            yield return new TestCaseData(scenario, initialOffset)
                .SetName($"Wrapper_{Names[scenario]}_Offset{initialOffset}");
    }

    [TestCaseSource(nameof(Cases))]
    public async Task PlansUpdatesAndRemovesOnlyRequiredWrappers(int scenario, float initialOffset)
    {
        _scenario = scenario;
        _offset.Value = initialOffset;
        _show.Value = true;
        using var driver = new UnityComposeTestDriver();
        driver.SetContent(Content);

        int Count() => driver.Root.Query().Where(e => e.GetType().Name == "Wrapper").ToList().Count;
        void Check(float offset)
        {
            Assert.That(Count(), Is.EqualTo(ExpectedWrappers(scenario, offset)), "native wrapper count");
            var item = driver.Root.Q("item");
            Assert.That(item, Is.Not.Null);
            Assert.That(float.IsNaN(item.layout.width) || float.IsNaN(item.layout.height), Is.False);
            Assert.That(item.layout.width, Is.GreaterThan(0));
            Assert.That(item.layout.height, Is.GreaterThan(0));
            if (scenario is 0 or 2 or 4 or 11 or 12 or 24)
            {
                Assert.That(item.layout.width, Is.EqualTo(60).Within(.1), "outer size constraint");
                Assert.That(item.layout.height, Is.EqualTo(60).Within(.1), "outer size constraint");
            }
            if (scenario is 4 or 7 or 19)
                Assert.That(item.resolvedStyle.translate.x, Is.EqualTo(offset).Within(.1));
            if (scenario == 10)
                Assert.That(item.resolvedStyle.translate.x, Is.EqualTo(2 * offset).Within(.1), "coalesced offsets");
            if (scenario == 4)
                Assert.That(item.resolvedStyle.paddingTop, Is.EqualTo(16).Within(.1));
            if (scenario is 5 or 15 && offset != 0)
            {
                Assert.That(item.resolvedStyle.translate.x, Is.Zero, "outer bounds stay fixed");
                Assert.That(item[0].resolvedStyle.translate.x, Is.EqualTo(offset).Within(.1));
            }
        }

        Check(initialOffset);
        _offset.Value = 24;
        await driver.WaitForIdleAsync(default);
        Check(24);
        var outer = driver.Root.Q("item");
        var parent = outer.parent;
        var detached = 0;
        outer.RegisterCallback<DetachFromPanelEvent>(_ => detached++);

        _offset.Value = 36;
        await driver.WaitForIdleAsync(default);
        Check(36);
        Assert.That(driver.Root.Q("item"), Is.SameAs(outer), "reuse on value update");
        Assert.That(outer.parent, Is.SameAs(parent));
        Assert.That(detached, Is.Zero, "value update must preserve panel attachment");

        _offset.Value = 0;
        await driver.WaitForIdleAsync(default);
        Check(0);
        _show.Value = false;
        await driver.WaitForIdleAsync(default);
        Assert.That(Count(), Is.Zero, "dispose all wrappers when content disappears");
        Assert.That(driver.Root.Q("item"), Is.Null);
        _show.Value = true;
        await driver.WaitForIdleAsync(default);
        Check(0);
    }

    private static int ExpectedWrappers(int scenario, float offset) => scenario switch
    {
        5 or 15 => offset == 0 ? 0 : 1,
        16 => offset == 0 ? 0 : 2,
        6 or 14 or 18 or 20 => 1,
        17 => 2,
        _ => 0
    };

    [Composable]
    private void Content() => Column(content: () =>
    {
        if (_show.Value) Spacer(Chain().TestTag("item"));
        Spacer(Modifier.Size(10.Dp()).TestTag("sibling"));
    });

    private IModifier Chain()
    {
        var x = _offset.Value.Dp();
        var size = 60.Dp();
        var padding = 8.Dp();
        return _scenario switch
        {
            0 => Modifier.Size(size),
            1 => Modifier.Padding(padding).Size(size).Background(Color.RedColor),
            2 => Modifier.Size(size).Padding(top: padding).Background(Color.RedColor),
            3 => Modifier.Background(Color.SteelBlue).Padding(padding).Clip().Clickable(() => { }).Size(size),
            4 => Modifier.Size(size).Padding(top: 16.Dp()).Offset(x: x).Background(Color.MediumSeaGreen)
                .Draggable(Orientation.Horizontal, delta => _offset.Value += delta),
            5 => Modifier.Background(Color.RedColor).Offset(x: x).Size(size),
            6 => Modifier.Background(Color.RedColor).Padding(padding).Background(Color.BlueColor).Size(size),
            7 => Modifier.Offset(x: x).Background(Color.RedColor).Size(size),
            8 => Modifier.Background(Color.RedColor).Padding(0.Dp()).Background(Color.BlueColor).Size(size),
            9 => Modifier.Padding(padding).Padding(padding).Background(Color.RedColor).Size(size),
            10 => Modifier.Offset(x: x).Offset(x: x).Size(size),
            11 => Modifier.Size(size).Size(40.Dp()),
            12 => Modifier.Size(size).Padding(padding).Size(40.Dp()),
            13 => Modifier.Background(Color.RedColor).Padding(padding)
                .PointerInput(0, _ => Task.CompletedTask).Size(size),
            14 => Modifier.Background(Color.RedColor).Padding(padding).Clip().Background(Color.BlueColor).Size(size),
            15 => Modifier.Background(Color.RedColor).Offset(x: x).Background(Color.BlueColor).Size(size),
            16 => Modifier.Background(Color.RedColor).Offset(x: x).Background(Color.BlueColor)
                .Offset(x: x).Background(Color.SteelBlue).Size(size),
            17 => Modifier.Background(Color.RedColor).Padding(padding).Background(Color.BlueColor)
                .Padding(padding).Background(Color.SteelBlue).Size(size),
            18 => Modifier.Padding(padding).Background(Color.RedColor).Padding(padding)
                .Background(Color.BlueColor).Size(size),
            19 => Modifier.Offset(x: x).Padding(padding).Background(Color.RedColor).Size(size),
            20 => Modifier.Size(size).Background(Color.RedColor).Padding(padding).Background(Color.BlueColor),
            21 => Modifier.Background(Color.RedColor).Padding(padding).DrawOnTop(_ => { }).Size(size),
            22 => Modifier.Background(Color.RedColor).Padding(padding).Clickable(() => { }).Size(size),
            23 => Modifier.Alpha(.5f).Offset(x: x).Size(size),
            _ => Modifier.Background(Color.RedColor).Padding(padding).Size(size)
        };
    }
}
