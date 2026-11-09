using Compose.Net;
using static Compose.Net.ComposeFunctions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Tests;

public sealed partial class BoxTests
{
    [TestCase(1f), TestCase(.5f), TestCase(.8f)]
    public void CompositionRootKeepsLogicalConstraintsInsideSmallerHost(float scale)
    {
        var host = new VisualElement();
        host.style.width = 660; host.style.height = 500;
        rootVisualElement.Add(host);
        var canvas = new VisualElement();
        canvas.style.position = Position.Absolute;
        canvas.style.width = 1320; canvas.style.height = 1000;
        canvas.style.scale = new Scale(new Vector3(scale, scale, 1));
        canvas.style.transformOrigin = new UnityEngine.UIElements.TransformOrigin(0, 0);
        host.Add(canvas);
        _ = new Root(canvas);
        var box = new Compose.Net.Unity.Box();
        box.style.width = Length.Percent(100); box.style.height = Length.Percent(100);
        canvas.Add(box);
        var child = new VisualElement();
        child.style.width = Length.Percent(100); child.style.height = Length.Percent(100);
        box.Add(child); box.Track(child); Flush();

        Near(child.layout.width, 1320); Near(child.layout.height, 1000);
        Near(child.worldBound.width, 1320 * scale); Near(child.worldBound.height, 1000 * scale);
        host.style.width = 400; host.style.height = 300; Flush();
        Near(child.layout.width, 1320); Near(child.layout.height, 1000);
        canvas.style.width = 1100; canvas.style.height = 800; Flush();
        Near(child.layout.width, 1100); Near(child.layout.height, 800);
    }

