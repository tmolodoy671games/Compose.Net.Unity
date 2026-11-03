using System.Linq;
using Compose.Net;
using Compose.Net.Unity.Samples.Behaviors.Content;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using static Compose.Net.ComposeFunctions;

namespace Compose.Net.Unity.Samples.Tests;

public sealed partial class SharedLayoutDemoTests
{
    private void MountNavigationShared(int width = 800, int height = 600)
    {
        _composer.Dispose(); Flush(); panelSize = new Vector2(width, height);
        _composer = IComposer.Create();
        Bootstrap(_composer, _adapter, false, new ComposeConfig(Logging: false),
            new RootNode(rootVisualElement), NavigationSharedTransitionSample.Layout);
        Flush();
    }

    private void NavigationClick(string tag)
    {
        var element = E("nav-shared." + tag);
        simulate.MouseDown(element); Flush(); simulate.MouseUp(element); Flush();
    }

    [TestCase(800, 600, 1f)][TestCase(1200, 800, 1.25f)][TestCase(600, 900, 2f)]
    public void NavigationSharedSettingsReturnKeepsContentVisibleAndBoundsContinuous(int width, int height, float scale)
    {
        pixelsPerPoint = scale; MountNavigationShared(width, height);
        var buttonBounds = E("nav-shared.button.screen:Settings").worldBound;
        NavigationClick("button.screen:Settings"); Flush(500);
        var screen = E("nav-shared.screen.screen:Settings");
        var screenBounds = screen.worldBound;
        NavigationClick("settings.theme");
        var theme = E("nav-shared.settings.theme");
        Assert.That(((TextElement)theme).text, Is.EqualTo("Тема: светлая"));
        NavigationClick("back.screen:Settings");
        var button = E("nav-shared.button.screen:Settings");
        var title = E("nav-shared.screen-title.title:Settings");
        var incomingTitle = E("nav-shared.button-title.title:Settings");
        Assert.That(theme.panel, Is.Not.Null, "Popped settings remain alive during their exit");
        Near(Alpha(theme), 1, .001f);
        Near(Alpha(button), 0, .001f);
        Near(Alpha(title), 1, .001f);
        Near(Alpha(incomingTitle), 0, .001f);
        var elapsed = 0;
        foreach (var time in new[] { 0, 1, 16, 100, 250, 400, 484, 499 })
        {
            Flush(time - elapsed); elapsed = time;
            var progress = EaseInOutEasing.Transform(time / 500f);
            var bounds = Interpolate(screenBounds, buttonBounds, progress);
            AssertBoundsMatchDrawing(screen, bounds, "Outgoing screen at " + time);
            AssertBoundsMatchDrawing(button, bounds, "Returning button at " + time);
            Assert.That(rootVisualElement.Q("nav-shared.settings.theme"), Is.SameAs(theme));
            Assert.That(((TextElement)theme).text, Is.EqualTo("Тема: светлая"));
            Near(Alpha(theme), 1 - progress, .01f);
            Near(Alpha(button), progress, .01f);
            RectNear(Layer(title)!.worldBound, Layer(incomingTitle)!.worldBound);
        }
        var before = button.worldBound; var titleBefore = incomingTitle.worldBound;
        Flush(1);
        RectNear(button.worldBound, before); RectNear(button.worldBound, buttonBounds);
        RectNear(incomingTitle.worldBound, titleBefore);
        Assert.That(rootVisualElement.Q("nav-shared.settings.theme"), Is.Null);
        Assert.That(rootVisualElement.Query<VisualElement>("shared-transition-placeholder").ToList(), Is.Empty);
        NavigationClick("button.screen:Settings"); Flush(500);
        Assert.That(((TextElement)E("nav-shared.settings.theme")).text, Is.EqualTo("Тема: тёмная"));
    }

    [TestCase("Settings")][TestCase("ProductList")]
    public void NavigationSharedOpeningHasNoFirstOrLastFrameJump(string route)
    {
        MountNavigationShared();
        var source = E("nav-shared.button.screen:" + route);
        var start = source.worldBound;
        NavigationClick("button.screen:" + route);
        var target = E("nav-shared.screen.screen:" + route);
        var end = new UnityEngine.Rect(Vector2.zero, new Vector2(800, 600));
        Near(Alpha(target), 0, .001f); Near(Alpha(source), 1, .001f);
        var elapsed = 0;
        foreach (var time in new[] { 0, 16, 100, 250, 400, 499 })
        {
            Flush(time-elapsed); elapsed = time;
            var bounds = Interpolate(start, end, EaseInOutEasing.Transform(time/500f));
            AssertBoundsMatchDrawing(target, bounds, "Incoming screen at " + time);
            AssertBoundsMatchDrawing(source, bounds, "Outgoing button at " + time);
        }
        var before = target.worldBound; Flush(1);
        RectNear(target.worldBound, before); RectNear(target.worldBound, end);
    }

    [TestCase(1)][TestCase(250)][TestCase(499)]
    public void NavigationSharedBackDuringOpeningReversesFromTheRenderedBounds(int reverseAt)
    {
        MountNavigationShared();
        var homeBounds = E("nav-shared.button.screen:Settings").worldBound;
        NavigationClick("button.screen:Settings"); Flush(reverseAt);
        var screen = E("nav-shared.screen.screen:Settings");
        var before = screen.worldBound; var opacity = Alpha(screen);
        NavigationClick("back.screen:Settings");
        var button = E("nav-shared.button.screen:Settings");
        RectNear(screen.worldBound, before); RectNear(button.worldBound, before);
        Near(Alpha(screen), opacity, .001f);
        Flush(499); before = button.worldBound; Flush(1);
        RectNear(button.worldBound, before); RectNear(button.worldBound, homeBounds);
        Assert.That(rootVisualElement.Q("nav-shared.settings.theme"), Is.Null);
        Assert.That(rootVisualElement.Query<VisualElement>("shared-transition-layer").ToList(), Is.Empty);
    }

    [TestCase("compose")][TestCase("nav3")][TestCase("uitk")]
    public void NavigationSharedProductsReturnToTheSameCardWithoutSnapping(string product)
    {
        MountNavigationShared();
        NavigationClick("button.screen:ProductList"); Flush(500);
        var key = "screen:product:" + product;
        var origin = E("nav-shared.button." + key).worldBound;
        NavigationClick("button." + key); Flush(500);
        var screen = E("nav-shared.screen." + key);
        var start = screen.worldBound;
        NavigationClick("back." + key);
        var card = E("nav-shared.button." + key);
        Near(Alpha(card), 0, .001f); Near(Alpha(screen), 1, .001f);
        RectNear(card.worldBound, start);
        Flush(250);
        var midpoint = Interpolate(start, origin, .5f);
        AssertBoundsMatchDrawing(card, midpoint, "Returning product card");
        AssertBoundsMatchDrawing(screen, midpoint, "Outgoing product screen");
        Flush(249); var before = card.worldBound; Flush(1);
        RectNear(card.worldBound, before); RectNear(card.worldBound, origin);
        Assert.That(rootVisualElement.Query<VisualElement>("shared-transition-layer").ToList(), Is.Empty);
        NavigationClick("back.screen:ProductList"); Flush(500);
        Assert.That(rootVisualElement.Q("nav-shared.home"), Is.Not.Null);
    }
}
