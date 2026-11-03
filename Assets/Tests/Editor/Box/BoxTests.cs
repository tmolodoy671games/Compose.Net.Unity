using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Compose.Net;
using static Compose.Net.ComposeFunctions;
using Compose.Net.Unity.Packages.Compose.Net.Unity.Runtime.Adapters;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;
using Color = Compose.Net.Color;

namespace Compose.Net.Unity.Tests;

public sealed partial class BoxTests : UITestFixture
{
    private IComposer _composer = null!;
    private Adapter _adapter = null!;
    private IMutableState<int> _revision = null!;
    private IModifier _box = null!;
    private Alignment _alignment = Alignment.TopStart;
    private string? _regressionText;
    private int _regressionFont;
    private bool _propagate, _emptyOverload, _text, _nested, _scroll;
    private readonly List<Item> _items = new();
    private int _clicked;
    private IScrollState? _scrollState;
    private sealed record Item(string Id, IModifier Modifier);

    [SetUp] public void Setup()
    {
        panelSize = new Vector2(800, 600); simulate.timePerSimulatedFrameMs = 0;
        rootVisualElement.style.width = 800; rootVisualElement.style.height = 600;
        _adapter = new Adapter(); _revision = MutableStateOf(0); _box = Modifier;
        _alignment = Alignment.TopStart; _propagate = _emptyOverload = _text = _nested = _scroll = false;
        _regressionText = null;
        _clicked = 0; _items.Clear(); _scrollState = null; _demo = null; _gallery = null;
        using var initial = IComposer.Create();
        Bootstrap(initial, _adapter, false, new ComposeConfig(Logging:false), new Root(rootVisualElement), Empty);
    }
    [TearDown] public void Teardown()
    {
        _composer?.Dispose(); simulate.ResetTimePerSimulatedFrameToDefault();
    }
    [Composable] private static void Empty() { }
    private void Mount()
    {
        _composer = IComposer.Create();
        Bootstrap(_composer, _adapter, false, new ComposeConfig(Logging: false), new Root(rootVisualElement), Content);
        Flush();
    }
    private void Flush()
    {
        for (var i = 0; i < 8; i++) { _adapter.Invalidator.Tick(); simulate.FrameUpdateMs(0); }
    }
    private void Update() { _revision.Value++; Flush(); }
    [Composable] private void Content()
    {
        var revision = _revision.Value;
        if (_demo != null) { Compose.Net.Unity.Samples.Behaviors.Content.BoxDemo.Card(_demo); return; }
        if (_gallery != null) { Compose.Net.Unity.Samples.Behaviors.Content.BoxDemo.Screen(_gallery); return; }
        var modifier = _box.TestTag("box");
        if (_scroll) modifier = modifier.VerticalScroll(_scrollState!);
        if (_emptyOverload) Box(modifier);
        else Box(Children, modifier, _alignment, _propagate);
    }
    [Composable, BoxScope] private void Children()
    {
        var revision = _revision.Value;
        foreach (var item in _items)
            Key(item.Id, () => Box(item.Modifier.TestTag(item.Id)));
        if (_regressionText != null) Text(_regressionText, fontSize: _regressionFont.Sp(), color: Color.White, modifier: Modifier.TestTag("sample-text"));
        if (_text) Text("Native text wraps inside the incoming maximum width, keeping its measured height.",
            fontSize: 16.Sp(), modifier: Modifier.TestTag("text"));
        if (_nested) Box(contentAlignment: Alignment.BottomEnd, modifier: Modifier.Size(120.Dp(), 80.Dp()).Align(Alignment.BottomEnd).TestTag("inner"),
            content: () => Box(Modifier.Size(30.Dp(), 20.Dp()).TestTag("inner-child")));
    }
    private void Add(string id, float w, float h, IModifier? extra = null) => _items.Add(new Item(id, Modifier.Size(w.Dp(),h.Dp()).Then(extra)));
    private VisualElement E(string id) => rootVisualElement.Q(id) ?? throw new AssertionException("Missing " + id);
    private static void Near(float value, float expected) => Assert.That(value, Is.EqualTo(expected).Within(.6f));
    private void Size(string id, float w, float h) { Near(E(id).layout.width,w); Near(E(id).layout.height,h); }
    private void At(string id, float x, float y)
    {
        var child = E(id); var parent = E("box");
        Near(child.worldBound.x-parent.worldBound.x,x); Near(child.worldBound.y-parent.worldBound.y,y);
    }

