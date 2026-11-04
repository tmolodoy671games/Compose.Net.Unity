using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Compose.Net;
using static Compose.Net.ComposeFunctions;
using Compose.Net.Unity.Packages.Compose.Net.Unity.Runtime.Adapters;
using Compose.Net.Unity.Samples.Behaviors.Content;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;
using PointerId = Compose.Net.PointerId;
using PointerType = Compose.Net.PointerType;

namespace Compose.Net.Unity.Tests;

public sealed partial class PointerInputTests : UITestFixture
{
    private IComposer _composer = null!;
    private readonly ComposeInvalidator _invalidator = new();
    private readonly UiContext _context = new();
    private SynchronizationContext? _previous;
    private IMutableState<int> _revision = null!;
    private Func<IPointerInputScope, Task> _body = null!, _parentBody = null!;
    private object? _key;
    private bool _present, _nested, _chain, _gallery;
    private PointerInputDemoState _demo = null!;
    private readonly List<PointerEvent> _events = new();
    private readonly List<string> _trace = new();
    private int _runs, _stops, _taps, _starts, _ends, _cancels;
    private Offset _delta;
    private VisualElement E(string name) => rootVisualElement.Q(name) ?? throw new AssertionException("Missing " + name);
    private VisualElement Target => E("target");

    [SetUp] public void SetUpPointer()
    {
        _previous = SynchronizationContext.Current; SynchronizationContext.SetSynchronizationContext(_context);
        panelSize = new Vector2(1100, 2100); simulate.timePerSimulatedFrameMs = 0;
        _revision = MutableStateOf(0); _key = 1; _present = true; _nested = _chain = _gallery = false;
        _body = Record; _parentBody = Record; _events.Clear(); _trace.Clear();
        _runs = _stops = _taps = _starts = _ends = _cancels = 0; _delta = Offset.Zero;
        _demo = new PointerInputDemoState();
        simulate.MouseMove(new Vector2(1090, 2090));
    }
    [TearDown] public void TearDownPointer()
    {
        _composer?.Dispose(); _composer = null!; _context.Drain();
        rootVisualElement.ReleasePointer(0);
        simulate.MouseUp(new Vector2(1090, 2090));
        simulate.ResetTimePerSimulatedFrameToDefault(); SynchronizationContext.SetSynchronizationContext(_previous);
    }
    [Composable] private void Content()
    {
        _ = _revision.Value;
        if (_gallery) { PointerInputDemo.Screen(_demo); return; }
        Column(modifier: Modifier.FillMaxSize().Padding(20.Dp()), content: () =>
        {
            if (!_present) return;
            if (_nested)
                Column(modifier: Modifier.Size(300.Dp(), 250.Dp()).TestTag("parent").PointerInput(1, _parentBody), content: TargetContent);
            else TargetContent();
        });
    }
    [Composable] private void TargetContent()
    {
        _ = _revision.Value;
        var modifier = Modifier.Size(180.Dp(), 110.Dp()).TestTag("target").PointerInput(_key, _body);
        if (_chain) modifier = modifier.PointerInput("inner", _parentBody);
        Box(modifier: modifier, content: () => Text("pointer target", modifier: Modifier.TestTag("label")));
    }
    private void Mount()
    {
        _composer?.Dispose(); rootVisualElement.Clear(); _composer = IComposer.Create();
        Bootstrap(_composer, new TestAdapter(_invalidator), false, new ComposeConfig(Logging: false), new RootNode(rootVisualElement), Content);
        Flush();
    }
    private void Flush()
    {
        SynchronizationContext.SetSynchronizationContext(_context);
        for (var i = 0; i < 5; i++) { _context.Drain(); _invalidator.Tick(); simulate.FrameUpdateMs(0); }
    }
    private void Recompose() { _revision.Value++; Flush(); }
    private Task Record(IPointerInputScope scope) => scope.AwaitPointerEventScope(async events =>
    { while (true) _events.Add(await events.AwaitPointerEvent()); });
    private Task Lifetime(IPointerInputScope scope)
    {
        _runs++;
        return scope.AwaitPointerEventScope(async events =>
        { try { while (true) _events.Add(await events.AwaitPointerEvent()); } finally { _stops++; } });
    }
    private Task Tap(IPointerInputScope scope) => scope.DetectTapGestures(onTap: _ => _taps++);
    private Task Drag(IPointerInputScope scope) => scope.DetectDragGestures((_, d) => _delta += d,
        _ => _starts++, () => _ends++, () => _cancels++);
    private void MouseDown() { simulate.MouseDown(Target.worldBound.center); Flush(); }
    private void MouseUp() { simulate.MouseUp(Target.worldBound.center); Flush(); }
    private void Touch(TouchPhase phase, Vector2 position, int finger = 0, VisualElement? targetElement = null)
    {
        var touch = new Touch { fingerId = finger, position = position, phase = phase, pressure = 1, maximumPossiblePressure = 1 };
        using EventBase evt = phase switch
        {
            TouchPhase.Began => PointerDownEvent.GetPooled(touch, EventModifiers.None),
            TouchPhase.Ended => PointerUpEvent.GetPooled(touch, EventModifiers.None),
            TouchPhase.Canceled => PointerCancelEvent.GetPooled(touch, EventModifiers.None),
            _ => PointerMoveEvent.GetPooled(touch, EventModifiers.None)
        };
        var target = targetElement ?? Target; evt.target = target; target.SendEvent(evt); Flush();
    }
    private void DownTouch(int finger = 0) => Touch(TouchPhase.Began, Target.worldBound.center, finger);
    private void UpTouch(int finger = 0) => Touch(TouchPhase.Ended, Target.worldBound.center, finger);
    private PointerEvent Last(PointerEventType type) => _events.Last(e => e.Type == type);

