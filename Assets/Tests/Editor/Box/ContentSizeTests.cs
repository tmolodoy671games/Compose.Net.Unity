using Compose.Net;
using NUnit.Framework;
using UnityEngine.UIElements;
using static Compose.Net.ComposeFunctions;

namespace Compose.Net.Unity.Tests;

public sealed partial class BoxTests
{
    [TestCase(true, 30, 80, 90, 0)]
    [TestCase(false, 120, 20, 0, 60)]
    public void WrapContentRelaxesOnlySelectedAxis(bool horizontal, int w, int h, int x, int y)
    {
        var outer = Modifier.Size(120.Dp(), 80.Dp());
        var wrap = horizontal ? outer.WrapContentWidth(Alignment.End) : outer.WrapContentHeight(Alignment.Bottom);
        _items.Add(new Item("item", wrap.Size(30.Dp(), 20.Dp())));
        Mount();
        Size("box", 120, 80);
        Size("item", w, h);
        At("item", x, y);
    }

    [TestCase(false, 80, 40, 0, 0)]
    [TestCase(true, 120, 70, -20, -15)]
    public void WrapContentSizeKeepsParentBoundsOnOverflow(bool unbounded, int w, int h, int x, int y)
    {
        _items.Add(new Item("item", Modifier.Size(80.Dp(), 40.Dp())
            .WrapContentSize(unbounded: unbounded).Size(120.Dp(), 70.Dp())));
        Mount();
        Size("box", 80, 40);
        Size("item", w, h);
        At("item", x, y);
    }

    [Test]
    public void WrapContentUpdatesAlignmentAndDetaches()
    {
        var outer = Modifier.Size(90.Dp(), 60.Dp());
        _items.Add(new Item("item", outer.WrapContentSize().Size(20.Dp(), 10.Dp())));
        Mount();
        At("item", 35, 25);
        _items[0] = new Item("item", outer.WrapContentSize(Alignment.BottomEnd).Size(20.Dp(), 10.Dp()));
        Update();
        At("item", 70, 50);
        _items[0] = new Item("item", outer.Size(20.Dp(), 10.Dp()));
        Update();
        Size("item", 90, 60);
        At("item", 0, 0);
    }

    [TestCase(false, 60, 0)]
    [TestCase(true, 100, -20)]
    public void IntrinsicWidthHonorsPreferredAndRequiredConstraints(bool required, int width, int x)
    {
        var outer = Modifier.Width(60.Dp());
        var intrinsic = required ? outer.RequiredWidth(IntrinsicSize.Max) : outer.Width(IntrinsicSize.Max);
        _items.Add(new Item("item", intrinsic.Size(100.Dp(), 20.Dp())));
        Mount();
        Size("box", 60, 20);
        Size("item", width, 20);
        At("item", x, 0);
    }

