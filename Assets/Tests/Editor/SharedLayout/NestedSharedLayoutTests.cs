using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Compose.Net;
using static Compose.Net.ComposeFunctions;
using Compose.Net.Unity.Samples.Behaviors.Content;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Samples.Tests;

public sealed partial class SharedLayoutDemoTests
{
    private IMutableState<bool> _expanded = null!;
    private bool _remeasure, _nestedElement, _threeLevels;
    private readonly SharedContentState[] _parents = new SharedContentState[2], _children = new SharedContentState[2], _middle = new SharedContentState[2];
    private SharedTransitionScope _nestedScope = null!;
    private IMutableState<int> _sampleSelection = null!;
    private IndicationProbe _indication = null!;
    private bool _defaultIndication;

    private void MountNested(bool remeasure, bool expanded = false, bool element = false, bool threeLevels = false)
    {
        _composer.Dispose(); Flush(); panelSize = new Vector2(800,600);
        _remeasure = remeasure; _nestedElement = element; _threeLevels = threeLevels;
        _expanded = MutableStateOf(expanded); _composer = IComposer.Create();
        Bootstrap(_composer,_adapter,false,new ComposeConfig(Logging:false),new RootNode(rootVisualElement),NestedContent); Flush();
    }
    [Composable] private void NestedContent()
    {
        SharedTransitionLayout(shared =>
        {
            _nestedScope = shared;
            AnimatedContent(_expanded.Value, (visibility, expanded) =>
            {
                var id=expanded?1:0;
                var parent=shared.RememberSharedContentState("parent"); _parents[id]=parent;
                Box(content: () =>
                {
                    if (_threeLevels)
                    {
                        var middle=shared.RememberSharedContentState("middle"); _middle[id]=middle;
                        Box(content:()=>NestedText(shared,visibility,expanded),
                            contentAlignment:expanded?Alignment.BottomEnd:Alignment.Center,
                            modifier:Modifier.Size((expanded?240:80).Dp(),(expanded?160:60).Dp())
                                .SharedBounds(middle,visibility,boundsTransform:(_,_)=>Tween(1000,easing:LinearEasing),
                                    resizeMode:_remeasure?ResizeMode.RemeasureToBounds:ResizeMode.ScaleToBounds()));
                    }
                    else NestedText(shared,visibility,expanded);
                }, contentAlignment:expanded?Alignment.BottomEnd:Alignment.Center,
                modifier:Modifier.Size((expanded?360:120).Dp(),(expanded?240:100).Dp()).TestTag("nested.parent."+id)
                    .SharedBounds(parent,visibility,boundsTransform:(_,_)=>Tween(1000,easing:LinearEasing),
                        resizeMode:_remeasure?ResizeMode.RemeasureToBounds:ResizeMode.ScaleToBounds()));
            }, modifier:Modifier.FillMaxSize(),contentAlignment:Alignment.Center,
                transitionSpec:_=>FadeIn(Tween(100)).TogetherWith(FadeOut(Tween(100))));
        }, Modifier.FillMaxSize());
    }
    [Composable] private void NestedText(SharedTransitionScope shared, AnimatedVisibilityScope visibility, bool expanded)
    {
        var id=expanded?1:0;
        var child=shared.RememberSharedContentState("child"); _children[id]=child;
        var modifier=Modifier.TestTag("nested.child."+id);
        modifier=_nestedElement ? modifier.SharedElement(child,visibility,(_,_)=>Tween(1000,easing:LinearEasing))
            : modifier.SharedBounds(child,visibility,boundsTransform:(_,_)=>Tween(1000,easing:LinearEasing),
                enter:FadeIn(Tween(1000,easing:LinearEasing)),exit:FadeOut(Tween(1000,easing:LinearEasing)));
        Text("#1", fontSize:(expanded?128:48).Sp(), modifier:modifier);
    }
    private static void RectNear(UnityEngine.Rect actual,UnityEngine.Rect expected)
    {
        // Native world bounds are rounded to physical pixels, including on reversal.
        Near(actual.x,expected.x,1.01f); Near(actual.y,expected.y,1.01f); Near(actual.width,expected.width,1.01f); Near(actual.height,expected.height,1.01f);
    }
    private static UnityEngine.Rect Interpolate(UnityEngine.Rect a,UnityEngine.Rect b,float progress) =>
        new(Vector2.Lerp(a.position,b.position,progress),Vector2.Lerp(a.size,b.size,progress));
    private UnityEngine.Rect NestedBounds(int id) => (Layer(E("nested.child."+id)) ?? E("nested.child."+id)).worldBound;

