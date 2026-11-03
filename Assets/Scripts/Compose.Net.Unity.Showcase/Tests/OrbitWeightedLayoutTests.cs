using System.Collections;
using System.Linq;
using Compose.Net;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static Compose.Net.ComposeFunctions;

namespace Compose.Net.Unity.Showcase.Tests;

public sealed partial class OrbitScreenTests
{
    private readonly IMutableState<bool> _rowHasWeights = MutableStateOf(true);

    [Composable] private void WeightedRowsProbe()
    {
        Column(modifier: Modifier.FillMaxSize().Padding(24.Dp()), content: () =>
        {
            Row(modifier: Modifier.TestTag("weight.auto"), content: () =>
            {
                Text("A short title", modifier: (_rowHasWeights.Value ? Modifier.Weight(1) : Modifier).TestTag("weight.first"));
                Text("Another title", modifier: (_rowHasWeights.Value ? Modifier.Weight(1) : Modifier).TestTag("weight.second"));
                Box(Modifier.Size(80.Dp(), 40.Dp()));
            });
            Row(modifier: Modifier.Width(320.Dp()).TestTag("weight.fixed"), content: () =>
            {
                Text("Fixed width", modifier: Modifier.Weight(1));
                Box(Modifier.Size(80.Dp(), 40.Dp()));
            });
        });
    }

    [UnityTest] public IEnumerator WeightedRowsFillAvailableWidthAndRestoreWrapContent()
    {
        _composer.Dispose(); rootVisualElement.Clear(); _composer = IComposer.Create();
        _rowHasWeights.Value = true;
        Bootstrap(_composer, _adapter, false, new ComposeConfig(Logging: false), new RootNode(rootVisualElement), WeightedRowsProbe);
        yield return Settle();
        foreach (var width in new[] { 1280, 760, 1800 })
        {
            panelSize = new Vector2(width, 900); yield return Settle();
            var row = Find("weight.auto");
            Assert.That(row.layout.width, Is.EqualTo(row.parent.contentRect.width).Within(1));
            Assert.That(Find("weight.first").layout.width, Is.EqualTo(Find("weight.second").layout.width).Within(2));
            Assert.That(Find("weight.fixed").layout.width, Is.EqualTo(320).Within(1), "An authored width must be preserved");
        }
        _rowHasWeights.Value = false; yield return Settle();
        Assert.That(Find("weight.auto").layout.width, Is.LessThan(450), "Removing every weight restores intrinsic sizing");
        _rowHasWeights.Value = true; yield return Settle();
        Assert.That(Find("weight.auto").layout.width, Is.EqualTo(Find("weight.auto").parent.contentRect.width).Within(1));
    }

    [UnityTest] public IEnumerator WeightedCardHeadersAndReplayTilesStayReadable()
    {
        yield return Settle();
        foreach (var size in new[] { new Vector2(1280, 900), new Vector2(2650, 742), new Vector2(760, 900) })
        {
            panelSize = size; Click("nav.Overview"); yield return Settle();
            CheckHeader("Creative momentum", 90);
            CapturePreview("momentum-width-" + (int)size.x);
            Click("nav.Motion"); yield return Settle();
            CheckHeader("A chain reaction", 65);
            CapturePreview("replay-width-" + (int)size.x);
            var tiles = Find("screen.Motion").Query<TextElement>().ToList()
                .Where(e => new[] { "01", "02", "03", "04", "05", "06" }.Contains(e.text)).OrderBy(e => e.text).ToArray();
            Assert.That(tiles.Length, Is.EqualTo(6));
            var row = tiles[0].GetFirstAncestorOfType<Row>();
            var boxes = tiles.Select(e => e.GetFirstAncestorOfType<Box>()).ToArray();
            for (var i = 0; i < boxes.Length; i++)
            {
                Assert.That(boxes[i].layout.width, Is.InRange(row.contentRect.width / 8, row.contentRect.width / 5), "Replay tile width: " + i);
                Assert.That(tiles[i].worldBound.center.x, Is.InRange(boxes[i].worldBound.xMin, boxes[i].worldBound.xMax), "Tile number must stay inside its card");
                if (i > 0) Assert.That(boxes[i].worldBound.xMin, Is.GreaterThanOrEqualTo(boxes[i - 1].worldBound.xMax + 5), "Replay cards must not overlap");
            }
        }
    }

    private void CheckHeader(string text, float maxHeight)
    {
        var title = rootVisualElement.Query<TextElement>().ToList().Single(e => e.text == text);
        var row = title.GetFirstAncestorOfType<Row>();
        row.GetFirstAncestorOfType<ScrollView>()?.ScrollTo(row);
        Flush();
        Assert.That(title.layout.width, Is.GreaterThan(170), text + " must not collapse to one letter per line");
        Assert.That(row.layout.height, Is.LessThan(maxHeight), text + " header must stay compact");
        Assert.That(row.layout.width, Is.EqualTo(row.parent.contentRect.width).Within(2), text + " weighted row must fill available width");
    }
}
