using Compose.Net;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Samples.Tests;

public sealed partial class AnimateContentSizeDemoTests
{
    [Test]
    public void CardsRowsAndControlsKeepVisibleGaps()
    {
        Flush();
        for (var row = 0; row < 5; row++)
        {
            var first = row * 3;
            for (var column = 0; column < 3; column++)
            {
                var tag = "animate." + _state.Cases[first + column].Id;
                var card = Element(tag + ".card");
                var stage = Element(tag + ".stage");
                Assert.That(card.resolvedStyle.paddingLeft, Is.EqualTo(12));
                Assert.That(card.resolvedStyle.paddingRight, Is.EqualTo(12));
                Assert.That(stage.worldBound.xMin - card.worldBound.xMin, Is.GreaterThanOrEqualTo(12));
                Assert.That(Element(tag + ".small").worldBound.yMin - stage.worldBound.yMax,
                    Is.GreaterThanOrEqualTo(7.5f), tag + " stage-to-controls gap");
                Assert.That(Element(tag + ".medium").worldBound.xMin - Element(tag + ".small").worldBound.xMax,
                    Is.GreaterThanOrEqualTo(7.5f), tag + " button gap");
                if (column > 0)
                    Assert.That(card.worldBound.xMin - Element("animate." + _state.Cases[first + column - 1].Id + ".card").worldBound.xMax,
                        Is.GreaterThanOrEqualTo(11.5f), tag + " card gap");
                if (row > 0)
                    Assert.That(card.worldBound.yMin - Element("animate." + _state.Cases[first + column - 3].Id + ".card").worldBound.yMax,
                        Is.GreaterThanOrEqualTo(11.5f), tag + " row gap");
            }
        }
        Assert.That(Element("animate.collapse-all").worldBound.xMin - Element("animate.expand-all").worldBound.xMax,
            Is.GreaterThanOrEqualTo(7.5f));
    }

    [Test]
    public void LargestTargetAndFollowingSiblingFitInsideTheStage()
    {
        _state["neighbors"].Large(); Flush(); Flush(1000);
        var target = Target("neighbors");
        var sibling = Element("animate.neighbors.neighbor");
        var stage = Element("animate.neighbors.stage");
        SizeIs(target, 216, 120);
        Assert.That(sibling.worldBound.yMin, Is.GreaterThanOrEqualTo(target.worldBound.yMax));
        Assert.That(sibling.worldBound.yMax, Is.LessThanOrEqualTo(stage.worldBound.yMax - stage.resolvedStyle.paddingBottom));
    }
}
