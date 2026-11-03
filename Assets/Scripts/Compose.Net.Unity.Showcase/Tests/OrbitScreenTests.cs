using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Compose.Net;
using Compose.Net.Showcase;
using Compose.Net.Unity.Packages.Compose.Net.Unity.Runtime.Adapters;
using NUnit.Framework;
using UnityEditor.UIElements;
using UnityEditor.UIElements.TestFramework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;
using static Compose.Net.ComposeFunctions;

namespace Compose.Net.Unity.Showcase.Tests;

public sealed partial class OrbitScreenTests : UITestFixture
{
    private OrbitState _state = null!;
    private IComposer _composer = null!;
    private DemoAdapter _adapter = null!;
    private SynchronizationContext? _previousContext;
    private readonly UiContext _context = new();
    private readonly OrbitUnityHost _host = new();
    [SetUp] public void Setup()
    {
        _previousContext = SynchronizationContext.Current; SynchronizationContext.SetSynchronizationContext(_context);
        panelSize = new Vector2(1280, 900); simulate.timePerSimulatedFrameMs = 0;
        _adapter = new DemoAdapter(); _state = new OrbitState(); _composer = IComposer.Create();
        Bootstrap(_composer, _adapter, false, new ComposeConfig(Logging: false), new RootNode(rootVisualElement), Content);
    }
    [Composable] private void Content() => OrbitApp.Content(_state, _host);
    [TearDown] public void Teardown()
    {
        _composer?.Dispose(); _context.Drain(); SynchronizationContext.SetSynchronizationContext(_previousContext);
        simulate.ResetTimePerSimulatedFrameToDefault();
    }
    private void Flush()
    {
        SynchronizationContext.SetSynchronizationContext(_context);
        for (var i = 0; i < 6; i++) { _adapter.Clock.Advance(30); _context.Drain(); _adapter.Invalidator.Tick(); simulate.FrameUpdateMs(16); }
    }
    private IEnumerator Settle() { for (var i = 0; i < 12; i++) { yield return null; Flush(); } }
    private VisualElement Find(string tag) => rootVisualElement.Q(tag) ?? throw new AssertionException("Missing " + tag);
    private void Click(string tag)
    {
        var target = Find(tag);
        target.GetFirstAncestorOfType<ScrollView>()?.ScrollTo(target);
        Flush();
        Assert.That(rootVisualElement.panel.Pick(target.worldBound.center), Is.SameAs(target), "Control is not reachable: " + tag);
        simulate.Click(target);
    }