    [TestCase(0,0)][TestCase(1,0)][TestCase(2,0)][TestCase(0,1)][TestCase(1,1)][TestCase(2,1)][TestCase(0,2)][TestCase(1,2)][TestCase(2,2)]
    [BoxScope] public void AllNinePerChildAlignments(int x,int y)
    {
        _alignment=Alignment.BottomEnd;_box=Modifier.Size(200.Dp(),140.Dp());
        Add("a",40,20,Modifier.Align(Alignments[y*3+x]));Mount();At("a",x*80,y*60);
    }
    [Test,BoxScope] public void RepeatedModifierInstanceRetainsParentDataAfterOneOccurrenceIsRemoved()
    {
        _box=Modifier.Size(200.Dp(),100.Dp());var alignment=Modifier.Align(Alignment.BottomEnd);
        _items.Add(new Item("a",Modifier.Size(40.Dp(),20.Dp()).Then(alignment).Then(alignment)));Mount();At("a",160,80);
        _items[0]=new Item("a",Modifier.Size(40.Dp(),20.Dp()).Then(alignment));Update();At("a",160,80);
    }
    [Test,BoxScope] public void DuplicateMatchParentModifiersRemainMatchingWhenOneIsRemoved()
    {
        Add("normal",120,80);Add("match",30,20,Modifier.MatchParentSize().MatchParentSize());Mount();Size("match",120,80);
        _items[1]=new Item("match",Modifier.Size(30.Dp(),20.Dp()).MatchParentSize());Update();Size("match",120,80);
        _items[1]=new Item("match",Modifier.Size(30.Dp(),20.Dp()));Update();Size("match",30,20);
    }
    [Test,BoxScope] public void FillFractionCanChangeAndBeRemovedWithoutStaleBounds()
    {
        _box=Modifier.SizeIn(maxWidth:240.Dp(),maxHeight:160.Dp());
        _items.Add(new Item("a",Modifier.FillMaxSize(.5f)));Mount();Size("a",120,80);
        _items[0]=new Item("a",Modifier.FillMaxSize(.75f));Update();Size("a",180,120);
        _items[0]=new Item("a",Modifier.Size(40.Dp(),30.Dp()));Update();Size("a",40,30);Size("box",40,30);
    }
    [Test,BoxScope] public void PropagatedMinimumWinsOverFractionalFill()
    {
        _box=Modifier.Size(200.Dp(),100.Dp());_propagate=true;_items.Add(new Item("a",Modifier.FillMaxSize(.5f)));Mount();Size("a",200,100);
        _propagate=false;Update();Size("a",100,50);
    }
    [Test,BoxScope] public void IncreasingMaximumRemeasuresEvenWhenParentHasNotGrownYet()
    {
        _box=Modifier.SizeIn(maxWidth:80.Dp(),maxHeight:50.Dp());Add("a",200,120);Mount();Size("a",80,50);
        _box=Modifier.SizeIn(maxWidth:160.Dp(),maxHeight:90.Dp());Update();Size("a",160,90);Size("box",160,90);
    }
    [Test,BoxScope] public void AuthoredSizeChangesWhileClampedSurviveConstraintRemoval()
    {
        _box=Modifier.SizeIn(maxWidth:80.Dp(),maxHeight:50.Dp());Add("a",200,120);Mount();
        _items[0]=new Item("a",Modifier.Size(150.Dp(),90.Dp()));Update();Size("a",80,50);
        _box=Modifier;Update();Size("a",150,90);Size("box",150,90);
    }
    [Test,BoxScope] public void ZeroMaximumClampsOrdinaryAndMatchingChildren()
    {
        _box=Modifier.SizeIn(maxWidth:0.Dp(),maxHeight:0.Dp());Add("a",100,100);Add("match",100,100,Modifier.MatchParentSize());Mount();
        Size("box",0,0);Size("a",0,0);Size("match",0,0);
    }
    [Test,BoxScope] public void FractionalUnitsDoNotAccumulateStackOffsets()
    {
        _box=Modifier.Size(100.5f.Dp(),80.5f.Dp());_alignment=Alignment.Center;
        Add("a",30.5f,20.5f);Add("b",50.5f,10.5f);Mount();At("a",35,30);At("b",25,35);
        for(var i=0;i<15;i++)Update();At("a",35,30);At("b",25,35);
    }
    [Test,BoxScope] public void NativeZIndexOverridesCompositionOrderForPicking()
    {
        Add("a",100,60,Modifier.Background(Color.Orange).ZIndex(5).Clickable(()=>_clicked=1));
        Add("b",100,60,Modifier.Background(Color.DodgerBlue).Clickable(()=>_clicked=2));Mount();
        simulate.Click(E("box").worldBound.center);Flush();Assert.That(_clicked,Is.EqualTo(1));
    }
    [Test,BoxScope] public void MatchingOverlayUsesNativePicking()
    {
        Add("base",100,60,Modifier.Background(Color.Orange).Clickable(()=>_clicked=1));
        Add("overlay",1,1,Modifier.MatchParentSize().Background(Color.DodgerBlue).Clickable(()=>_clicked=2));Mount();
        simulate.Click(E("box").worldBound.center);Flush();Assert.That(_clicked,Is.EqualTo(2));
    }
    [Test,BoxScope] public void StableBoxDoesNotScheduleGeometryWorkEveryFrame()
    {
        _box=Modifier.Size(200.Dp(),120.Dp());Add("a",80,50);Add("b",30,20,Modifier.MatchParentSize());Mount();
        var events=0;E("box").RegisterCallback<GeometryChangedEvent>(_=>events++);
        for(var i=0;i<30;i++)simulate.FrameUpdateMs(16);
        Assert.That(events,Is.EqualTo(0));
    }
    [Test,BoxScope] public void RemovingSizeInRestoresAuthoredBoundsAcrossParentResize()
    {
        _box=Modifier.Size(100.Dp(),80.Dp());
        _items.Add(new Item("a",Modifier.FillMaxSize().SizeIn(maxWidth:70.Dp())));Mount();
        _items[0]=new Item("a",Modifier.FillMaxSize());Update();Size("a",100,80);
        _box=Modifier.Size(240.Dp(),140.Dp());Update();Size("a",240,140);
    }
    [Test,BoxScope] public void SizeInUpdatesAtAnEqualEffectiveLimitAreNotLost()
    {
        _box=Modifier.SizeIn(maxWidth:80.Dp());
        _items.Add(new Item("a",Modifier.Size(200.Dp(),40.Dp()).SizeIn(maxWidth:140.Dp())));Mount();Size("a",80,40);
        _items[0]=new Item("a",Modifier.Size(200.Dp(),40.Dp()).SizeIn(maxWidth:80.Dp()));Update();
        _box=Modifier.SizeIn(maxWidth:180.Dp());Update();Size("a",80,40);
    }
    [TestCase(false), TestCase(true)] public void FixedBoxInsideNativeScrollStillConstrainsItsOwnChildren(bool horizontal)
    {
        var scroll=new ScrollView(horizontal ? ScrollViewMode.Horizontal : ScrollViewMode.Vertical);
        scroll.style.width=300;scroll.style.height=200;rootVisualElement.Add(scroll);
        var box=new Compose.Net.Unity.Box();box.style.width=200;box.style.height=100;scroll.Add(box);
        var child=new VisualElement();child.style.width=400;child.style.height=300;box.Add(child);box.Track(child);Flush();
        Near(child.layout.width,200);Near(child.layout.height,100);
    }
    [Test,BoxScope] public void PropagatedMinimumDoesNotFreezeAnimatedContentSizeWhenShrinking()
    {
        _box=Modifier.SizeIn(minWidth:80.Dp(),minHeight:50.Dp()).AnimateContentSize(Tween(0));_propagate=true;
        Add("a",160,100);Mount();Size("box",160,100);
        _items[0]=new Item("a",Modifier.Size(30.Dp(),20.Dp()));Update();Size("box",80,50);Size("a",80,50);
    }
    [Test] public void ReparentingRestoresOldGridStylesAndAppliesNewAlignment()
    {
        var first=new Compose.Net.Unity.Box {ContentAlignment=Alignment.TopStart};first.style.width=200;first.style.height=100;
        var second=new Compose.Net.Unity.Box {ContentAlignment=Alignment.BottomEnd};second.style.width=150;second.style.height=80;
        rootVisualElement.Add(first);rootVisualElement.Add(second);
        var child=new VisualElement();child.style.width=40;child.style.height=20;first.Add(child);first.Track(child);Flush();
        first.Untrack(child);child.RemoveFromHierarchy();second.Add(child);second.Track(child);Flush();
        Near(child.layout.x,110);Near(child.layout.y,60);
        second.Untrack(child);Assert.That(child.style.alignSelf.keyword,Is.EqualTo(StyleKeyword.Null));
    }
}
