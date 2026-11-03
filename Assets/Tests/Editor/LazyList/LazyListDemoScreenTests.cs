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

public sealed partial class LazyListDemoScreenTests : UITestFixture
{
    private LazyListDemoState _demo = null!;
    private IComposer _composer = null!;
    private DemoAdapter _adapter = null!;
    private SynchronizationContext? _previousContext;
    private readonly UiContext _context = new();
    [SetUp] public void Setup()
    {
        _previousContext = SynchronizationContext.Current; SynchronizationContext.SetSynchronizationContext(_context);
        panelSize = new Vector2(1140, 700); simulate.timePerSimulatedFrameMs = 0;
        _adapter = new DemoAdapter(); _demo = new LazyListDemoState(); _composer = IComposer.Create();
        Bootstrap(_composer, _adapter, false, new ComposeConfig(Logging: false), new RootNode(rootVisualElement), Content);
    }
    [Composable] private void Content() => LazyListDemo.Screen(_demo);
    [TearDown] public void Teardown()
    {
        _composer?.Dispose(); _context.Drain(); SynchronizationContext.SetSynchronizationContext(_previousContext);
        simulate.ResetTimePerSimulatedFrameToDefault();
    }
    private void Flush()
    {
        SynchronizationContext.SetSynchronizationContext(_context);
        for (var i = 0; i < 6; i++) { _context.Drain(); _adapter.Invalidator.Tick(); simulate.FrameUpdateMs(16); }
    }
    private IEnumerator Settle() { for (var i = 0; i < 14; i++) { yield return null; Flush(); } }
    [UnityTest] public IEnumerator AllEighteenCardsFitAndBottomControlsAreReachable()
    {
        yield return Settle();
        Assert.That(rootVisualElement.Query<ScrollView>().ToList().Count, Is.GreaterThanOrEqualTo(19));
        var page = rootVisualElement.Q("lazy-demo").Children().OfType<ScrollView>().Single();
        Assert.That(page.verticalScroller.highValue, Is.GreaterThan(1500));
        var button = rootVisualElement.Q("lazy.short.count"); page.ScrollTo(button);
        yield return Settle();
        Assert.That(rootVisualElement.panel.Pick(button.worldBound.center), Is.SameAs(button));
        simulate.Click(button); yield return Settle();
        var state = _demo.Cases.Single(c => c.Id == "short").State.Value;
        Assert.That(state.LayoutInfo.TotalItemsCount, Is.Zero);
        foreach (var item in _demo.Cases)
        {
            var viewport = rootVisualElement.Q("lazy." + item.Id + ".viewport");
            Assert.That(viewport.layout.width, Is.EqualTo(310).Within(.6));
            Assert.That(item.State.Value.MaxValue, Is.LessThan(float.MaxValue));
        }
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