    [TestCase(false, 50, 0)]
    [TestCase(true, 20, 15)]
    public void IntrinsicHeightCentersRequiredContentBelowMinimum(bool required, int height, int y)
    {
        var outer = Modifier.Height(50.Dp());
        var intrinsic = required ? outer.RequiredHeight(IntrinsicSize.Min) : outer.Height(IntrinsicSize.Min);
        _items.Add(new Item("item", intrinsic.Size(30.Dp(), 20.Dp())));
        Mount();
        Size("box", 30, 50);
        Size("item", 30, height);
        At("item", 0, y);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void RequiredIntrinsicSizeCentersContentOnBothAxes(bool width)
    {
        var outer = Modifier.Size(80.Dp(), 60.Dp());
        var intrinsic = width ? outer.RequiredWidth(IntrinsicSize.Min) : outer.RequiredHeight(IntrinsicSize.Max);
        _items.Add(new Item("item", intrinsic.Size(100.Dp(), 20.Dp())));
        Mount();
        Size("box", 80, 60);
        Size("item", 100, 20);
        At("item", -10, 20);
    }

    [Test]
    public void IntrinsicWidthIncludesInnerPaddingOnce()
    {
        _items.Add(new Item("item", Modifier.Width(IntrinsicSize.Max).Padding(10.Dp()).Size(20.Dp(), 15.Dp())));
        Mount();
        Size("box", 40, 35);
        Size("item", 20, 15);
        At("item", 10, 10);
    }

    [Test]
    public void UnboundedWidthIgnoresAnInnerFillButAllowsNaturalContent()
    {
        _box = Modifier.Size(90.Dp(), 60.Dp()).WrapContentWidth(unbounded: true).FillMaxWidth();
        Add("leaf", 30, 15);
        Mount();
        Size("leaf", 30, 15);
        Near(E("box").layout.width, 30);
        Near(E("box").worldBound.x - rootVisualElement.worldBound.x, 30);
    }

    [TestCase(false, 20, 80)]
    [TestCase(true, 120, -20)]
    public void WrapContentWidthFollowsRtlStart(bool unbounded, int width, int x)
    {
        rootVisualElement.languageDirection = LanguageDirection.RTL;
        try
        {
            _items.Add(new Item("item", Modifier.Size(100.Dp(), 30.Dp()).WrapContentWidth(Alignment.Start, unbounded).Width(width.Dp())));
            Mount();
            At("item", x, 0);
        }
        finally { rootVisualElement.languageDirection = LanguageDirection.LTR; }
    }

    [TestCase(false, 50)]
    [TestCase(true, 100)]
    public void IntrinsicWidthRespectsLooseParentMaximum(bool required, int expectedWidth)
    {
        _box = Modifier.WidthIn(max: 50.Dp());
        var intrinsic = required ? Modifier.RequiredWidth(IntrinsicSize.Max) : Modifier.Width(IntrinsicSize.Max);
        _items.Add(new Item("item", intrinsic.Size(100.Dp(), 20.Dp())));
        Mount();
        Size("box", 50, 20);
        Size("item", expectedWidth, 20);
        At("item", required ? -25 : 0, 0);
    }

    [Composable]
    private void WeightedIntrinsicRow()
    {
        Row(modifier: Modifier.Width(IntrinsicSize.Min).TestTag("weighted-row"), content: () =>
        {
            Box(Modifier.Weight(1).Width(30.Dp()).TestTag("one"));
            Box(Modifier.Weight(2).Width(25.Dp()).TestTag("two"));
        });
    }

    [Test]
    public void IntrinsicWidthUsesProportionalSharesForWeightedRow()
    {
        _composer = IComposer.Create();
        Bootstrap(_composer, _adapter, false, new ComposeConfig(Logging: false), new Root(rootVisualElement), WeightedIntrinsicRow);
        Flush();
        Near(E("weighted-row").layout.width, 90);
    }


    private IntrinsicSize _intrinsicMode;
    [Composable]
    private void WeightedIntrinsicColumn()
    {
        Column(modifier: Modifier.Height(_intrinsicMode).TestTag("weighted-column"), content: () =>
        {
            Box(Modifier.Weight(1).Height(30.Dp()));
            Box(Modifier.Weight(2).Height(25.Dp()));
        });
    }

    [TestCase(IntrinsicSize.Min)]
    [TestCase(IntrinsicSize.Max)]
    public void IntrinsicHeightUsesProportionalSharesForWeightedColumn(IntrinsicSize mode)
    {
        _intrinsicMode = mode;
        _composer = IComposer.Create();
        Bootstrap(_composer, _adapter, false, new ComposeConfig(Logging: false), new Root(rootVisualElement), WeightedIntrinsicColumn);
        Flush();
        Near(E("weighted-column").layout.height, 90);
    }
    private int _rowWidth;
    [Composable]
    private void IntrinsicRow()
    {
        var revision = _revision.Value;
        Row(modifier: Modifier.Width(_rowWidth.Dp()).Height(IntrinsicSize.Min).TestTag("row"), content: () =>
        {
            Box(Modifier.Width(2.Dp()).FillMaxHeight().TestTag("divider"));
            Text("a sentence that wraps onto several lines", fontSize: 16.Sp(),
                modifier: Modifier.Weight(1).TestTag("body"));
        });
    }

    [Test]
    public void IntrinsicHeightMeasuresWeightedTextAtAvailableWidth()
    {
        _rowWidth = 100;
        _composer = IComposer.Create();
        Bootstrap(_composer, _adapter, false, new ComposeConfig(Logging: false), new Root(rootVisualElement), IntrinsicRow);
        Flush();
        var label = (TextElement)E("body");
        float HeightAt(int width) => label.MeasureTextSize(label.text, width - 2, VisualElement.MeasureMode.AtMost,
            0, VisualElement.MeasureMode.Undefined).y;
        Near(E("row").layout.height, HeightAt(_rowWidth));
        Near(E("divider").layout.height, E("row").layout.height);
        _rowWidth = 200;
        Update();
        Near(E("row").layout.height, HeightAt(_rowWidth));
        Near(E("divider").layout.height, E("row").layout.height);
    }

    private string _intrinsicText = "";
    [Composable]
    private void IntrinsicColumn()
    {
        var revision = _revision.Value;
        Column(modifier: Modifier.Width(_intrinsicMode).TestTag("column"), content: () =>
        {
            Text(_intrinsicText, fontSize: 16.Sp(), modifier: Modifier.TestTag("label"));
            Box(Modifier.FillMaxWidth().Height(2.Dp()).TestTag("line"));
        });
    }

    [Test]
    public void IntrinsicColumnMeasuresTextAndSizesFillSiblingAfterRecomposition()
    {
        _intrinsicMode = IntrinsicSize.Min;
        _intrinsicText = "hello medium hello";
        _composer = IComposer.Create();
        Bootstrap(_composer, _adapter, false, new ComposeConfig(Logging: false), new Root(rootVisualElement), IntrinsicColumn);
        Flush();
        var label = (TextElement)E("label");
        float WidthOf(string text) => label.MeasureTextSize(text, 0, VisualElement.MeasureMode.Undefined,
            0, VisualElement.MeasureMode.Undefined).x;
        Assert.That(E("column").layout.width, Is.EqualTo(WidthOf("medium")).Within(1));
        Near(E("line").layout.width, E("column").layout.width);
        _intrinsicMode = IntrinsicSize.Max;
        Update();
        Assert.That(E("column").layout.width, Is.EqualTo(WidthOf(_intrinsicText)).Within(1));
        Near(E("line").layout.width, E("column").layout.width);
        _intrinsicText = "short";
        Update();
        Assert.That(E("column").layout.width, Is.EqualTo(WidthOf(_intrinsicText)).Within(1));
        Near(E("line").layout.width, E("column").layout.width);
    }
}