    [Test] public void HandlerStartsLazilyAndDoesNotRestartOnEquivalentRecomposition()
    {
        _body = Lifetime; Mount(); Assert.That(_runs, Is.Zero);
        MouseDown(); Assert.That(_runs, Is.EqualTo(1));
        _body = Lifetime; Recompose(); Assert.That(_stops, Is.Zero);
        MouseUp(); Assert.That(_runs, Is.EqualTo(1));
    }
    [Test] public void KeyChangeCancelsThenRestartsOnNextEvent()
    {
        _body = Lifetime; Mount(); MouseDown(); _key = 2; Recompose();
        Assert.That(_stops, Is.EqualTo(1)); Assert.That(_runs, Is.EqualTo(1));
        MouseUp(); Assert.That(_runs, Is.EqualTo(2));
    }
    [Test] public void ChangingLambdaMethodRestartsEvenWithEqualKey()
    {
        _body = Lifetime; Mount(); MouseDown(); _body = Record; Recompose(); Assert.That(_stops, Is.EqualTo(1));
        MouseUp(); Assert.That(_events.Last().Type, Is.EqualTo(PointerEventType.Release));
    }
    [Test] public void UpdatedClosureAtSameCallSiteDoesNotRestart()
    {
        Func<IPointerInputScope, Task> Make(int value) => async scope =>
        { _runs++; await scope.AwaitEachGesture(async s => { await s.AwaitFirstDown(); _taps += value; }); };
        _body = Make(1); Mount(); MouseDown(); MouseUp(); _body = Make(10); Recompose(); MouseDown(); MouseUp();
        Assert.That(_runs, Is.EqualTo(1)); Assert.That(_taps, Is.EqualTo(2));
        _key = 2; Recompose(); MouseDown(); MouseUp(); Assert.That(_taps, Is.EqualTo(12));
    }
    [Test] public void NullKeyIsValidAndDoesNotThrowOnModifierHash()
    { _key = null; Mount(); MouseDown(); Assert.That(Last(PointerEventType.Press).Changes[0].Pressed, Is.True); }
    [Test] public void RemovingModifierCancelsWaitsAndReleasesNativeCapture()
    {
        _body = Lifetime; Mount(); var element = Target; MouseDown(); Assert.That(element.HasPointerCapture(0), Is.True);
        _present = false; Recompose(); Assert.That(_stops, Is.EqualTo(1)); Assert.That(element.HasPointerCapture(0), Is.False);
    }
    [Test] public void DetachAndReattachRestartsLazilyWithoutDuplicateCallbacks()
    {
        _body = Lifetime; Mount(); MouseDown(); MouseUp(); var element = Target; var parent = element.parent;
        element.RemoveFromHierarchy(); Flush(); Assert.That(_stops, Is.EqualTo(1)); simulate.MouseMove(new Vector2(1090, 2090)); parent.Add(element); Flush();
        Assert.That(_runs, Is.EqualTo(1)); MouseDown(); MouseUp(); Assert.That(_runs, Is.EqualTo(2));
        Assert.That(_events.Count(e => e.Type == PointerEventType.Press), Is.EqualTo(2));
    }
    [Test] public void SameSnapshotIsDeliveredSynchronouslyInThreePasses()
    {
        PointerEvent? first = null, main = null, final = null;
        _body = s => s.AwaitPointerEventScope(async e =>
        { first = await e.AwaitPointerEvent(PointerEventPass.Initial); main = await e.AwaitPointerEvent(); main.Changes[0].Consume(); final = await e.AwaitPointerEvent(PointerEventPass.Final); });
        Mount(); DownTouch(); Assert.That(main, Is.SameAs(first)); Assert.That(final, Is.SameAs(first)); Assert.That(final!.Changes[0].IsConsumed, Is.True);
    }
    private Task Passes(IPointerInputScope scope, string name, bool consume) => scope.AwaitPointerEventScope(async e =>
    {
        await e.AwaitPointerEvent(PointerEventPass.Initial); _trace.Add(name + ".Initial");
        var main = await e.AwaitPointerEvent(); _trace.Add(name + ".Main:" + main.Changes[0].IsConsumed);
        if (consume) main.Changes[0].Consume();
        var final = await e.AwaitPointerEvent(PointerEventPass.Final); _trace.Add(name + ".Final:" + final.Changes[0].IsConsumed);
    });
    [TestCase(false)][TestCase(true)] public void NestedAndChainedModifiersUseComposePassOrder(bool chain)
    {
        _chain = chain; _nested = !chain;
        if (chain) { _body = s => Passes(s, "outer", false); _parentBody = s => Passes(s, "inner", true); }
        else { _parentBody = s => Passes(s, "outer", false); _body = s => Passes(s, "inner", true); }
        Mount(); DownTouch();
        Assert.That(_trace, Is.EqualTo(new[] { "outer.Initial", "inner.Initial", "inner.Main:False", "outer.Main:True", "outer.Final:True", "inner.Final:True" }));
    }
    [Test] public void InitialConsumptionPreventsChildTapButStillDeliversRawChanges()
    {
        _nested = true; _body = Tap;
        _parentBody = s => s.AwaitPointerEventScope(async e => { while (true) (await e.AwaitPointerEvent(PointerEventPass.Initial)).Changes[0].Consume(); });
        Mount(); MouseDown(); MouseUp(); Assert.That(_taps, Is.Zero);
    }
    [Test] public void ParentMainConsumptionCancelsChildTapAtFinalPass()
    {
        _nested = true; _body = Tap;
        _parentBody = s => s.AwaitPointerEventScope(async e => { while (true) { var evt = await e.AwaitPointerEvent(); if (evt.Type == PointerEventType.Move) evt.Changes[0].Consume(); } });
        Mount(); MouseDown(); simulate.MouseMove(Target.worldBound.center + Vector2.right * 8); Flush(); MouseUp(); Assert.That(_taps, Is.Zero);
    }
    [Test] public void CoordinatesHistoryAndWheelDeltaAreIndependentAndSurviveEventPooling()
    {
        Mount(); DownTouch(); var first = Last(PointerEventType.Press).Changes[0];
        Touch(TouchPhase.Moved, Target.worldBound.center + new Vector2(13, 7)); var move = Last(PointerEventType.Move).Changes[0];
        Assert.That(first.Position, Is.EqualTo(new Offset(90, 55))); Assert.That(move.PreviousPosition, Is.EqualTo(first.Position));
        Assert.That(move.PreviousUptimeMillis, Is.EqualTo(first.UptimeMillis)); Assert.That(move.PositionChange(), Is.EqualTo(new Offset(13, 7)));
        Assert.That(move.ScrollDelta, Is.EqualTo(Offset.Zero)); UpTouch();
        for (var i = 0; i < 10; i++) { DownTouch(); UpTouch(); }
        Assert.That(first.Pressed, Is.True); Assert.That(first.Position, Is.EqualTo(new Offset(90, 55)));
        first.Consume(); Assert.That(move.IsConsumed, Is.False);
    }
    [Test] public void PointerFramesContainOtherPressedFingersWithStationaryPreviousPositions()
    {
        Mount(); DownTouch(0); DownTouch(1); var frame = Last(PointerEventType.Press);
        Assert.That(frame.Changes.Count, Is.EqualTo(2)); Assert.That(frame.Changes.Count(c => c.ChangedToDown()), Is.EqualTo(1));
        Touch(TouchPhase.Moved, Target.worldBound.center + Vector2.right * 20, 1);
        var stationary = Last(PointerEventType.Move).Changes.Single(c => c.Id.Value == 1);
        Assert.That(stationary.PositionChange(), Is.EqualTo(Offset.Zero)); Assert.That(stationary.PreviousPressed, Is.True);
        UpTouch(1); Assert.That(Last(PointerEventType.Release).Changes.Count(c => c.Pressed), Is.EqualTo(1)); UpTouch(0);
    }
    [Test] public void HoverAndWheelAreReportedWithoutPressTransitions()
    {
        Mount(); simulate.MouseMove(Target.worldBound.center); Flush();
        Assert.That(_events.Any(e => e.Type == PointerEventType.Enter), Is.True);
        simulate.ScrollWheel(Target, new Vector2(2, 3)); Flush();
        var wheel = Last(PointerEventType.Scroll).Changes[0]; Assert.That(wheel.ScrollDelta, Is.EqualTo(new Offset(2, 3))); Assert.That(wheel.Pressed, Is.False);
        simulate.MouseMove(new Vector2(900, 1900)); Flush(); Assert.That(_events.Any(e => e.Type == PointerEventType.Exit), Is.True);
    }
    [Test] public void ConcurrentAwaitScopesEachReceiveEventsAndCanConsumeInOrder()
    {
        var counts = new int[2];
        _body = s => Task.WhenAll(Enumerable.Range(0, 2).Select(index => s.AwaitPointerEventScope(async e =>
        { var evt = await e.AwaitPointerEvent(); counts[index]++; if (index == 1) evt.Changes[0].Consume(); else Assert.That(evt.Changes[0].IsConsumed, Is.True); })));
        Mount(); DownTouch(); Assert.That(counts, Is.EqualTo(new[] { 1, 1 }));
    }
    [Test] public void CompletedBodyDoesNotRestartUntilKeyChanges()
    { _body = _ => { _runs++; return Task.CompletedTask; }; Mount(); MouseDown(); MouseUp(); MouseDown(); Assert.That(_runs, Is.EqualTo(1)); }
    [Test] public void TapReleasesImmediatelyAndIgnoresCanceledOrOutOfBoundsGestures()
    {
        _body = Tap; Mount(); MouseDown(); MouseUp(); Assert.That(_taps, Is.EqualTo(1), "first release");
        MouseDown(); simulate.MouseMove(new Vector2(900, 900)); Flush(); MouseUp(); Assert.That(_taps, Is.EqualTo(1), "outside cancel");
        MouseDown(); rootVisualElement.CapturePointer(0); simulate.FrameUpdateMs(0); Flush(); MouseUp(); Assert.That(_taps, Is.EqualTo(1), "capture loss");
    }
    [Test] public void NativeCaptureContinuesDragOutsideTheOriginalHitTarget()
    {
        _body = Drag; Mount(); MouseDown(); simulate.MouseMove(Target.worldBound.center + Vector2.right * 20); Flush();
        Assert.That(_starts, Is.EqualTo(1)); var before = _delta.X;
        simulate.MouseMove(new Vector2(900, 900)); Flush(); Assert.That(_delta.X, Is.GreaterThan(before));
        simulate.MouseUp(new Vector2(900, 900)); Flush(); Assert.That(_ends, Is.EqualTo(1)); Assert.That(_cancels, Is.Zero);
    }
    [Test] public void TouchDragReportsOnlyOverSlopAndHandsOffToRemainingFinger()
    {
        _body = Drag; Mount(); var origin = Target.worldBound.center; DownTouch();
        Touch(TouchPhase.Moved, origin + Vector2.right * 5); Assert.That(_starts, Is.Zero);
        Touch(TouchPhase.Moved, origin + Vector2.right * 12); Assert.That(_delta.X, Is.EqualTo(4).Within(.01));
        DownTouch(1); UpTouch(0); Touch(TouchPhase.Moved, origin + Vector2.right * 10, 1); UpTouch(1);
        Assert.That(_starts, Is.EqualTo(1)); Assert.That(_ends, Is.EqualTo(1)); Assert.That(_delta.X, Is.EqualTo(14).Within(.01));
    }
    [Test] public void ActiveDragCancelsExactlyOnceOnRemoval()
    {
        _body = Drag; Mount(); DownTouch(); Touch(TouchPhase.Moved, Target.worldBound.center + Vector2.right * 20);
        _present = false; Recompose(); Assert.That(_cancels, Is.EqualTo(1)); Assert.That(_ends, Is.Zero);
    }
    [TestCase(true)][TestCase(false)] public void AxisDragIgnoresCrossAxisSlop(bool horizontal)
    {
        _body = s => horizontal ? s.DetectHorizontalDragGestures((_, d) => _delta += new Offset(d, 0), _ => _starts++) :
            s.DetectVerticalDragGestures((_, d) => _delta += new Offset(0, d), _ => _starts++);
        Mount(); var origin = Target.worldBound.center; DownTouch();
        Touch(TouchPhase.Moved, origin + (horizontal ? Vector2.up : Vector2.right) * 30); Assert.That(_starts, Is.Zero);
        Touch(TouchPhase.Moved, origin + new Vector2(30, 30)); Assert.That(_starts, Is.EqualTo(1));
        Assert.That(horizontal ? _delta.Y : _delta.X, Is.Zero);
    }
    [UnityTest] public IEnumerator LongPressSuppressesTapAndReleasesPressScope()
    {
        var longs = 0; bool? released = null;
        _body = s => s.DetectTapGestures(onLongPress: _ => longs++, onTap: _ => _taps++, onPress: async (p, _) => released = await p.TryAwaitRelease());
        Mount(); MouseDown(); yield return Delay(620); Assert.That(longs, Is.EqualTo(1)); MouseUp();
        Assert.That(_taps, Is.Zero); Assert.That(released, Is.True);
    }
    [UnityTest] public IEnumerator DoubleTapAndDelayedSingleTapAreMutuallyExclusive()
    {
        var doubles = 0; _body = s => s.DetectTapGestures(onDoubleTap: _ => doubles++, onTap: _ => _taps++);
        Mount(); MouseDown(); MouseUp(); Assert.That(_taps, Is.Zero); yield return Delay(360); Assert.That(_taps, Is.EqualTo(1));
        MouseDown(); MouseUp(); yield return Delay(70); simulate.FrameUpdateMs(70); MouseDown(); MouseUp(); yield return Delay(360);
        Assert.That(doubles, Is.EqualTo(1)); Assert.That(_taps, Is.EqualTo(1));
    }
    [UnityTest] public IEnumerator ScopedTimeoutRemovesWaiterAndDoesNotStealNextEvent()
    {
        PointerEvent? next = null; var timedOut = false;
        _body = s => s.AwaitPointerEventScope(async e =>
        { await e.AwaitFirstDown(); timedOut = await e.WithTimeoutOrNull(30, t => t.AwaitPointerEvent()) == null; next = await e.AwaitPointerEvent(); });
        Mount(); MouseDown(); yield return Delay(80); Assert.That(timedOut, Is.True); MouseUp(); Assert.That(next!.Type, Is.EqualTo(PointerEventType.Release));
    }
    [Test] public void PressScopeReportsCancellationWhenHandlerIsRemoved()
    {
        bool? released = null; _body = s => s.DetectTapGestures(onPress: async (p, _) => released = await p.TryAwaitRelease());
        Mount(); MouseDown(); _present = false; Recompose(); Assert.That(released, Is.False);
    }
    [UnityTest] public IEnumerator HoldThenDragStartsAfterTimeoutAndEndsOnRelease()
    {
        _body = s => s.DetectDragGesturesAfterLongPress((_, d) => _delta += d, _ => _starts++, () => _ends++, () => _cancels++);
        Mount(); MouseDown(); Assert.That(_starts, Is.Zero); yield return Delay(620); Assert.That(_starts, Is.EqualTo(1));
        simulate.MouseMove(Target.worldBound.center + Vector2.right * 3); Flush(); MouseUp(); Assert.That(_delta.X, Is.EqualTo(3)); Assert.That(_ends, Is.EqualTo(1));
    }
    [TestCase(false)][TestCase(true)] public void TransformWaitsForSlopAndPanZoomLockSuppressesLaterRotation(bool locked)
    {
        var rotations = 0f; var calls = 0;
        _body = s => s.DetectTransformGestures((_, pan, zoom, rotation) => { _delta += pan; rotations += rotation; calls++; }, locked);
        Mount(); var center = Target.worldBound.center;
        Touch(TouchPhase.Began, center + Vector2.left * 30, 0); Touch(TouchPhase.Began, center + Vector2.right * 30, 1);
        Touch(TouchPhase.Moved, center + Vector2.left * 29, 0); Assert.That(calls, Is.Zero);
        Touch(TouchPhase.Moved, center + Vector2.left * 5, 0); Touch(TouchPhase.Moved, center + new Vector2(25, 30), 1);
        Assert.That(calls, Is.GreaterThan(0)); Assert.That(Math.Abs(rotations), locked ? Is.EqualTo(0) : Is.GreaterThan(1));
        UpTouch(0); UpTouch(1);
    }
    [Test] public void FullGalleryContainsEveryLiveExampleAndUsesNativeScrollView()
    {
        _gallery = true; Mount(); Assert.That(rootVisualElement.Q<ScrollView>(), Is.Not.Null);
        foreach (var id in PointerInputDemoState.Ids) Assert.That(E("pointer." + id).worldBound.width, Is.GreaterThan(0), id);
        var target = E("pointer.tap"); simulate.MouseDown(target); Flush(); simulate.MouseUp(target); Flush(); Assert.That(_demo["tap"].Taps, Is.EqualTo(1));
    }
    [Test] public void GalleryKeyControlsRestartButLatestValueDoesNot()
    {
        _gallery = true; Mount();
        void Click(string id) { var e = E("pointer." + id); simulate.MouseDown(e); Flush(); simulate.MouseUp(e); Flush(); }
        var scroll = rootVisualElement.Q<ScrollView>(); scroll.scrollOffset = new Vector2(0, 800); Flush();
        Click("keys"); Click("keys.step"); Click("keys"); Assert.That(_demo["keys"].Taps, Is.EqualTo(2));
        Click("keys.key"); Click("keys"); Assert.That(_demo["keys"].Taps, Is.EqualTo(12));
        Click("latest"); Click("latest.step"); Click("latest"); Assert.That(_demo["latest"].Taps, Is.EqualTo(11)); Assert.That(_demo["latest"].Runs, Is.EqualTo(1));
    }

