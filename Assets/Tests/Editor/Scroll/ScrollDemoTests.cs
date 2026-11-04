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

namespace Compose.Net.Unity.Tests;

[TestFixture("vertical"), TestFixture("horizontal"), TestFixture("reverse-v"), TestFixture("reverse-h"),
 TestFixture("disabled-v"), TestFixture("disabled-h"), TestFixture("initial-v"), TestFixture("initial-h"),
 TestFixture("dynamic-v"), TestFixture("dynamic-h"), TestFixture("short"), TestFixture("empty"),
 TestFixture("no-fling"), TestFixture("clamped"), TestFixture("row"), TestFixture("column"),
 TestFixture("nested-v"), TestFixture("nested-h")]
public sealed partial class ScrollDemoTests : UITestFixture
{
    private readonly string _id;
    private ScrollDemoCase _case = null!;
    private IComposer _composer = null!;
    private DemoAdapter _adapter = null!;
    private SynchronizationContext? _previousContext;
    private readonly UiContext _context = new();
    public ScrollDemoTests(string id) => _id = id;
    [SetUp] public void Setup()
    {
        _previousContext = SynchronizationContext.Current; SynchronizationContext.SetSynchronizationContext(_context);
        panelSize = new Vector2(700, 500); simulate.timePerSimulatedFrameMs = 0;
        _adapter = new DemoAdapter(); _case = new ScrollDemoState()[_id]; _composer = IComposer.Create();
        Bootstrap(_composer, _adapter, false, new ComposeConfig(Logging: false), new RootNode(rootVisualElement), Content);
    }
    [Composable] private void Content() => ScrollDemo.Card(_case);
    [TearDown] public void Teardown()
    {
        _composer?.Dispose(); _context.Drain(); SynchronizationContext.SetSynchronizationContext(_previousContext);
        simulate.ResetTimePerSimulatedFrameToDefault();
    }
    private VisualElement E(string suffix) => rootVisualElement.Q("scroll." + _id + "." + suffix) ?? throw new AssertionException("Missing " + suffix);
    private void Flush(int milliseconds = 0)
    {
        SynchronizationContext.SetSynchronizationContext(_context); _context.Drain(); _adapter.Clock.Advance(milliseconds);
        for (var i = 0; i < 6; i++) { _context.Drain(); _adapter.Invalidator.Tick(); simulate.FrameUpdateMs(16); }
    }
    private IEnumerator Settle() { for (var i = 0; i < 5; i++) { yield return null; Flush(); } }
    private IEnumerator Click(string suffix)
    {
        var element = E(suffix); var picked = element.panel.Pick(element.worldBound.center);
        Assert.That(picked == element || element.Contains(picked), Is.True, "Covered demo button " + suffix + ": " + picked);
        simulate.Click(element); yield return Settle();
    }
    [UnityTest] public IEnumerator ControlsDriveRealNativeView()
    {
        yield return Settle(); Assert.That(E("viewport").Children().OfType<ScrollView>().FirstOrDefault(), Is.Not.Null);
        yield return Click("end"); Assert.That(_case.State.Value.Value, Is.EqualTo(_case.State.Value.MaxValue).Within(.6));
        yield return Click("start"); Assert.That(_case.State.Value.Value, Is.Zero);
        yield return Click("by"); Assert.That(_case.State.Value.Value, Is.EqualTo(Math.Min(60, _case.State.Value.MaxValue)).Within(.6));
        var old = _case.State.Value;
        yield return Click("replace"); Assert.That(_case.State.Value, Is.Not.SameAs(old));
        yield return Click("remove"); Assert.That(E("viewport").Children().OfType<ScrollView>().FirstOrDefault(), Is.Null);
        yield return Click("remove"); Assert.That(E("viewport").Children().OfType<ScrollView>().FirstOrDefault(), Is.Not.Null);
    }
    [UnityTest] public IEnumerator TogglesResizeAndAnimationRemainUsable()
    {
        yield return Settle(); var enabled = _case.Enabled.Value; var reverse = _case.Reverse.Value;
        yield return Click("enabled"); Assert.That(_case.Enabled.Value, Is.EqualTo(!enabled));
        yield return Click("reverse"); Assert.That(_case.Reverse.Value, Is.EqualTo(!reverse));
        yield return Click("resize"); Assert.That(_case.Viewport.Value, Is.EqualTo(85));
        yield return Click("count"); yield return Click("animate"); Flush(1000); yield return Settle();
        Assert.That(_case.State.Value.Value, Is.InRange(0, _case.State.Value.MaxValue));
        Assert.That(_case.State.Value.IsScrollInProgress, Is.False);
    }
    private sealed class UiContext : SynchronizationContext
    {
        private readonly Queue<(SendOrPostCallback Callback, object? State)> _queue = new();
        public override void Post(SendOrPostCallback callback, object? state)
        { lock (_queue) _queue.Enqueue((callback, state)); }
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
    private sealed class RootNode(VisualElement element) : UnityReusableComposeNode(element), IReusableComposeNode { public new bool IsRoot => true; }
    private sealed class DemoAdapter : IComposeAdapter
    {
        internal readonly TestClock Clock=new(); internal readonly ComposeInvalidator Invalidator=new();
        private IComposeAdapter Native => ComposeUnityAdapter.Instance;
        public IFrameClock FrameClock=>Clock;
        public ComposeInvalidator ComposeInvalidatorInstance=>Invalidator;
        public IModifiersFactory ModifiersFactory=>Native.ModifiersFactory;
        public IReusableNodeFactory NodeFactory=>Native.NodeFactory;
        public IComposeLayouts Layouts => Native.Layouts;
        public IComposeLogger Logger=>Native.Logger;
        public IEnterTransitionsFactory EnterTransitionsFactory=>Native.EnterTransitionsFactory;
        public IExitTransitionsFactory ExitTransitionsFactory=>Native.ExitTransitionsFactory;
        public IBrushFactory BrushFactory=>Native.BrushFactory;
        public IDisposable StartTimeCoroutine(IEnumerable<TimeSpan?> coroutine)=>Native.StartTimeCoroutine(coroutine);
        public int Framerate()=>60;
    }
    private sealed class TestClock : IFrameClock
    {
        private readonly List<TaskCompletionSource<TimeSpan>> _pending=new();
        public TimeSpan Now {get; private set;}
        public Task<TimeSpan> NextFrame(CancellationToken token)
        {
            var source=new TaskCompletionSource<TimeSpan>(); var registration=token.Register(()=>source.TrySetCanceled(token));
            _pending.Add(source); return Await(source.Task,registration);
        }
        private static async Task<TimeSpan> Await(Task<TimeSpan> task,CancellationTokenRegistration registration)
        { try{return await task;} finally{registration.Dispose();} }
        internal void Advance(int milliseconds)
        {
            Now+=TimeSpan.FromMilliseconds(milliseconds); var pending=_pending.ToArray(); _pending.Clear();
            foreach(var source in pending) source.TrySetResult(Now);
        }
    }
}
