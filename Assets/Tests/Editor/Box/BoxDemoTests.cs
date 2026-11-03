using System.Linq;
using Compose.Net.Unity.Samples.Behaviors.Content;
using NUnit.Framework;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Tests;

[AutoStaticsCleanup]
public sealed partial class BoxTests
{
    private BoxDemoCase? _demo;
    private BoxDemoState? _gallery;
    [TestCase("align-0")][TestCase("align-1")][TestCase("align-2")][TestCase("align-3")][TestCase("align-4")]
    [TestCase("align-5")][TestCase("align-6")][TestCase("align-7")][TestCase("align-8")]
    [TestCase("largest")][TestCase("empty")][TestCase("empty-min")][TestCase("minimum")][TestCase("fixed-min")]
    [TestCase("maximum")][TestCase("padding")][TestCase("match")][TestCase("only-match")][TestCase("fill")]
    [TestCase("fraction")][TestCase("fill-width")][TestCase("fill-height")][TestCase("child-align")]
    [TestCase("parent-data")][TestCase("toggle-match")][TestCase("order")][TestCase("offset")][TestCase("nested")]
    [TestCase("resize")][TestCase("text")][TestCase("scroll")][TestCase("dynamic")]
    public void EveryDemoCaseChangesAndResetsThroughNativeClicks(string id)
    {
        _demo=new BoxDemoState().Cases.Single(item=>item.Id==id);Mount();
        var first=E("box."+id+".box");var width=first.layout.width;var height=first.layout.height;
        for(var i=1;i<=4;i++)
        {
            simulate.Click(E("box."+id+".change").worldBound.center);Flush();
            Assert.That(_demo.Step.Value,Is.EqualTo(i),id);
            Assert.That(float.IsFinite(_demo.Measured.Value.Width)&&float.IsFinite(_demo.Measured.Value.Height),Is.True,id);
            Assert.That(_demo.Measured.Value.Width,Is.GreaterThanOrEqualTo(0),id);
        }
        simulate.Click(E("box."+id+".reset").worldBound.center);Flush();
        Assert.That(_demo.Step.Value,Is.EqualTo(0));Near(first.layout.width,width);Near(first.layout.height,height);
    }
    [TestCase(360)][TestCase(800)][TestCase(1100)]
    public void GalleryReflowsWithNativeWrapAndScrolls(int width)
    {
        panelSize=new Vector2(width,600);rootVisualElement.style.width=width;
        _gallery=new BoxDemoState();Mount();Assert.That(_gallery.Cases.Length,Is.EqualTo(32));
        foreach(var item in _gallery.Cases)
        {
            var card=E("box."+item.Id+".card");Assert.That(card.layout.height,Is.EqualTo(310).Within(.6));
            Assert.That(card.worldBound.xMin,Is.GreaterThanOrEqualTo(-.6));Assert.That(card.worldBound.xMax,Is.LessThanOrEqualTo(width+.6));
        }
        var scroll=(ScrollView)E("box-demo");Assert.That(scroll,Is.Not.Null);
        Assert.That(scroll.verticalScroller.highValue,Is.GreaterThan(600));
        scroll.scrollOffset=new Vector2(0,scroll.verticalScroller.highValue);Flush();
        Assert.That(E("box.dynamic.card").worldBound.yMax,Is.LessThanOrEqualTo(600.6));
        Assert.That(E("box.dynamic.card").worldBound.yMin,Is.GreaterThanOrEqualTo(0));
    }
    [Test] public void DemoMatchAndMinimumControlsShowDifferentSemantics()
    {
        _demo=new BoxDemoState().Cases.Single(item=>item.Id=="minimum");Mount();
        Near(E("box.minimum.small").layout.width,60);
        _demo.Step.Value++;Flush();Near(E("box.minimum.small").layout.width,150);
    }
}