    [Test, BoxScope] public void EmptyBoxIsZero() { _emptyOverload=true; Mount(); Size("box",0,0); }
    [Test, BoxScope] public void EmptyContentIsZero() { Mount(); Size("box",0,0); }
    [Test, BoxScope] public void EmptyBoxHonorsMinimums() { _box=Modifier.SizeIn(minWidth:90.Dp(),minHeight:50.Dp());_emptyOverload=true;Mount();Size("box",90,50); }
    [Test, BoxScope] public void EmptyBoxHasFixedSizeAndNativeInput()
    {
        _box=Modifier.Size(100.Dp(),60.Dp()).Background(Color.Orange).Clickable(()=>_clicked++);_emptyOverload=true;Mount();
        simulate.Click(E("box").worldBound.center);Flush();Assert.That(_clicked,Is.EqualTo(1));Size("box",100,60);
    }
    [Test, BoxScope] public void WrapsLargestChildOnEachAxisInNativeGrid()
    {
        Add("wide",130,30);Add("tall",40,90);Mount();Size("box",130,90);At("wide",0,0);At("tall",0,0);
        Assert.That(E("box").resolvedStyle.display,Is.EqualTo(DisplayStyle.Grid));
        Assert.That(E("box").childCount,Is.EqualTo(2));
        Assert.That(E("wide").resolvedStyle.translate.x,Is.EqualTo(0));
        Assert.That(E("tall").resolvedStyle.translate.y,Is.EqualTo(0));
    }
    [TestCase(0,0)][TestCase(1,0)][TestCase(2,0)][TestCase(0,1)][TestCase(1,1)][TestCase(2,1)][TestCase(0,2)][TestCase(1,2)][TestCase(2,2)]
    public void AllNineContentAlignments(int x,int y)
    {
        _alignment=Alignments[y*3+x];_box=Modifier.Size(200.Dp(),140.Dp());Add("a",40,20);Add("b",80,60);Mount();
        At("a",x*80,y*60);At("b",x*60,y*40);
    }
    private static readonly Alignment[] Alignments={Alignment.TopStart,Alignment.TopCenter,Alignment.TopEnd,Alignment.CenterStart,Alignment.Center,Alignment.CenterEnd,Alignment.BottomStart,Alignment.BottomCenter,Alignment.BottomEnd};
    [Test, BoxScope] public void PerChildAlignmentOverridesDefaultAndUpdates()
    {
        _alignment=Alignment.Center;_box=Modifier.Size(200.Dp(),140.Dp());Add("a",40,20,Modifier.Align(Alignment.BottomEnd));Add("b",80,60);Mount();
        At("a",160,120);At("b",60,40);_alignment=Alignment.TopEnd;Update();At("a",160,120);At("b",120,0);
        _items[0]=new Item("a",Modifier.Size(40.Dp(),20.Dp()));Update();At("a",160,0);
    }
    [Test, BoxScope] public void FirstParentDataModifierWins()
    {
        _box=Modifier.Size(180.Dp(),100.Dp());
        Add("align-first",30,20,Modifier.Align(Alignment.BottomEnd).MatchParentSize());
        Add("match-first",30,20,Modifier.MatchParentSize().Align(Alignment.TopStart));
        Add("two-aligns",30,20,Modifier.Align(Alignment.Center).Align(Alignment.BottomEnd));Mount();
        Size("align-first",30,20);At("align-first",150,80);Size("match-first",180,100);At("two-aligns",75,40);
    }
    [Test, BoxScope] public void MatchParentDoesNotParticipateInMeasurementAndHonorsPadding()
    {
        _box=Modifier.Background(Color.Gray).Padding(10.Dp());Add("overlay",500,400,Modifier.MatchParentSize());Add("normal",90,50);Mount();
        Size("box",110,70);Size("overlay",90,50);At("overlay",10,10);At("normal",10,10);
    }
    [Test, BoxScope] public void OnlyMatchParentChildrenYieldMinimumSize()
    {
        _box=Modifier.SizeIn(minWidth:80.Dp(),minHeight:60.Dp());Add("a",500,400,Modifier.MatchParentSize());Add("b",200,300,Modifier.MatchParentSize());Mount();
        Size("box",80,60);Size("a",80,60);Size("b",80,60);
    }
    [Test, BoxScope] public void OnlyMatchParentChildrenWithoutMinimumAreZero()
    { Add("a",500,400,Modifier.MatchParentSize());Mount();Size("box",0,0);Size("a",0,0); }
    [Test, BoxScope] public void MatchParentTogglesAndRestoresChildSize()
    {
        Add("a",120,80);Add("b",30,20,Modifier.MatchParentSize());Mount();Size("b",120,80);
        _items[1]=new Item("b",Modifier.Size(30.Dp(),20.Dp()).Align(Alignment.BottomEnd));Update();Size("b",30,20);At("b",90,60);
        _items.RemoveAt(0);Update();Size("box",30,20);At("b",0,0);
    }
    [Test, BoxScope] public void DefaultDropsMinimums()
    { _box=Modifier.SizeIn(minWidth:140.Dp(),minHeight:100.Dp());Add("a",30,20);Mount();Size("box",140,100);Size("a",30,20); }
    [Test, BoxScope] public void PropagatesMinimumsAndTogglesBack()
    {
        _box=Modifier.SizeIn(minWidth:140.Dp(),minHeight:100.Dp());_propagate=true;Add("a",30,20);Add("b",180,40);Mount();
        Size("box",180,100);Size("a",140,100);Size("b",180,100);
        _propagate=false;Update();Size("a",30,20);Size("b",180,40);
    }
    [Test, BoxScope] public void FixedConstraintsPropagateMinusPadding()
    { _box=Modifier.Size(180.Dp(),120.Dp()).Padding(10.Dp());_propagate=true;Add("a",30,20);Mount();Size("a",160,100);At("a",10,10); }
    [Test, BoxScope] public void MaximumConstraintsClampBothChildren()
    { _box=Modifier.SizeIn(maxWidth:110.Dp(),maxHeight:70.Dp());Add("a",300,20);Add("b",30,400);Mount();Size("box",110,70);Size("a",110,20);Size("b",30,70); }
    [Test, BoxScope] public void FixedParentClampsOversizedChildWithoutMinimumPropagation()
    { _box=Modifier.Size(110.Dp(),70.Dp());Add("a",300,400);Mount();Size("a",110,70); }
    [Test, BoxScope] public void FillMaxDeterminesBoxSize()
    { _items.Add(new Item("a",Modifier.FillMaxSize()));Mount();Size("box",800,600);Size("a",800,600); }
    [Test, BoxScope] public void FractionalFillUsesIncomingMaximumOnce()
    { _box=Modifier.SizeIn(maxWidth:200.Dp(),maxHeight:160.Dp());_items.Add(new Item("a",Modifier.FillMaxSize(.5f)));Mount();Size("box",100,80);Size("a",100,80); }
    [Test, BoxScope] public void FillOneAxisKeepsOtherIntrinsic()
    { _box=Modifier.SizeIn(maxWidth:200.Dp(),maxHeight:160.Dp());_items.Add(new Item("a",Modifier.FillMaxWidth().Height(35.Dp())));Mount();Size("box",200,35); }
    [Test, BoxScope] public void ContentGrowthShrinkAndRemovalRemeasure()
    {
        Add("a",30,20);Mount();Size("box",30,20);Add("b",140,90);Update();Size("box",140,90);
        _items[1]=new Item("b",Modifier.Size(50.Dp(),40.Dp()));Update();Size("box",50,40);
        _items.Clear();Update();Size("box",0,0);
    }
    [Test, BoxScope] public void ResizeUpdatesAlignmentAndMatchParent()
    {
        _box=Modifier.Size(200.Dp(),140.Dp());_alignment=Alignment.BottomEnd;Add("a",40,20);Add("overlay",0,0,Modifier.MatchParentSize());Mount();
        _box=Modifier.Size(120.Dp(),80.Dp());Update();At("a",80,60);Size("overlay",120,80);
    }
    [Test, BoxScope] public void NestedBoxesAlignIndependently()
    { _box=Modifier.Size(200.Dp(),140.Dp());_nested=true;Mount();At("inner",80,60);At("inner-child",170,120); }
    [Test, BoxScope] public void OffsetDoesNotAffectBoxMeasurementOrOtherChildren()
    { Add("a",100,60,Modifier.Offset(15.Dp(),12.Dp()));Add("b",40,20);Mount();Size("box",100,60);At("a",15,12);At("b",0,0); }
    [Test, BoxScope] public void NativePickingFollowsCompositionOrderAndKeyedReorder()
    {
        Add("a",100,60,Modifier.Background(Color.Orange).Clickable(()=>_clicked=1));Add("b",100,60,Modifier.Background(Color.DodgerBlue).Clickable(()=>_clicked=2));Mount();
        var original=E("a");simulate.Click(E("box").worldBound.center);Flush();Assert.That(_clicked,Is.EqualTo(2));
        _items.Reverse();Update();Assert.That(E("a"),Is.SameAs(original));simulate.Click(E("box").worldBound.center);Flush();Assert.That(_clicked,Is.EqualTo(1));
    }
    [Test, BoxScope] public void TextWrapUsesNativeMeasurement()
    { _box=Modifier.Width(150.Dp());_text=true;Mount();Assert.That(E("text").layout.width,Is.LessThanOrEqualTo(150));Assert.That(E("text").layout.height,Is.GreaterThan(35));Near(E("box").layout.height,E("text").layout.height+6); }
    [Test, BoxScope] public void VerticalScrollLeavesHeightUnbounded()
    { _box=Modifier.Size(200.Dp(),100.Dp());_scroll=true;_scrollState=new ScrollState();Add("a",150,300);Mount();Size("a",150,300);Near(_scrollState.MaxValue,200); }
    [Test, BoxScope] public void UntrackRestoresAuthoredNativeStyles()
    {
        var box=new Compose.Net.Unity.Box();rootVisualElement.Add(box);var child=new VisualElement();child.style.width=40;child.style.minWidth=12;child.style.maxWidth=60;child.style.alignSelf=Align.Center;
        box.Add(child);box.Track(child);simulate.FrameUpdateMs(0);box.Untrack(child);
        Assert.That(child.style.minWidth.value.value,Is.EqualTo(12));Assert.That(child.style.maxWidth.value.value,Is.EqualTo(60));Assert.That(child.style.alignSelf.value,Is.EqualTo(Align.Center));Assert.That(child.style.gridRowStart.keyword,Is.EqualTo(StyleKeyword.Null));
    }
    private sealed class Root(VisualElement element) : UnityReusableComposeNode(element), IReusableComposeNode { public new bool IsRoot=>true; }
    private sealed class Adapter : IComposeAdapter
    {
        internal readonly ComposeInvalidator Invalidator=new();
        private IComposeAdapter Native=>ComposeUnityAdapter.Instance;
        public IFrameClock FrameClock=>Native.FrameClock;
        public ComposeInvalidator ComposeInvalidatorInstance=>Invalidator;
        public IModifiersFactory ModifiersFactory=>Native.ModifiersFactory;
        public IReusableNodeFactory NodeFactory=>Native.NodeFactory;
        public IComposeLogger Logger=>Native.Logger;
        public IEnterTransitionsFactory EnterTransitionsFactory=>Native.EnterTransitionsFactory;
        public IExitTransitionsFactory ExitTransitionsFactory=>Native.ExitTransitionsFactory;
        public IBrushFactory BrushFactory=>Native.BrushFactory;
        public IDisposable StartTimeCoroutine(IEnumerable<TimeSpan?> coroutine)=>Native.StartTimeCoroutine(coroutine);
        public int Framerate()=>60;
    }
}
