using Compose.Net;
using static Compose.Net.ComposeFunctions;
using NUnit.Framework;
namespace Compose.Net.Unity.Tests;
public sealed partial class BoxTests
{
    [TestCase(false,16),TestCase(false,32),TestCase(false,64),TestCase(false,96)]
    [TestCase(true,16),TestCase(true,32),TestCase(true,64),TestCase(true,96)]
    [BoxScope]
    public void CenteredTextKeepsPaddingWhenGrowingAndShrinking(bool animated,int font)
    {
        _alignment=Alignment.Center;_regressionFont=font;_regressionText="Short";
        _box=Modifier.Clip(RoundedCornerShape(32.Dp())).Padding(16.Dp()).Background(Color.LightCoral);
        if(animated)_box=_box.AnimateContentSize(Tween(300));
        Mount();SettleText();AssertTextFits();
        for(var cycle=0;cycle<2;cycle++)
        {
            _regressionText="Looooong\nLooooong\nLooooong";
            Update();SettleText();AssertTextFits();
            _regressionText="Short";
            Update();SettleText();AssertTextFits();
        }
    }
    private void SettleText()
    { for(var i=0;i<3;i++){simulate.FrameUpdateMs(350);Flush();} }
    private void AssertTextFits()
    {
        var box=E("box");var text=E("sample-text");var content=text.parent;
        Assert.That(text.layout.yMin,Is.GreaterThanOrEqualTo(content.resolvedStyle.paddingTop-.6f),"Text escaped the measured grid at the top");
        Assert.That(text.layout.yMax,Is.LessThanOrEqualTo(content.layout.height-content.resolvedStyle.paddingBottom+.6f),"Text escaped the measured grid at the bottom");
        // The text content box itself must be centered, not merely its margin box.
        Near(text.worldBound.center.x,box.worldBound.center.x);
        Near(text.worldBound.center.y,box.worldBound.center.y);
        Near(text.contentRect.center.x,text.layout.width/2);
        Near(text.contentRect.center.y,text.layout.height/2);
        Near(text.resolvedStyle.paddingLeft+text.resolvedStyle.paddingRight,0);
        Near(text.resolvedStyle.paddingTop+text.resolvedStyle.paddingBottom,0);
        Near(text.resolvedStyle.marginLeft+text.resolvedStyle.marginRight,0);
        Near(text.resolvedStyle.marginTop+text.resolvedStyle.marginBottom,0);
        Near(text.worldBound.yMin-box.worldBound.yMin,16+text.resolvedStyle.marginTop);
        Near(box.worldBound.yMax-text.worldBound.yMax,16+text.resolvedStyle.marginBottom);
        Near(box.layout.height,text.layout.height+32+text.resolvedStyle.marginTop+text.resolvedStyle.marginBottom);
    }
}