    [UnityTest] public IEnumerator AllScreensComposeAndDisposeAtDesktopAndCompactWidths()
    {
        yield return Settle();
        CapturePreview("overview");
        Assert.That(Find("orbit.root").layout.width, Is.EqualTo(1280).Within(2));
        foreach (var width in new[] { 1280, 760 })
        {
            panelSize = new Vector2(width, 900);
            foreach (var page in new[] { OrbitPage.Projects, OrbitPage.Activity, OrbitPage.Focus, OrbitPage.Motion, OrbitPage.Settings, OrbitPage.Overview })
            {
                Click("nav." + page); yield return Settle();
                var screen = Find("screen." + page);
                Assert.That(screen.layout.width, Is.GreaterThan(500));
                Assert.That(float.IsNaN(screen.layout.height), Is.False);
                Assert.That(screen.layout.height, Is.GreaterThan(400));
                Assert.That(screen.worldBound.xMax, Is.LessThanOrEqualTo(width + 1));
                Assert.That(screen.worldBound.yMax, Is.LessThanOrEqualTo(901));
                CapturePreview(page + "-" + width);
            }
        }
        _composer.Dispose(); yield return null;
        Assert.That(rootVisualElement.childCount, Is.Zero);
    }
    [UnityTest] public IEnumerator CreateSearchStarAndTaskFlowsUseRealControls()
    {
        yield return Settle(); Click("project.new"); yield return Settle();
        Find("project.name").Q<TextField>().value = "A new constellation";
        yield return Settle(); Click("project.create"); yield return Settle();
        Assert.That(_state.Projects.Size, Is.EqualTo(7)); Assert.That(_state.Route.Page, Is.EqualTo(OrbitPage.Project));
        Click("task.0"); yield return Settle(); Assert.That(_state.Projects[6].Completed, Is.EqualTo(1));
        Click("detail.star"); yield return Settle(); Assert.That(_state.Projects[6].Starred.Value, Is.True);
        Click("project.back"); yield return Settle();
        Find("projects.search").Q<TextField>().value = "CONSTELLATION"; yield return Settle();
        Assert.That(_state.VisibleProjects().Length, Is.EqualTo(1));
        Assert.That(Find("project.card." + _state.Projects[6].Id), Is.Not.Null);
        Find("projects.search").Q<TextField>().value = "no such project"; yield return Settle();
        Click("projects.clear"); yield return Settle(); Assert.That(_state.VisibleProjects().Length, Is.EqualTo(7));
    }
    [UnityTest] public IEnumerator CancelAndScrimPreserveProjectCountAndUnblockNavigation()
    {
        yield return Settle(); Click("project.new"); yield return Settle(); Click("project.cancel"); yield return Settle();
        Assert.That(_state.Projects.Size, Is.EqualTo(6)); Assert.That(rootVisualElement.Q("dialog.create"), Is.Null);
        Click("nav.Focus"); yield return Settle(); Assert.That(_state.Route.Page, Is.EqualTo(OrbitPage.Focus));
    }
    [UnityTest] public IEnumerator SharedProjectTransitionSurvivesFastBackAndAllTasksCanComplete()
    {
        yield return Settle(); Click("nav.Projects"); yield return Settle();
        Click("project.card.1"); Flush(); _state.Back(); Flush(); _state.OpenProject(_state.Projects[0]); yield return Settle();
        Assert.That(Find("detail.art").worldBound.width, Is.GreaterThan(400));
        Click("task.3"); Click("task.4"); yield return Settle(); Assert.That(_state.Projects[0].Progress, Is.EqualTo(1));
    }
    [UnityTest] public IEnumerator SettingsDelegatedStateAndParticleControlsAreLive()
    {
        yield return Settle(); Click("nav.Settings"); yield return Settle();
        Click("settings.reduced"); yield return Settle(); Assert.That(_state.ReducedMotion.Value, Is.True);
        Click("accent.2"); yield return Settle(); Assert.That(_state.AccentIndex.Value, Is.EqualTo(2));
        Click("nav.Motion"); yield return Settle(); Click("particles.144"); Click("pattern.2"); yield return Settle();
        Assert.That(_state.ParticleCount.Value, Is.EqualTo(144)); Assert.That(_state.MotionPattern.Value, Is.EqualTo(2));
        Assert.That(Find("motion.canvas").layout.height, Is.EqualTo(300).Within(1));
    }
    private float _fillAlpha = -1, _strokeAlpha = -1, _strokeRed = -1;
    [UnityTest] public IEnumerator DrawingMultipliesAlphaAndSetsStrokeColor()
    {
        _composer.Dispose(); rootVisualElement.Clear();
        Bootstrap(_composer, _adapter, false, new ComposeConfig(Logging: false), new RootNode(rootVisualElement), DrawingProbe);
        yield return Settle();
        Assert.That(_fillAlpha, Is.EqualTo(.2f).Within(.001));
        Assert.That(_strokeAlpha, Is.EqualTo(.2f).Within(.001));
        Assert.That(_strokeRed, Is.EqualTo(.7f).Within(.001));
    }
    [Composable] private void DrawingProbe() => Box(Modifier.Size(100.Dp()).DrawBehind<MeshGenerationContext>(scope =>
    {
        scope.DrawCircle(new Compose.Net.Color(.2f, .4f, .6f, .4f), radius: 20, alpha: .5f);
        _fillAlpha = scope.Context.painter2D.fillColor.a;
        scope.DrawOval(new Compose.Net.Color(.7f, .3f, .2f, .8f), size: new Size(40, 20), alpha: .25f, style: Stroke(2));
        _strokeAlpha = scope.Context.painter2D.strokeColor.a;
        _strokeRed = scope.Context.painter2D.strokeColor.r;
    }));
    private void CapturePreview(string name)
    {
        var directory = Environment.GetEnvironmentVariable("ORBIT_CAPTURE_DIR");
        if (string.IsNullOrEmpty(directory)) return;
        System.IO.Directory.CreateDirectory(directory);
        rootVisualElement.CaptureToPNG(System.IO.Path.Combine(directory, name + ".png"));
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
