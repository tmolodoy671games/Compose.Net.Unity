using System;
using Compose.Net;
using static Compose.Net.ComposeFunctions;
using NUnit.Framework;
using UnityEngine;

namespace Compose.Net.Unity.Tests;

public sealed partial class BoxTests
{
    [Test]
    public void ChainedOffsetsUpdateAndDetachIndependently()
    {
        var step = Modifier.Offset(7.Dp(), (-3).Dp());
        Add("item", 50, 20, step.Then(step));
        Add("sibling", 20, 10);
        Mount();
        At("item", 14, -6);
        var item = E("item");
        _items[0] = new Item("item", Modifier.Size(50.Dp(), 20.Dp())
            .Offset((-2).Dp(), 8.Dp()).Then(step));
        Update();
        At("item", 5, 5);
        _items[0] = new Item("item", Modifier.Size(50.Dp(), 20.Dp()).Then(step));
        Update();
        At("item", 7, -3);
        _items[0] = new Item("item", Modifier.Size(50.Dp(), 20.Dp()));
        Update();
        At("item", 0, 0);
        Assert.That(E("item"), Is.SameAs(item));
        Size("box", 50, 20);
        At("sibling", 0, 0);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void ObservedAndStaticOffsetsComposeInEitherOrder(bool observedFirst)
    {
        var source = new OffsetSource();
        var observed = Modifier.Offset(source.Subscribe);
        var offset = Modifier.Offset(12.Dp(), (-3).Dp());
        Add("item", 40, 20, observedFirst ? observed.Then(offset) : offset.Then(observed));
        Mount();
        At("item", 17, 3);
        source.Send(new Offset(-8, 4));
        Flush();
        At("item", 4, 1);
        var queuedNotification = source.Listener!;
        var replacement = new OffsetSource();
        _items[0] = new Item("item", Modifier.Size(40.Dp(), 20.Dp()).Offset(replacement.Subscribe));
        Update();
        Assert.That(source.Disposed, Is.True);
        queuedNotification(new Offset(99, 99));
        Flush();
        At("item", 5, 6);
        replacement.Send(Offset.Zero);
        Flush();
        At("item", 0, 0);
    }

    [TestCase(true, 20)]
    [TestCase(false, 10)]
    public void OuterScaleChangesInnerOffsetDistance(bool scaleFirst, float expected)
    {
        var scale = Modifier.Scale(2);
        var offset = Modifier.Offset(10.Dp());
        Add("item", 40, 20, scaleFirst ? scale.Then(offset) : offset.Then(scale));
        Mount();
        Near(E("item").worldBound.center.x - E("box").worldBound.x - 20, expected);
        Size("box", 40, 20);
    }

    [Test]
    public void BackgroundBetweenOffsetsStaysAtItsOwnPosition()
    {
        Add("item", 40, 20, Modifier.Offset(10.Dp(), 8.Dp())
            .Background(Compose.Net.Color.Orange).Offset(70.Dp(), (-3).Dp()));
        Mount();
        var element = E("item");
        var background = element;
        while (background != null && background.resolvedStyle.backgroundColor.a == 0)
            background = background.parent;
        Assert.That(background, Is.Not.Null);
        Near(background!.worldBound.x - E("box").worldBound.x, 10);
        Near(background.worldBound.y - E("box").worldBound.y, 8);
        Near(background.worldBound.width, 40);
        At("item", 80, 5);
    }

    [TestCase(true, 10)]
    [TestCase(false, 90)]
    public void ClickableUsesBoundsAtItsPositionInOffsetChain(bool clickableFirst, float hitX)
    {
        var click = Modifier.Clickable(() => _clicked++);
        var offset = Modifier.Offset(80.Dp());
        Add("item", 40, 20, clickableFirst ? click.Then(offset) : offset.Then(click));
        Mount();
        var origin = (Vector2)E("box").worldBound.position;
        simulate.Click(origin + new Vector2(hitX, 10));
        Flush();
        Assert.That(_clicked, Is.EqualTo(1));
        simulate.Click(origin + new Vector2(100 - hitX, 10));
        Flush();
        Assert.That(_clicked, Is.EqualTo(1));
    }

    [Test]
    public void ScalesAndAlphaComposeThroughZero()
    {
        IModifier Effects(float scale, float alpha) => Modifier.Size(40.Dp(), 20.Dp())
            .Scale(scale, 2).Scale(3, .5f).Alpha(alpha).Alpha(.5f);
        _items.Add(new Item("item", Effects(0, 0)));
        Mount();
        Near(E("item").worldBound.width, 0);
        _items[0] = new Item("item", Effects(2, .4f));
        Update();
        Near(E("item").worldBound.width, 240);
        Near(E("item").worldBound.height, 20);
        var opacity = 1f;
        for (var element = E("item"); element != E("box"); element = element.parent)
            opacity *= element.resolvedStyle.opacity;
        Assert.That(opacity, Is.EqualTo(.2f).Within(.001f));
    }

    [Test]
    public void AdjacentOffsetsRoundIndividuallyAndFollowInheritedDirection()
    {
        rootVisualElement.languageDirection = UnityEngine.UIElements.LanguageDirection.RTL;
        Add("item", 40, 20, Modifier.Offset(.5f.Dp(), (-1.5f).Dp()).Offset(.5f.Dp(), 1.5f.Dp()));
        try
        {
            Mount();
            At("item", -2, 1);
            Assert.That(E("item").parent, Is.SameAs(E("box")));
        }
        finally { rootVisualElement.languageDirection = UnityEngine.UIElements.LanguageDirection.LTR; }
    }

    [Test]
    public void RemovingOffsetsRestoresAuthoredPercentageAndDepth()
    {
        Add("item", 40, 20);
        Mount();
        var view = E("item");
        var original = new UnityEngine.UIElements.Translate(UnityEngine.UIElements.Length.Percent(50), 4, 3);
        view.style.translate = original;
        _items[0] = new Item("item", Modifier.Size(40.Dp(), 20.Dp()).Offset(7.Dp(), 2.Dp()));
        Update();
        At("item", 27, 6);
        _items[0] = new Item("item", Modifier.Size(80.Dp(), 20.Dp()).Offset(7.Dp(), 2.Dp()));
        Update();
        At("item", 47, 6);
        _items[0] = new Item("item", Modifier.Size(80.Dp(), 20.Dp()));
        Update();
        Assert.That(view.style.translate.value, Is.EqualTo(original));
    }

    [Test]
    public void RotatedOffsetUsesTheRotatedAxes()
    {
        Add("item", 40, 20, Modifier.Rotate(90).Offset(10.Dp()));
        Mount();
        var center = E("item").worldBound.center - E("box").worldBound.center;
        Near(center.x, 0);
        Near(center.y, 10);
    }

    [Test, BoxScope]
    public void ChangingLayersPreservesFractionalWidthAndParentAlignment()
    {
        _box = Modifier.Size(200.Dp(), 100.Dp());
        var alignment = Modifier.Align(Alignment.BottomEnd);
        IModifier Content(bool layered)
        {
            var modifier = layered ? Modifier.Background(Compose.Net.Color.Gray).Offset(10.Dp()) : Modifier;
            return modifier.FillMaxWidth(.5f).Height(20.Dp()).Then(alignment);
        }
        _items.Add(new Item("item", Content(true)));
        Mount();
        Size("item", 100, 20);
        At("item", 110, 80);
        _items[0] = new Item("item", Content(false));
        Update();
        Size("item", 100, 20);
        At("item", 100, 80);
        _items[0] = new Item("item", Content(true));
        Update();
        Size("item", 100, 20);
        At("item", 110, 80);
    }

    [Test]
    public void OuterSizeConstrainsRepeatedInnerSizes()
    {
        _items.Add(new Item("item", Modifier.Size(80.Dp(), 40.Dp())
            .Width(30.Dp()).Height(15.Dp())));
        Mount();
        Size("box", 80, 40);
        Size("item", 80, 40);
        _items[0] = new Item("item", Modifier.Width(60.Dp()).Width(20.Dp()).Height(10.Dp()));
        Update();
        Size("item", 60, 10);
    }

    private IModifier _sizePrefix = null!;

    [Composable]
    private void ChangingSizeSuffix() =>
        Box(_revision.Value == 0 ? _sizePrefix.Height(20.Dp()) : _sizePrefix.Width(30.Dp()));

    [Test]
    public void ReusedPrefixReevaluatesLayersWhenTheLastSizeChangesAxis()
    {
        _sizePrefix = Modifier.TestTag("size-suffix").Width(80.Dp());
        _composer = IComposer.Create();
        Bootstrap(_composer, _adapter, false, new ComposeConfig(Logging: false), new Root(rootVisualElement), ChangingSizeSuffix);
        Flush();
        Near(E("size-suffix").layout.width, 80);
        Update();
        Near(E("size-suffix").layout.width, 80);
    }

    [Test]
    public void GlobalPositionIncludesOffsetsOnBothSidesOfTheCallback()
    {
        Offset? position = null;
        Add("item", 40, 20, Modifier.Offset(10.Dp()).OnGloballyPositioned(c =>
            position = c.ToRelativeLayoutBounds().TopLeft).Offset(20.Dp()));
        Mount();
        for (var i = 0; i < 4; i++) simulate.FrameUpdateMs(16);
        Assert.That(position.HasValue, Is.True);
        Near(position!.Value.X, E("item").worldBound.x);
        At("item", 30, 0);
    }

    [Test]
    public void TranslucentBackgroundKeepsTheColorUnderneath()
    {
        var outer = Compose.Net.Color.Orange;
        var inner = new Compose.Net.Color(0, 0, 1, .5f);
        Add("item", 40, 20, Modifier.Background(outer).Background(inner));
        Mount();
        var view = E("item");
        Assert.That(view.resolvedStyle.backgroundColor.a, Is.EqualTo(.5f).Within(.01f));
        Assert.That(view.parent.resolvedStyle.backgroundColor.r, Is.EqualTo(1f).Within(.01f));
        Near(view.parent.worldBound.width, 40);
    }

    [Test]
    public void FractionalZIndicesAddBeforeNativeRounding()
    {
        Add("item", 40, 20, Modifier.ZIndex(.4f).ZIndex(.4f));
        Mount();
        Assert.That(E("item").style.zIndex.value, Is.EqualTo(1));
        _items[0] = new Item("item", Modifier.Size(40.Dp(), 20.Dp()).ZIndex(2.4f).ZIndex(.4f));
        Update();
        Assert.That(E("item").style.zIndex.value, Is.EqualTo(3));
        _items[0] = new Item("item", Modifier.Size(40.Dp(), 20.Dp()));
        Update();
        Assert.That(E("item").style.zIndex.keyword, Is.EqualTo(UnityEngine.UIElements.StyleKeyword.Null));
    }

    private sealed class OffsetSource : IDisposable
    {
        internal Action<Offset>? Listener { get; private set; }
        internal bool Disposed { get; private set; }
        internal IDisposable Subscribe(Action<Offset> listener)
        {
            Listener = listener;
            listener(new Offset(5, 6));
            return this;
        }
        internal void Send(Offset offset) => Listener?.Invoke(offset);
        public void Dispose() { Disposed = true; Listener = null; }
    }
}