    [TestCase(false,false)][TestCase(false,true)][TestCase(true,false)][TestCase(true,true)]
    public void NestedContentFollowsOneTrajectoryForwardAndBack(bool remeasure,bool element)
    {
        MountNested(remeasure,true,element); var end=NestedBounds(1);
        MountNested(remeasure,false,element); var start=NestedBounds(0);
        for(var direction=0;direction<2;direction++)
        {
            var target=direction==0?1:0; var source=1-target;
            var from=direction==0?start:end; var to=direction==0?end:start;
            _expanded.Value=target==1; Flush();
            for(var step=0;step<4;step++)
            {
                if(step>0) Flush(250);
                Assert.That(_children[target].IsMatchFound,Is.True);
                Assert.That(_children[target].ParentSharedContentState,Is.SameAs(_parents[target]));
                RectNear(NestedBounds(target),Interpolate(from,to,step/4f));
                RectNear(NestedBounds(source),NestedBounds(target));
                if(element) Assert.That(E("nested.child."+source).resolvedStyle.visibility,Is.EqualTo(Visibility.Hidden));
                else Near(Alpha(E("nested.child.0"))+Alpha(E("nested.child.1")),1,.01f);
            }
            Flush(250);
            Assert.That(_nestedScope.IsTransitionActive,Is.False);
            RectNear(NestedBounds(target),to);
            Assert.That(rootVisualElement.Query<VisualElement>("shared-transition-placeholder").ToList(),Is.Empty);
        }
    }
    [TestCase(false)][TestCase(true)] public void ThreeNestedMatchesKeepIndependentRootCoordinates(bool remeasure)
    {
        MountNested(remeasure,true,threeLevels:true); var end=NestedBounds(1);
        MountNested(remeasure,threeLevels:true); var start=NestedBounds(0);
        _expanded.Value=true; Flush(); Flush(500);
        Assert.That(_children[1].ParentSharedContentState,Is.SameAs(_middle[1]));
        Assert.That(_middle[1].ParentSharedContentState,Is.SameAs(_parents[1]));
        RectNear(NestedBounds(1),Interpolate(start,end,.5f));
        Flush(500); Assert.That(_nestedScope.IsTransitionActive,Is.False); RectNear(NestedBounds(1),end);
    }
    [TestCase(false)][TestCase(true)] public void NestedReversalKeepsBoundsAndOpacityContinuous(bool remeasure)
    {
        MountNested(remeasure); var start=NestedBounds(0);
        _expanded.Value=true; Flush(); Flush(400); var before=NestedBounds(1);
        var a=Alpha(E("nested.child.0")); var b=Alpha(E("nested.child.1"));
        _expanded.Value=false; Flush(); RectNear(NestedBounds(0),before);
        Near(Alpha(E("nested.child.0")),a,.01f); Near(Alpha(E("nested.child.1")),b,.01f);
        Flush(500); RectNear(NestedBounds(0),Interpolate(before,start,.5f));
        Flush(500); Assert.That(_nestedScope.IsTransitionActive,Is.False); RectNear(NestedBounds(0),start);
    }
    [Test] public void DisposingThreeNestedMatchesRemovesEveryOverlay()
    {
        MountNested(true,threeLevels:true); _expanded.Value=true; Flush(); Flush(300);
        _composer.Dispose(); Flush();
        Assert.That(rootVisualElement.Query<VisualElement>("shared-transition-layer").ToList(),Is.Empty);
        Assert.That(rootVisualElement.Query<VisualElement>("shared-transition-placeholder").ToList(),Is.Empty);
        Assert.That(_children[0].IsMatchFound || _children[1].IsMatchFound,Is.False);
    }

