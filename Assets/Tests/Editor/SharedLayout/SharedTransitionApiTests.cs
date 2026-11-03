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
    private IMutableState<bool> _overlayEnabled = null!, _overlayPage = null!;
    private bool _automaticOverlay;
    private SharedTransitionScope _overlayScope = null!;
    private void MountOverlay(bool automatic)
    {
        _composer.Dispose(); Flush(); panelSize = new Vector2(800,600);
        _overlayEnabled = MutableStateOf(false); _overlayPage = MutableStateOf(false); _automaticOverlay = automatic;
        _composer = IComposer.Create();
        Bootstrap(_composer,_adapter,false,new ComposeConfig(Logging:false),new RootNode(rootVisualElement),OverlayContent); Flush();
    }
    [Composable] private void OverlayContent() => SharedTransitionScope((shared, rootModifier) =>
    {
        _overlayScope = shared;
        Box(modifier:rootModifier.FillMaxSize().TestTag("overlay.root"), content: () =>
        {
            Box(modifier:Modifier.Alpha(.25f).TestTag("overlay.parent"), content: () =>
                Box(modifier:Modifier.Size(50.Dp(),30.Dp()).TestTag("overlay.badge")
                    .RenderInSharedTransitionScopeOverlay(shared, renderInOverlay:_automaticOverlay ? null : () => _overlayEnabled.Value), content:()=>{}));
            AnimatedContent(_overlayPage.Value, (visibility, page) =>
                Box(modifier:Modifier.Size((page ? 200 : 100).Dp(),100.Dp()).TestTag("overlay.card."+page)
                    .SharedElement(shared.RememberSharedContentState("card"),visibility,(_,_)=>Tween(500)),content:()=>{}),
                transitionSpec:_=>IEnterTransition.None.TogetherWith(IExitTransition.None).Using(null));
        });
    });
    [Test] public void OverlayPredicateIsObservedWhileIdleAndRestoresNativeHierarchy()
    {
        MountOverlay(false); var badge=E("overlay.badge"); var parent=badge.parent; var bounds=badge.worldBound;
        Near(Alpha(badge),.25f,.001f);
        _overlayEnabled.Value=true; Flush();
        Assert.That(Layer(badge),Is.Not.Null); Near(Alpha(badge),1,.001f); RectNear(badge.worldBound,bounds);
        Assert.That(_overlayScope.IsTransitionActive,Is.False,"An ordinary overlay does not start a shared transition");
        _overlayEnabled.Value=false; Flush();
        Assert.That(badge.parent,Is.SameAs(parent)); RectNear(badge.worldBound,bounds); Near(Alpha(badge),.25f,.001f);
        Assert.That(rootVisualElement.Query<VisualElement>("shared-transition-placeholder").ToList(),Is.Empty);
    }
    [Test] public void DefaultOverlayTracksScopeActivityAndDrawsAboveRootSharedContent()
    {
        MountOverlay(true); var badge=E("overlay.badge"); Assert.That(Layer(badge),Is.Null);
        _overlayPage.Value=true; Flush();
        var badgeLayer=Layer(badge)!; var cardLayer=Layer(E("overlay.card.True"))!;
        Assert.That(badgeLayer,Is.Not.Null);
        Assert.That(badgeLayer.parent,Is.SameAs(cardLayer.parent));
        Assert.That(badgeLayer.parent.IndexOf(badgeLayer),Is.GreaterThan(cardLayer.parent.IndexOf(cardLayer)));
        Flush(500); Assert.That(Layer(badge),Is.Null); Near(Alpha(badge),.25f,.001f);
    }
    [Test] public void ResolvedOverlayClipIsAvailableOnlyDuringSharedRendering()
    {
        Select("clip",1); Flush(100);
        var state=_state["clip"].States[1];
        Assert.That(state.ClipPathInOverlay,Is.Not.Null); Assert.That(state.ClipPathInOverlay!.IsEmpty,Is.False);
        Flush(1000); Assert.That(state.ClipPathInOverlay,Is.Null);
    }
    [TestCase(1f)][TestCase(1.25f)][TestCase(2f)]
    public void RetainedNavigationCanRepeatPushAndPopWithoutAccumulatingTransforms(float scale)
    {
        pixelsPerPoint=scale; MountNavigationShared(1200,800);
        var card=E("nav-shared.button.screen:Settings"); var bounds=card.worldBound;
        for (var i=0;i<3;i++)
        {
            NavigationClick("button.screen:Settings"); Flush(500);
            NavigationClick("back.screen:Settings"); Flush(500);
            Assert.That(E("nav-shared.button.screen:Settings"),Is.SameAs(card),"Home stays retained");
            RectNear(card.worldBound,bounds);
            Assert.That(card.resolvedStyle.scale.value,Is.EqualTo(Vector3.one));
            Assert.That(rootVisualElement.Query<VisualElement>("shared-transition-layer").ToList(),Is.Empty);
        }
    }
}