    [Test] public void RunTokenRemainsCanceledAfterAKeyRestart()
    {
        CancellationToken old = default; bool? observed = null;
        _body = async input =>
        {
            old = input.Token;
            try { await input.AwaitPointerEventScope(async e => { while (true) await e.AwaitPointerEvent(); }); }
            finally { observed = input.Token.IsCancellationRequested; }
        };
        Mount(); DownTouch(); _key = 2; Recompose(); Assert.That(old.IsCancellationRequested, Is.True); Assert.That(observed, Is.True);
    }
    [Test] public void ThereIsNoBacklogWhenUserCodeIsNotAwaitingEvents()
    {
        var gate = new TaskCompletionSource<bool>(); PointerEvent? next = null;
        _body = input => input.AwaitPointerEventScope(async e =>
        { await e.AwaitFirstDown(); await gate.Task; next = await e.AwaitPointerEvent(); });
        Mount(); DownTouch(); Touch(TouchPhase.Moved, Target.worldBound.center + Vector2.right * 10);
        gate.SetResult(true); Flush(); Assert.That(next, Is.Null); UpTouch(); Assert.That(next!.Type, Is.EqualTo(PointerEventType.Release));
    }
    [Test] public void ScopeCanReturnAResultAndACompletedWaitIsNotReplayed()
    {
        PointerId? id = null;
        _body = async input => id = await input.AwaitPointerEventScope(async e => (await e.AwaitFirstDown()).Id);
        Mount(); DownTouch(); Assert.That(id!.Value.Value, Is.EqualTo(1));
    }
    [Test] public void DisabledNativeElementDoesNotStartPointerHandler()
    { _body = Lifetime; Mount(); Target.SetEnabled(false); DownTouch(); Assert.That(_runs, Is.Zero); }
    [Test] public void NativeCancellationRetainsLastCoordinatesAndTypeAndCannotBeATap()
    {
        Mount(); DownTouch(); var down = Last(PointerEventType.Press).Changes[0];
        Touch(TouchPhase.Canceled, Target.worldBound.center); var cancel = Last(PointerEventType.Release).Changes[0];
        Assert.That(cancel.Type, Is.EqualTo(PointerType.Touch)); Assert.That(cancel.Position, Is.EqualTo(down.Position));
        Assert.That(cancel.PreviousPressed, Is.True); Assert.That(cancel.Pressed, Is.False); Assert.That(cancel.IsConsumed, Is.True); Assert.That(cancel.ChangedToUp(), Is.False);
    }
    [Test] public void TouchPressureAndStylusIdentityComeFromNativeEvents()
    {
        Mount(); var point = Target.worldBound.center;
        var touch = new Touch { fingerId = 3, position = point, phase = TouchPhase.Began, pressure = .4f, maximumPossiblePressure = 1 };
        using (var evt = PointerDownEvent.GetPooled(touch, EventModifiers.Shift)) { evt.target = Target; Target.SendEvent(evt); }
        Flush(); var snapshot = Last(PointerEventType.Press); Assert.That(snapshot.Changes[0].Pressure, Is.EqualTo(.4f).Within(.001)); Assert.That(snapshot.KeyboardModifiers.IsShiftPressed, Is.True);
        UpTouch(3);
        var pen = new PenData { position = point, pressure = .7f, contactType = PenEventType.PenDown };
        using (var evt = PointerDownEvent.GetPooled(pen, EventModifiers.None)) { evt.target = Target; Target.SendEvent(evt); }
        Flush(); Assert.That(Last(PointerEventType.Press).Changes[0].Type, Is.EqualTo(PointerType.Stylus));
    }
    [Test] public void RightMouseButtonAndModifiersAreNotReportedAsPrimary()
    {
        Mount(); simulate.MouseDown(Target.worldBound.center, MouseButton.RightMouse, EventModifiers.Control | EventModifiers.Shift); Flush();
        var evt = Last(PointerEventType.Press); Assert.That(evt.Buttons.IsSecondaryPressed, Is.True); Assert.That(evt.Buttons.IsPrimaryPressed, Is.False);
        Assert.That(evt.KeyboardModifiers.IsCtrlPressed && evt.KeyboardModifiers.IsShiftPressed, Is.True);
        simulate.MouseUp(Target.worldBound.center, MouseButton.RightMouse); Flush();
    }
    [Test] public void NestedSnapshotsUseEachElementsLocalCoordinateSystem()
    {
        _nested = true; PointerEvent? parent = null, child = null;
        _parentBody = s => s.AwaitPointerEventScope(async e => parent = await e.AwaitPointerEvent());
        _body = s => s.AwaitPointerEventScope(async e => child = await e.AwaitPointerEvent());
        Mount(); Target.style.marginLeft = 25; Target.style.marginTop = 15; Flush(); DownTouch();
        Assert.That(parent!.Changes[0].Position - child!.Changes[0].Position, Is.EqualTo(new Offset(25, 15)));
    }
    [Test] public void KeyArrayIsCopiedAndComparedByValues()
    {
        Mount();
        var keys = new object?[] { 1, "a", null };
        var first = Modifier.PointerInput(keys: keys, block: Lifetime); keys[0] = 5;
        var equal = Modifier.PointerInput(keys: new object?[] { 1, "a", null }, block: Lifetime);
        var changed = Modifier.PointerInput(keys: keys, block: Lifetime);
        Assert.That(first.Equals(equal), Is.True); Assert.That(first.Equals(changed), Is.False);
        Assert.Throws<ArgumentException>(() => Modifier.PointerInput(keys: Array.Empty<object?>(), block: Lifetime));
        Assert.DoesNotThrow(() => Modifier.PointerInput(null, Lifetime).GetHashCode());
        Assert.DoesNotThrow(() => Modifier.PointerInput(key1: null, block: Lifetime).GetHashCode());
    }
    [UnityTest] public IEnumerator TooFastSecondDownDoesNotBecomeDoubleTap()
    {
        var doubles = 0; _body = s => s.DetectTapGestures(onDoubleTap: _ => doubles++, onTap: _ => _taps++);
        Mount(); MouseDown(); MouseUp(); MouseDown(); MouseUp(); yield return Delay(350);
        Assert.That(doubles, Is.Zero); Assert.That(_taps, Is.EqualTo(1));
    }
    [UnityTest] public IEnumerator CanceledSecondTapStillReportsSuccessfulFirstTap()
    {
        var doubles = 0; _body = s => s.DetectTapGestures(onDoubleTap: _ => doubles++, onTap: _ => _taps++);
        Mount(); MouseDown(); MouseUp(); yield return Delay(60); simulate.FrameUpdateMs(60); MouseDown();
        simulate.MouseMove(new Vector2(900, 900)); Flush(); MouseUp(); Assert.That(doubles, Is.Zero); Assert.That(_taps, Is.EqualTo(1));
    }
    [Test] public void TapWaitsUntilEveryFingerHasReleased()
    {
        _body = Tap; Mount(); DownTouch(0); DownTouch(1); UpTouch(0); Assert.That(_taps, Is.Zero); UpTouch(1); Assert.That(_taps, Is.EqualTo(1));
    }
    [Test] public void TransformCancellationStopsCallbacksUntilAllPointersAreUp()
    {
        var calls = 0; _nested = true;
        _parentBody = s => s.AwaitPointerEventScope(async e => { while (true) { var evt = await e.AwaitPointerEvent(PointerEventPass.Initial); if (evt.Type == PointerEventType.Move) evt.Changes[0].Consume(); } });
        _body = s => s.DetectTransformGestures((_, _, _, _) => calls++);
        Mount(); DownTouch(); Touch(TouchPhase.Moved, Target.worldBound.center + Vector2.right * 30); UpTouch(); Assert.That(calls, Is.Zero);
    }
    private IEnumerator Delay(int milliseconds)
    {
        var end = DateTime.UtcNow.AddMilliseconds(milliseconds);
        while (DateTime.UtcNow < end) { yield return null; Flush(); }
    }
    private sealed class RootNode(VisualElement element) : UnityReusableComposeNode(element), IReusableComposeNode { public new bool IsRoot => true; }
    private sealed class TestAdapter(ComposeInvalidator invalidator) : IComposeAdapter
    {
        private IComposeAdapter Native => ComposeUnityAdapter.Instance;
        public IFrameClock FrameClock => Native.FrameClock;
        public ComposeInvalidator ComposeInvalidatorInstance => invalidator;
        public IModifiersFactory ModifiersFactory => Native.ModifiersFactory;
        public IReusableNodeFactory NodeFactory => Native.NodeFactory;
        public IComposeLayouts Layouts => Native.Layouts;
        public IComposeLogger Logger => Native.Logger;
        public IEnterTransitionsFactory EnterTransitionsFactory => Native.EnterTransitionsFactory;
        public IExitTransitionsFactory ExitTransitionsFactory => Native.ExitTransitionsFactory;
        public IBrushFactory BrushFactory => Native.BrushFactory;
        public IDisposable StartTimeCoroutine(IEnumerable<TimeSpan?> coroutine) => Native.StartTimeCoroutine(coroutine);
        public int Framerate() => 60;
    }
    private sealed class UiContext : SynchronizationContext
    {
        private readonly Queue<(SendOrPostCallback Callback, object? State)> _queue = new();
        public override void Post(SendOrPostCallback callback, object? state) { lock (_queue) _queue.Enqueue((callback, state)); }
        internal void Drain()
        {
            for (var i = 0; i < 10000; i++)
            {
                (SendOrPostCallback Callback, object? State) work;
                lock (_queue) { if (_queue.Count == 0) return; work = _queue.Dequeue(); }
                work.Callback(work.State);
            }
            throw new AssertionException("UI continuations did not settle");
        }
    }
}