    private void MountNestedSample(int selection=-1, bool defaultIndication=false)
    {
        _composer.Dispose(); Flush(); panelSize=new Vector2(800,600);
        _defaultIndication=defaultIndication; _sampleSelection=MutableStateOf(selection); _indication=new IndicationProbe(); _composer=IComposer.Create();
        Bootstrap(_composer,_adapter,false,new ComposeConfig(Logging:false),new RootNode(rootVisualElement),NestedSampleContent); Flush();
    }
    [Composable] private void NestedSampleContent()
    {
        if (_defaultIndication) SharedTransitionLayoutSample.Screen(_sampleSelection);
        else CompositionLocalProvider(LocalIndication.Provides<IIndication>(_indication),()=>SharedTransitionLayoutSample.Screen(_sampleSelection));
    }
    private VisualElement SampleText(string page,int index) => E($"shared-sample.text.{page}.{index}");
    private static void AssertOneLine(VisualElement element)
    {
        var text=(TextElement)element;
        var natural=text.MeasureTextSize(text.text,0,VisualElement.MeasureMode.Undefined,0,VisualElement.MeasureMode.Undefined);
        // MeasureTextSize floors to physical pixels. Avoid losing a pixel to float error
        // when a fractional-DPI layout is reparented from a nonzero origin to (0, 0).
        var constrained=text.MeasureTextSize(text.text,text.contentRect.width + .0001f,VisualElement.MeasureMode.AtMost,0,VisualElement.MeasureMode.Undefined);
        Assert.That(text.contentRect.width,Is.GreaterThanOrEqualTo(natural.x-.6f),"First-frame width must fit the complete label");
        Assert.That(constrained.y,Is.EqualTo(natural.y).Within(.6f),"# and number must stay on one line");
    }
    [TestCase(0,1f)][TestCase(1,1f)][TestCase(2,1f)][TestCase(0,1.25f)][TestCase(1,1.5f)][TestCase(2,2f)]
    public void SampleNestedTextMatchesCrossfadesAndNeverWrapsOnFirstFrame(int index,float scale)
    {
        pixelsPerPoint=scale;
        MountNestedSample(index); AssertOneLine(SampleText("detail",index)); var destination=SampleText("detail",index).worldBound;
        MountNestedSample(); var origin=SampleText("thumb",index).worldBound;
        _sampleSelection.Value=index; Flush();
        var outgoing=SampleText("thumb",index); var incoming=SampleText("detail",index);
        AssertOneLine(outgoing); AssertOneLine(incoming);
        RectNear(Layer(incoming)!.worldBound,origin);
        Flush(200);
        RectNear(Layer(incoming)!.worldBound,Layer(outgoing)!.worldBound);
        Assert.That(Alpha(incoming),Is.InRange(.01f,.99f)); Assert.That(Alpha(outgoing),Is.InRange(.01f,.99f));
        Near(Alpha(incoming)+Alpha(outgoing),1,.01f);
        Flush(300); RectNear(incoming.worldBound,destination); AssertOneLine(incoming);
        Assert.That(rootVisualElement.Q($"shared-sample.text.thumb.{index}"),Is.Null);
        _sampleSelection.Value=-1; Flush();
        AssertOneLine(SampleText("thumb",index)); AssertOneLine(SampleText("detail",index));
        Flush(500); RectNear(SampleText("thumb",index).worldBound,origin);
        Assert.That(rootVisualElement.Query<TextElement>().ToList().Count(e=>e.text=="#"+index),Is.EqualTo(1));
    }
    [UnityTest] public IEnumerator SampleCardsProvideHoverPressAndClickIndication()
    {
        MountNestedSample(); Assert.That(_indication.Sources.Count,Is.EqualTo(3),"Cards must use LocalIndication");
        var card=E("shared-sample.thumb.1");
        simulate.MouseMove(card); yield return null; Flush();
        Assert.That(_indication.Hovered,Is.True);
        simulate.MouseDown(card); yield return null; Flush();
        Assert.That(_indication.Pressed,Is.True);
        simulate.MouseUp(card); yield return null; Flush(); Assert.That(_sampleSelection.Value,Is.EqualTo(1));
        Flush(500); card=E("shared-sample.detail.1");
        simulate.MouseDown(card); yield return null; Flush(); simulate.MouseUp(card); yield return null; Flush();
        Assert.That(_sampleSelection.Value,Is.EqualTo(-1)); Flush(500);
    }
    [UnityTest] public IEnumerator DefaultRippleRecompositionDoesNotLoseHeldClicks()
    {
        MountNestedSample(defaultIndication:true);
        for(var index=0;index<3;index++)
        {
            var card=E($"shared-sample.thumb.{index}");
            simulate.MouseMove(card); yield return null; Flush(350);
            simulate.MouseDown(card); yield return null; Flush(150);
            simulate.MouseUp(card); yield return null; Flush();
            Assert.That(_sampleSelection.Value,Is.EqualTo(index),"Hover/press recomposition must preserve the click handler");
            Flush(500); card=E($"shared-sample.detail.{index}");
            simulate.MouseMove(card); yield return null; Flush(350);
            simulate.MouseDown(card); yield return null; Flush(150);
            simulate.MouseUp(card); yield return null; Flush();
            Assert.That(_sampleSelection.Value,Is.EqualTo(-1)); Flush(500);
        }
    }
    [UnityTest] public IEnumerator HeldClickSurvivesTheEndOfSharedTransition()
    {
        MountNestedSample(1); var destination=E("shared-sample.detail.1").worldBound;
        MountNestedSample(defaultIndication:true); _sampleSelection.Value=1; Flush(); Flush(450);
        var card=E("shared-sample.detail.1"); Assert.That(Layer(card),Is.Not.Null);
        simulate.MouseDown(card); yield return null; Flush(100);
        RectNear(card.worldBound,destination);
        simulate.MouseUp(card); yield return null; Flush();
        Assert.That(_sampleSelection.Value,Is.EqualTo(-1),"Finishing the animation must not cancel a held click");
        Flush(500);
        Assert.That(rootVisualElement.Query<VisualElement>("shared-transition-layer").ToList(),Is.Empty);
    }
    [UnityTest] public IEnumerator DisposalCancelsHeldInputAndRemovesDeferredOverlay()
    {
        MountNestedSample(defaultIndication:true); _sampleSelection.Value=1; Flush(); Flush(450);
        var card=E("shared-sample.detail.1"); var point=card.worldBound.center;
        simulate.MouseDown(point); yield return null; Flush(100);
        _composer.Dispose(); Flush(); simulate.MouseUp(point); yield return null; Flush();
        Assert.That(_sampleSelection.Value,Is.EqualTo(1));
        Assert.That(rootVisualElement.Query<VisualElement>("shared-transition-layer").ToList(),Is.Empty);
        Assert.That(rootVisualElement.Query<VisualElement>("shared-transition-placeholder").ToList(),Is.Empty);
    }
    [UnityTest] public IEnumerator RapidReversalsKeepTheEnteringCardClickable()
    {
        MountNestedSample(defaultIndication:true);
        for(var step=0;step<4;step++)
        {
            var detail=_sampleSelection.Value>=0;
            var card=E(detail?"shared-sample.detail.1":"shared-sample.thumb.1");
            simulate.MouseDown(card); yield return null; Flush(); simulate.MouseUp(card); yield return null; Flush();
            Assert.That(_sampleSelection.Value,Is.EqualTo(detail?-1:1),"Click must reach the entering content after reversal");
            Flush(150);
        }
        Flush(500);
    }
    private sealed partial class IndicationProbe : IIndication
    {
        internal readonly Dictionary<IInteractionSource,(bool Hovered,bool Pressed)> Sources=new();
        internal bool Hovered=>Sources.Values.Any(value=>value.Hovered);
        internal bool Pressed=>Sources.Values.Any(value=>value.Pressed);
        [Composable] public IIndicationInstance RememberUpdatedInstance(IInteractionSource source)
        {
            var hovered=source.CollectIsHoveredAsState().Value;
            var pressed=source.CollectIsPressedAsState().Value;
            Sources[source]=(hovered,pressed);
            return new EmptyIndication();
        }
        private sealed class EmptyIndication : IIndicationInstance { public void DrawIndication(IDrawScope scope) { } }
    }
}
