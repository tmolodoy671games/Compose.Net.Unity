using Compose.Net;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Tests;

public sealed partial class BoxTests
{
    [Test, BoxScope] public void ZeroAndOneChildUseFlexAndMultipleChildrenUseGrid()
    {
        var box=new Compose.Net.Unity.Box(); box.style.alignSelf=Align.FlexStart; rootVisualElement.Add(box);
        simulate.FrameUpdateMs(0); Assert.That(box.resolvedStyle.display,Is.EqualTo(DisplayStyle.Flex));
        var first=new VisualElement(); first.style.width=120; first.style.height=50;
        box.Add(first); box.Track(first); simulate.FrameUpdateMs(0);
        Near(box.layout.width,120); Near(box.layout.height,50); Assert.That(box.resolvedStyle.display,Is.EqualTo(DisplayStyle.Flex));
        var second=new VisualElement(); second.style.width=60; second.style.height=90;
        box.Add(second); box.Track(second); simulate.FrameUpdateMs(0);
        Near(box.layout.width,120); Near(box.layout.height,90); Assert.That(box.resolvedStyle.display,Is.EqualTo(DisplayStyle.Grid));
        box.Untrack(second); second.RemoveFromHierarchy(); simulate.FrameUpdateMs(0);
        Near(box.layout.width,120); Near(box.layout.height,50); Assert.That(box.resolvedStyle.display,Is.EqualTo(DisplayStyle.Flex));
        box.Untrack(first); first.RemoveFromHierarchy(); simulate.FrameUpdateMs(0); Near(box.layout.width,0); Near(box.layout.height,0);
    }
    [Test, BoxScope] public void SingleTextKeepsNativeIntrinsicDimensionsAcrossGridChanges()
    {
        _regressionText="#1"; _regressionFont=128; Mount();
        var text=E("sample-text"); var natural=text.layout.size;
        Assert.That(text.style.width.keyword,Is.EqualTo(StyleKeyword.Null).Or.EqualTo(StyleKeyword.Auto));
        Assert.That(text.style.height.keyword,Is.EqualTo(StyleKeyword.Null).Or.EqualTo(StyleKeyword.Auto));
        Add("a",20,20); Update(); _items.Clear(); Update();
        Assert.That(E("box").resolvedStyle.display,Is.EqualTo(DisplayStyle.Flex));
        Assert.That(text.style.width.keyword,Is.EqualTo(StyleKeyword.Null).Or.EqualTo(StyleKeyword.Auto));
        Assert.That(text.style.height.keyword,Is.EqualTo(StyleKeyword.Null).Or.EqualTo(StyleKeyword.Auto));
        Near(text.layout.width,natural.x); Near(text.layout.height,natural.y);
    }
}
