using System;
using System.Linq;
using Compose.Net;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using static Compose.Net.ComposeFunctions;

namespace Compose.Net.Unity.Samples.Tests;

public sealed partial class SharedLayoutDemoTests
{
    private IMutableState<bool> _edgeOverlay=null!;
    private IMutableState<float> _edgeWidth=null!, _edgeScale=null!, _edgeMax=null!;
    private bool _edgeConstrained, _edgeTransformed;
    private IMutableState<bool> _edgePage=null!;
    private IMutableState<float> _edgeTargetWidth=null!;
    private void MountEdgeOverlay(bool constrained=false,bool transformed=false)
    {
        _composer.Dispose(); Flush(); panelSize=new Vector2(800,600);
        _edgeOverlay=MutableStateOf(false);_edgeWidth=MutableStateOf(400f);_edgeScale=MutableStateOf(.8f);_edgeMax=MutableStateOf(200f);
        _edgeConstrained=constrained;_edgeTransformed=transformed;_composer=IComposer.Create();
        Bootstrap(_composer,_adapter,false,new ComposeConfig(Logging:false),new RootNode(rootVisualElement),EdgeOverlayContent);Flush();
    }
    [Composable] private void EdgeOverlayContent() => SharedTransitionScope((shared,rootModifier)=>
        Box(modifier:rootModifier.Size(_edgeWidth.Value.Dp(),300.Dp()).TestTag("edge.parent"),content:()=>
        {
            var modifier=Modifier.Height(60.Dp()).TestTag("edge.child");
            modifier=_edgeConstrained ? modifier.Width(300.Dp()).SizeIn(maxWidth:_edgeMax.Value < 0 ? Dp.Unspecified : _edgeMax.Value.Dp()) : modifier.FillMaxWidth(.5f);
            if(_edgeTransformed) modifier=modifier.Scale(_edgeScale.Value).Offset(20.Dp(),15.Dp());
            Box(modifier:modifier.RenderInSharedTransitionScopeOverlay(shared,renderInOverlay:()=>_edgeOverlay.Value),content:()=>{});
        }));
    [TestCase(1f)][TestCase(1.25f)][TestCase(2f)]
    public void OverlayPercentageSizeFollowsResizedParentWithoutCompletionSnap(float scale)
    {
        pixelsPerPoint=scale;MountEdgeOverlay();_edgeOverlay.Value=true;Flush();
        _edgeWidth.Value=600;Flush();var child=E("edge.child");
        Near(child.worldBound.width,300);var before=child.worldBound;
        _edgeOverlay.Value=false;Flush();RectNear(child.worldBound,before);
    }
    [Test] public void OverlayKeepsAuthoredMaxWidthConstraint()
    {
        MountEdgeOverlay(constrained:true);var child=E("edge.child");var bounds=child.worldBound;
        _edgeOverlay.Value=true;Flush();RectNear(child.worldBound,bounds);Near(child.worldBound.width,200);
        _edgeOverlay.Value=false;Flush();RectNear(child.worldBound,bounds);
    }
    [TestCase(100f,100f)][TestCase(-1f,300f)][TestCase(0f,0f)]
    public void OverlayKeepsConstraintChangesIncludingRemoval(float constraint,float expected)
    {
        MountEdgeOverlay(constrained:true);_edgeOverlay.Value=true;Flush();_edgeMax.Value=constraint;Flush();
        var child=E("edge.child");Near(child.worldBound.width,expected);
        _edgeOverlay.Value=false;Flush();Near(child.worldBound.width,expected);
    }
    [Test] public void OverlayPreservesAuthoredTransformsWhenElevating()
    {
        MountEdgeOverlay(transformed:true);var child=E("edge.child");var bounds=child.worldBound;
        _edgeOverlay.Value=true;Flush();RectNear(child.worldBound,bounds);
        _edgeOverlay.Value=false;Flush();RectNear(child.worldBound,bounds);
    }
    [Test] public void OverlayPreservesTransformChangesOnRestore()
    {
        MountEdgeOverlay(transformed:true);_edgeOverlay.Value=true;Flush();_edgeScale.Value=.6f;Flush();
        var child=E("edge.child");Near(child.worldBound.width,120);var before=child.worldBound;
        _edgeOverlay.Value=false;Flush();RectNear(child.worldBound,before);Near(child.resolvedStyle.scale.value.x,.6f,.001f);
    }
    [Composable] private void EdgeRemeasureContent() => SharedTransitionLayout(shared =>
        AnimatedContent(_edgePage.Value,(visibility,page)=>
            Box(modifier:Modifier.Size((page?_edgeTargetWidth.Value:100).Dp(),80.Dp()).TestTag("edge.card."+page)
                .SharedElement(shared.RememberSharedContentState("card"),visibility,(_,_)=>Tween(1000,easing:LinearEasing)),content:()=>{}),
            transitionSpec:_=>IEnterTransition.None.TogetherWith(IExitTransition.None).Using(null)),Modifier.FillMaxSize());
    [Test] public void TargetSizeEqualToCurrentAnimatedSizeIsNotLostOnRestore()
    {
        _composer.Dispose();Flush();panelSize=new Vector2(800,600);
        _edgePage=MutableStateOf(false);_edgeTargetWidth=MutableStateOf(200f);_composer=IComposer.Create();
        Bootstrap(_composer,_adapter,false,new ComposeConfig(Logging:false),new RootNode(rootVisualElement),EdgeRemeasureContent);Flush();
        _edgePage.Value=true;Flush();Flush(500);Near(E("edge.card.True").worldBound.width,150);
        _edgeTargetWidth.Value=150;Flush();Flush(2000);
        Near(E("edge.card.True").worldBound.width,150);Assert.That(Layer(E("edge.card.True")),Is.Null);
    }
    [TestCase(0,1f,800,600)][TestCase(1,1f,800,600)][TestCase(2,1f,800,600)]
    [TestCase(0,1.25f,800,600)][TestCase(1,1.25f,800,600)][TestCase(2,1.25f,800,600)]
    [TestCase(0,2f,800,600)][TestCase(1,2f,800,600)][TestCase(2,2f,800,600)]
    [TestCase(1,1f,1920,1080)][TestCase(1,1.25f,1200,800)]
    [TestCase(0,1f,400,700)][TestCase(1,1.25f,600,900)][TestCase(2,2f,400,700)]
    public void CompletedSampleOpeningAndReturnUseTheSame500msTrajectory(int index,float scale,int width,int height)
    {
        pixelsPerPoint=scale;MountNestedSample(index);panelSize=new Vector2(width,height);Flush();var expanded=E("shared-sample.detail."+index).worldBound;
        MountNestedSample();panelSize=new Vector2(width,height);Flush();var collapsed=E("shared-sample.thumb."+index).worldBound;
        for(var direction=0;direction<2;direction++)
        {
            _sampleSelection.Value=direction==0?index:-1;Flush();
            var target=E("shared-sample."+(direction==0?"detail.":"thumb.")+index);
            var start=direction==0?collapsed:expanded;var end=direction==0?expanded:collapsed;var elapsed=0;
            foreach(var time in new[]{0,16,100,250,400,499})
            {
                Flush(time-elapsed);elapsed=time;
                Assert.That(Layer(target),Is.Not.Null,"Still active before 500 ms");
                var expected=Interpolate(start,end,EaseInOutEasing.Transform(time/500f));
                AssertBoundsMatchDrawing(target,expected,$"Direction {direction}, {time} ms");
            }
            Flush(1);RectNear(target.worldBound,end);Assert.That(Layer(target),Is.Null);
            Flush(1000);
        }
    }
    [Test] public void NativeSharedTransitionsSurviveRepeatedInterruptionsAndDisposeAllLayers()
    {
        MountNested(true,threeLevels:true);var random=new System.Random(9217);
        for(var i=0;i<60;i++)
        {
            _expanded.Value=!_expanded.Value;Flush();Flush(random.Next(0,65));
            var child=E("nested.child."+(_expanded.Value?1:0));
            Assert.That(float.IsFinite(child.worldBound.width)&&float.IsFinite(child.worldBound.x),Is.True);
        }
        Flush(3000);Assert.That(_nestedScope.IsTransitionActive,Is.False);
        _composer.Dispose();Flush();
        Assert.That(rootVisualElement.Query<VisualElement>("shared-transition-layer").ToList(),Is.Empty);
        Assert.That(rootVisualElement.Query<VisualElement>("shared-transition-placeholder").ToList(),Is.Empty);
    }
}
