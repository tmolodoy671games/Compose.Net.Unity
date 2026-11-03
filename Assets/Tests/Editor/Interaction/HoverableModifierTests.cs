using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Compose.Net;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;
using static Compose.Net.ComposeFunctions;
using Color = Compose.Net.Color;
namespace Compose.Net.Unity.Tests;

public sealed partial class HoverableModifierTests : UITestFixture
{
    private ComposeView _view = null!;
    private IMutableState<bool> _enabled = null!, _present = null!, _visible = null!;
    private IMutableState<int> _revision = null!;
    private IMutableState<IMutableInteractionSource> _source = null!;
    private IState<bool> _hovered = null!;
    private readonly List<IInteraction> _events = new();
    private CancellationTokenSource _collect = null!;
    private bool _nested, _shared, _duplicate;
    private SynchronizationContext? _previous;
    private readonly TestUiContext _context = new();
    private VisualElement Target => _view.Q("hover");
    private IHoverInteraction.Enter[] Enters => _events.OfType<IHoverInteraction.Enter>().ToArray();
    private IHoverInteraction.Exit[] Exits => _events.OfType<IHoverInteraction.Exit>().ToArray();
    [SetUp] public void Setup()
    {
        RecreatePanel(); panelSize = new Vector2(700, 500); _previous = SynchronizationContext.Current; SynchronizationContext.SetSynchronizationContext(_context);
        _view = new ComposeView(); _view.style.flexGrow = 1; rootVisualElement.Add(_view);
        _enabled = MutableStateOf(true); _present = MutableStateOf(true); _visible = MutableStateOf(true); _revision = MutableStateOf(0);
        _source = MutableStateOf<IMutableInteractionSource>(MutableInteractionSource()); _events.Clear(); _nested = _shared = _duplicate = false;
        _collect = new CancellationTokenSource(); _ = _source.Value.Interactions.Collect(_events.Add, _collect.Token);
        simulate.MouseMove(new Vector2(680, 480));
    }
    [TearDown] public void Teardown() { _view.Dispose(); _context.Drain(); _collect.Cancel(); _collect.Dispose(); SynchronizationContext.SetSynchronizationContext(_previous); }
    [Composable] private void Content()
    {
        var revision = _revision.Value; _hovered = _source.Value.CollectIsHoveredAsState();
        Column(content: () =>
        {
            if (_visible.Value)
            {
                var modifier = Modifier.Size(240.Dp(), 160.Dp()).TestTag("hover").Background(Color.DodgerBlue);
                if (_present.Value) modifier = modifier.Hoverable(_source.Value, _enabled.Value);
                if (_duplicate) modifier = modifier.Hoverable(_source.Value, _enabled.Value);
                Column(modifier: modifier, content: () =>
                {
                    var child = Modifier.Size(120.Dp(), 50.Dp()).TestTag("hover-child");
                    if (_nested) child = child.Hoverable(_shared ? _source.Value : Remember(MutableInteractionSource));
                    Text("Child", modifier: child);
                });
            }
            Text("sibling", modifier: Modifier.Size(240.Dp(), 60.Dp()).TestTag("sibling").Hoverable(_source.Value));
        });
    }
    private IEnumerator Mount() { _view.SetContent(Content); yield return Settle(); }
    private void Flush() { _context.Drain(); ComposeInvalidatorHolder.ComposeInvalidator.Tick(); simulate.FrameUpdate(); _context.Drain(); }
    private IEnumerator Settle() { for (var i = 0; i < 5; i++) { Flush(); yield return null; } }
    private IEnumerator Wait(Func<bool> ready)
    { var until = DateTime.UtcNow.AddSeconds(3); while (!ready() && DateTime.UtcNow < until) { Flush(); yield return null; } Assert.That(ready(), Is.True, string.Join(", ", _events)); }
    [UnityTest] public IEnumerator EnterMoveLeaveArePairedWithoutDuplicates()
    {
        yield return Mount(); simulate.MouseMove(Target); yield return Wait(() => Enters.Length == 1);
        simulate.MouseMove(Target.worldBound.center + new Vector2(10, 10)); yield return Settle(); Assert.That(Enters.Length, Is.EqualTo(1)); Assert.That(_hovered.Value, Is.True);
        simulate.MouseMove(new Vector2(680, 480)); yield return Wait(() => Exits.Length == 1);
        Assert.That(Exits.Single().Enter, Is.SameAs(Enters.Single())); Assert.That(_hovered.Value, Is.False);
    }
    [UnityTest] public IEnumerator ChildBoundaryDoesNotExitParent()
    {
        _nested = true; yield return Mount(); simulate.MouseMove(Target); yield return Wait(() => Enters.Length == 1);
        simulate.MouseMove(_view.Q("hover-child")); yield return Settle(); Assert.That(Enters.Length, Is.EqualTo(1)); Assert.That(Exits, Is.Empty);
    }
    [UnityTest] public IEnumerator SharedNestedSourceRemainsHoveredAfterChildExit()
    {
        _nested = _shared = true; yield return Mount(); simulate.MouseMove(_view.Q("hover-child")); yield return Wait(() => Enters.Length == 2);
        simulate.MouseMove(Target.worldBound.center); yield return Wait(() => Exits.Length == 1); Assert.That(_hovered.Value, Is.True);
        simulate.MouseMove(new Vector2(680, 480)); yield return Wait(() => Exits.Length == 2); Assert.That(_hovered.Value, Is.False);
    }
    [UnityTest] public IEnumerator DuplicateModifiersOwnSeparatePairs()
    {
        _duplicate = true; yield return Mount(); simulate.MouseMove(Target); yield return Wait(() => Enters.Length == 2);
        simulate.MouseMove(new Vector2(680, 480)); yield return Wait(() => Exits.Length == 2);
        Assert.That(Exits.Select(e => e.Enter).Distinct(ReferenceComparer.Instance).Count(), Is.EqualTo(2));
    }
    [UnityTest] public IEnumerator DisabledDoesNotEmitAndDoesNotDisableElement()
    {
        _enabled.Value = false; yield return Mount(); simulate.MouseMove(Target); yield return Settle(); Assert.That(_events, Is.Empty); Assert.That(Target.enabledSelf, Is.True);
        _enabled.Value = true; yield return Settle(); simulate.MouseMove(new Vector2(680,480)); simulate.MouseMove(Target); yield return Wait(() => Enters.Length == 1);
    }
    [UnityTest] public IEnumerator DisableHoveredClosesInteractionOnce()
    {
        yield return Mount(); simulate.MouseMove(Target); yield return Wait(() => Enters.Length == 1);
        _enabled.Value = false; yield return Wait(() => Exits.Length == 1); simulate.MouseMove(new Vector2(680,480)); yield return Settle(); Assert.That(Exits.Length, Is.EqualTo(1));
    }
    [UnityTest] public IEnumerator RecompositionKeepsEnterIdentity()
    {
        yield return Mount(); simulate.MouseMove(Target); yield return Wait(() => Enters.Length == 1);
        for (var i=0;i<5;i++) { _revision.Value++; yield return Settle(); }
        Assert.That(Enters.Length, Is.EqualTo(1)); Assert.That(Exits, Is.Empty);
    }
    [UnityTest] public IEnumerator SourceReplacementExitsOldSourceAndWaitsForNextEnter()
    {
        yield return Mount(); simulate.MouseMove(Target); yield return Wait(() => Enters.Length == 1);
        var next = MutableInteractionSource(); var events = new List<IInteraction>(); _ = next.Interactions.Collect(events.Add, _collect.Token);
        _source.Value = next; yield return Wait(() => Exits.Length == 1); Assert.That(events, Is.Empty); Assert.That(_hovered.Value, Is.False);
        simulate.MouseMove(new Vector2(680,480)); simulate.MouseMove(Target); yield return Wait(() => events.OfType<IHoverInteraction.Enter>().Count() == 1);
    }
    [UnityTest] public IEnumerator RemovingModifierClosesInteraction()
    {
        yield return Mount(); simulate.MouseMove(Target); yield return Wait(() => Enters.Length == 1);
        _present.Value = false; yield return Wait(() => Exits.Length == 1);
    }
    [UnityTest] public IEnumerator RemovingElementClosesInteraction()
    {
        yield return Mount(); simulate.MouseMove(Target); yield return Wait(() => Enters.Length == 1);
        _visible.Value = false; yield return Wait(() => Exits.Length == 1);
    }
    [UnityTest] public IEnumerator DetachingPanelClosesInteraction()
    {
        yield return Mount(); simulate.MouseMove(Target); yield return Wait(() => Enters.Length == 1);
        _view.RemoveFromHierarchy(); yield return Wait(() => Exits.Length == 1);
    }
    [UnityTest] public IEnumerator SiblingSourceEndsOnlyTheExitedElement()
    {
        yield return Mount(); simulate.MouseMove(Target); yield return Wait(() => Enters.Length == 1);
        simulate.MouseMove(_view.Q("sibling")); yield return Wait(() => Enters.Length == 2 && Exits.Length == 1); Assert.That(_hovered.Value, Is.True);
        simulate.MouseMove(new Vector2(680,480)); yield return Wait(() => Exits.Length == 2); Assert.That(_hovered.Value, Is.False);
    }
    [UnityTest] public IEnumerator TouchEnterDoesNotHover()
    {
        yield return Mount(); var touch = new Touch { fingerId=0, position=Target.worldBound.center, phase=TouchPhase.Began, pressure=1 };
        using var down = PointerDownEvent.GetPooled(touch, EventModifiers.None);
        using var enter = PointerEnterEvent.GetPooled(down); enter.target = Target; Target.SendEvent(enter);
        yield return Settle(); Assert.That(Enters, Is.Empty);
    }
    [UnityTest] public IEnumerator HoverDoesNotConsumeParentPointerEvents()
    {
        yield return Mount(); var clicks = 0; rootVisualElement.RegisterCallback<PointerDownEvent>(_ => clicks++);
        simulate.Click(Target); yield return Settle(); Assert.That(clicks, Is.EqualTo(1));
    }
    private sealed class ReferenceComparer : IEqualityComparer<IHoverInteraction.Enter>
    {
        internal static readonly ReferenceComparer Instance = new();
        public bool Equals(IHoverInteraction.Enter a, IHoverInteraction.Enter b) => ReferenceEquals(a,b);
        public int GetHashCode(IHoverInteraction.Enter value) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value);
    }
}
