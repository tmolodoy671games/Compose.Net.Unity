using System;
using System.Collections;
using System.Linq;
using System.Threading;
using Compose.Net.Unity.Samples.Behaviors.Content;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;
using UnityEditor.UIElements;
using UnityEditor.UIElements.TestFramework;
using static Compose.Net.ComposeFunctions;

namespace Compose.Net.Unity.Tests;

[TestFixtureSource(nameof(Cases))]
public sealed partial class InteractionDemoTests : UITestFixture
{
    private readonly string id;
    public InteractionDemoTests(string caseId) => id = caseId;
    public static object[] Cases => new InteractionDemoState().Cases.Select(item => (object)new object[] { item.Id }).ToArray();
    private ComposeView _view = null!;
    private InteractionDemoCase _case = null!;
    private SynchronizationContext? _previous;
    private readonly TestUiContext _context = new();
    private VisualElement Target => _view.Q("interaction." + id);
    private VisualElement Control(string name) => _view.Q("interaction." + id + "." + name);
    [SetUp] public void Setup()
    {
        RecreatePanel(); panelSize = new Vector2(700, 500);
        _previous = SynchronizationContext.Current; SynchronizationContext.SetSynchronizationContext(_context);
        _case = new InteractionDemoState()[id];
        _view = new ComposeView(); rootVisualElement.Add(_view); _view.style.flexGrow = 1;
        simulate.MouseMove(new Vector2(680,480));
    }
    [TearDown] public void Teardown() { _view.Dispose(); _context.Drain(); SynchronizationContext.SetSynchronizationContext(_previous); }
    [Composable] private void Content() => InteractionDemo.Card(_case);
    private IEnumerator Mount() { _view.SetContent(Content); yield return Settle(); }
    private IEnumerator Settle()
    { for(var i=0;i<6;i++) { _context.Drain(); ComposeInvalidatorHolder.ComposeInvalidator.Tick(); simulate.FrameUpdate(); _context.Drain(); yield return null; } }
    private IEnumerator Wait(Func<bool> ready)
    {
        var until=DateTime.UtcNow.AddSeconds(3);
        while(!ready() && DateTime.UtcNow<until) yield return Settle();
        Assert.That(ready(),Is.True,_case.Log.Value);
    }
    [UnityTest] public IEnumerator CardHasUsableLayoutAndInputUpdatesLiveFeedback()
    {
        yield return Mount();
        Assert.That(Target.worldBound.width,Is.EqualTo(314).Within(1)); Assert.That(Target.worldBound.height,Is.EqualTo(110).Within(1));
        Assert.That(Control("enabled").worldBound.yMax,Is.LessThanOrEqualTo(_view.Q("case."+id).worldBound.yMax));
        if(!_case.Enabled.Value) { simulate.Click(Control("enabled")); yield return Settle(); }
        var point=Target.worldBound.center;
        if(_case.Kind==InteractionDemoKind.Hover)
        { simulate.MouseMove(Target); yield return Wait(()=>_case.Enters>0); Assert.That(Control("status").worldBound.height,Is.GreaterThan(0)); }
        else if(_case.Kind==InteractionDemoKind.Scroll)
        { simulate.ScrollWheel(Target,new Vector2(0,1)); yield return Settle(); Assert.That(_case.Value.Value,Is.Not.Zero); }
        else
        {
            simulate.MouseDown(point); simulate.MouseMove(point+(_case.Axis==Orientation.Horizontal?new Vector2(30,0):new Vector2(0,30))); simulate.MouseUp(point);
            yield return Wait(()=>_case.Stops==1); Assert.That(_case.Value.Value,Is.Not.Zero);
        }
    }
    [UnityTest] public IEnumerator ControlsToggleEnabledReplaceSourceAndRemoveRestoreTarget()
    {
        yield return Mount(); var enabled=_case.Enabled.Value;
        simulate.Click(Control("enabled")); yield return Settle(); Assert.That(_case.Enabled.Value,Is.EqualTo(!enabled));
        var source=_case.Source.Value; simulate.Click(Control("source")); yield return Settle(); Assert.That(_case.Source.Value,Is.Not.SameAs(source));
        simulate.Click(Control("show")); yield return Settle(); Assert.That(Target,Is.Null);
        simulate.Click(Control("show")); yield return Settle(); Assert.That(Target,Is.Not.Null);
        if(_case.Kind!=InteractionDemoKind.Hover)
        {
            var reverse=_case.Reverse.Value; simulate.Click(Control("reverse")); yield return Settle(); Assert.That(_case.Reverse.Value,Is.EqualTo(!reverse));
            simulate.Click(Control("callback")); yield return Settle(); Assert.That(_case.Multiplier.Value,Is.EqualTo(2));
            simulate.Click(Control("program")); yield return Settle(); Assert.That(_case.Value.Value,Is.GreaterThan(0));
            simulate.Click(Control("reset")); yield return Settle(); Assert.That(_case.Value.Value,Is.Zero);
        }
    }
}
public sealed partial class InteractionDemoScreenTests : UITestFixture
{
    private ComposeView _view = null!;
    private InteractionDemoState _state = null!;
    private SynchronizationContext? _previous;
    private readonly TestUiContext _context = new();
    [SetUp] public void Setup()
    {
        RecreatePanel(); panelSize=new Vector2(1100,800); _previous=SynchronizationContext.Current; SynchronizationContext.SetSynchronizationContext(_context);
        _view=new ComposeView(); _view.style.flexGrow=1; rootVisualElement.Add(_view); _state=new InteractionDemoState();
    }
    [TearDown] public void Teardown() { _view.Dispose(); _context.Drain(); SynchronizationContext.SetSynchronizationContext(_previous); }
    [Composable] private void Content()=>InteractionDemo.Screen(_state);
    private IEnumerator Settle() { for(var i=0;i<8;i++) { _context.Drain(); ComposeInvalidatorHolder.ComposeInvalidator.Tick(); simulate.FrameUpdate(); _context.Drain(); yield return null; } }
    [UnityTest] public IEnumerator All24ExamplesArePresentAndLastRowIsReachable()
    {
        _view.SetContent(Content); yield return Settle();
        var capture = Environment.GetEnvironmentVariable("COMPOSE_INTERACTION_CAPTURE");
        if (!string.IsNullOrEmpty(capture)) rootVisualElement.CaptureToPNG(capture);
        Assert.That(_state.Cases.Length,Is.EqualTo(24)); Assert.That(_state.Cases.Select(c=>c.Id).Distinct().Count(),Is.EqualTo(24));
        foreach(var item in _state.Cases) Assert.That(_view.Q("case."+item.Id),Is.Not.Null,item.Id);
        var scroll=_view.Q<ScrollView>(); Assert.That(scroll,Is.Not.Null);
        scroll.ScrollTo(_view.Q("case.hover-recompose")); yield return Settle();
        var last=_view.Q("case.hover-recompose"); Assert.That(last.worldBound.Overlaps(scroll.contentViewport.worldBound),Is.True);
        Assert.That(last.worldBound.xMax,Is.LessThanOrEqualTo(1100));
        simulate.Click(_view.Q("interaction.hover-recompose.enabled")); yield return Settle(); Assert.That(_state["hover-recompose"].Enabled.Value,Is.False);
    }
}
