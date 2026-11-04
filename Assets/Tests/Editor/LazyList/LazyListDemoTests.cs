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

[TestFixture("column"), TestFixture("row"), TestFixture("reverse-column"), TestFixture("reverse-row"), TestFixture("variable-column"), TestFixture("variable-row"), TestFixture("padding-column"), TestFixture("padding-row"), TestFixture("keys-column"), TestFixture("keys-row"), TestFixture("sticky-column"), TestFixture("sticky-row"), TestFixture("initial-column"), TestFixture("initial-row"), TestFixture("disabled"), TestFixture("massive"), TestFixture("nested"), TestFixture("short")]
public sealed partial class LazyListDemoTests : UITestFixture
{
    private readonly string _id;
    private LazyListDemoCase _case = null!;
    private IComposer _composer = null!;
    private DemoAdapter _adapter = null!;
    private SynchronizationContext? _previousContext;
    private readonly UiContext _context = new();
    public LazyListDemoTests(string id) => _id = id;
    [SetUp] public void Setup()
    {
        _previousContext = SynchronizationContext.Current; SynchronizationContext.SetSynchronizationContext(_context);
        panelSize = new Vector2(700, 500); simulate.timePerSimulatedFrameMs = 0;
        _adapter = new DemoAdapter(); _case = new LazyListDemoState().Cases.Single(c => c.Id == _id); _composer = IComposer.Create();
        Bootstrap(_composer, _adapter, false, new ComposeConfig(Logging: false), new RootNode(rootVisualElement), Content);
    }
    [Composable] private void Content() => LazyListDemo.Card(_case);
    [TearDown] public void Teardown()
    {
        _composer?.Dispose(); _context.Drain(); SynchronizationContext.SetSynchronizationContext(_previousContext);
        simulate.ResetTimePerSimulatedFrameToDefault();
    }
    private VisualElement E(string suffix) => rootVisualElement.Q("lazy." + _id + "." + suffix) ?? throw new AssertionException("Missing " + suffix);
    private void Flush(int milliseconds = 0)
    {
        SynchronizationContext.SetSynchronizationContext(_context); _context.Drain(); _adapter.Clock.Advance(milliseconds);
        for (var i = 0; i < 6; i++) { _context.Drain(); _adapter.Invalidator.Tick(); simulate.FrameUpdateMs(16); }
    }
    private IEnumerator Settle() { for (var i = 0; i < 12; i++) { yield return null; Flush(); } }
    private IEnumerator Click(string suffix)
    {
        var element = E(suffix); var picked = element.panel.Pick(element.worldBound.center);
        Assert.That(picked == element || element.Contains(picked), Is.True, "Covered demo button " + suffix + ": " + picked);
        simulate.Click(element); yield return Settle();
    }
    [UnityTest] public IEnumerator AllControlsDriveNativeList()
    {
        yield return Settle(); Assert.That(E("viewport").Q<ScrollView>(), Is.Not.Null);
        Assert.That(_case.Active.Value, Is.InRange(1, 45));
        yield return Click("jump"); Assert.That(_case.State.Value.FirstVisibleItemIndex, Is.InRange(Math.Max(0, _case.Count.Value / 2 - 4), _case.Count.Value / 2 + 1));
        yield return Click("start"); Assert.That(_case.State.Value.FirstVisibleItemIndex, Is.Zero);
        yield return Click("end"); Assert.That(_case.State.Value.CanScrollForward, Is.False);
        var old = _case.State.Value;
        yield return Click("replace"); Assert.That(_case.State.Value, Is.Not.SameAs(old));
        yield return Click("remove"); Assert.That(rootVisualElement.Q("lazy." + _id + ".viewport"), Is.Null);
        Assert.That(_case.Active.Value, Is.Zero);
        yield return Click("remove"); Assert.That(E("viewport").Q<ScrollView>(), Is.Not.Null);
    }
    [UnityTest] public IEnumerator TogglesAndDataChangesStayUsable()
    {
        yield return Settle(); var enabled = _case.Enabled.Value; var reverse = _case.Reverse.Value;
        yield return Click("enabled"); Assert.That(_case.Enabled.Value, Is.EqualTo(!enabled));
        yield return Click("reverse"); Assert.That(_case.Reverse.Value, Is.EqualTo(!reverse));
        yield return Click("resize"); Assert.That(_case.Viewport.Value, Is.EqualTo(90));
        yield return Click("size"); Assert.That(_case.ItemSize.Value, Is.EqualTo(78));
        yield return Click("prepend"); Assert.That(_case.Prefix.Value, Is.EqualTo(3));
        yield return Click("physics"); Assert.That(_case.Physics.Value, Is.False);
        yield return Click("count"); Assert.That(_case.State.Value.LayoutInfo.TotalItemsCount, Is.Zero);
        yield return Click("count"); Assert.That(_case.State.Value.LayoutInfo.TotalItemsCount, Is.EqualTo(1000));
        yield return Click("animate"); Flush(1000); yield return Settle();
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
