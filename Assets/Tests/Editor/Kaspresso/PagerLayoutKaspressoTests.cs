using Compose.Net;
using NUnit.Framework;
using UnityEngine.UIElements;
using static Compose.Net.ComposeFunctions;

public partial class PagerLayoutKaspressoTests
{
    private bool _horizontal, _reverse, _fixed;
    private IPagerState _state;

    [TestCase(false, false, false)]
    [TestCase(false, true, false)]
    [TestCase(true, false, false)]
    [TestCase(true, true, false)]
    [TestCase(false, false, true)]
    [TestCase(false, true, true)]
    [TestCase(true, false, true)]
    [TestCase(true, true, true)]
    public void PagesHaveTheirFinalSizeInTheFirstLayout(bool horizontal, bool reverse, bool fixedSize)
    {
        _horizontal = horizontal; _reverse = reverse; _fixed = fixedSize;
        using var driver = new UnityComposeTestDriver();
        driver.SetContentForFirstLayout(Content);
        var viewport = driver.Root.Q<VisualElement>("pager");
        var page = driver.Root.Q<VisualElement>("page-1").parent;
        var size = fixedSize ? 100 : horizontal ? 260 : 160;
        Assert.That(horizontal ? page.layout.width : page.layout.height, Is.EqualTo(size).Within(1));
        Assert.That(_state.Value, Is.EqualTo((size + 10) * 1.25f).Within(1));
        Assert.That(_state.CurrentPage, Is.EqualTo(1));
        Assert.That(_state.CurrentPageOffsetFraction, Is.EqualTo(.25f).Within(.01f));
        Assert.That(viewport.layout.width, Is.EqualTo(300).Within(1));
        Assert.That(viewport.layout.height, Is.EqualTo(200).Within(1));
    }

    [Composable]
    private void Content()
    {
        _state = RememberPagerState(() => 3, initialPage: 1, initialPageOffsetFraction: .25f);
        if (_horizontal)
            HorizontalPager(_state, Page, pageSpacing: 10.Dp(), contentPadding: new PaddingValues(20.Dp()),
                pageSize: _fixed ? new PageSize.Fixed(100.Dp()) : PageSize.Fill,
                reverseLayout: _reverse, modifier: Modifier.Width(300.Dp()).Height(200.Dp()).TestTag("pager"));
        else
            VerticalPager(_state, Page, pageSpacing: 10.Dp(), contentPadding: new PaddingValues(20.Dp()),
                pageSize: _fixed ? new PageSize.Fixed(100.Dp()) : PageSize.Fill,
                reverseLayout: _reverse, modifier: Modifier.Width(300.Dp()).Height(200.Dp()).TestTag("pager"));
    }

    [Composable]
    private void Page(int page) => Box(() => { }, modifier: Modifier.FillMaxSize().TestTag("page-" + page));
}
