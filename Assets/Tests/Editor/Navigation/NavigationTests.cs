using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

public sealed partial class NavigationTests : UITestFixture
{
    private IComposer _composer = null!;
    private TestAdapter _adapter = null!;
    private SynchronizationContext? _previous;
    private readonly UiContext _context = new();

    [SetUp] public void Mount()
    {
        _previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(_context);
        panelSize = new Vector2(1000, 700); simulate.timePerSimulatedFrameMs = 0;
        rootVisualElement.style.width = 1000; rootVisualElement.style.height = 700;
        _adapter = new TestAdapter(); _composer = IComposer.Create();
        Bootstrap(_composer, _adapter, false, new ComposeConfig(Logging: false), new RootNode(rootVisualElement), NavigationSample.Layout);
        Flush();
    }
    [TearDown] public void Unmount()
    {
        _composer?.Dispose(); _context.Drain(); SynchronizationContext.SetSynchronizationContext(_previous);
        simulate.ResetTimePerSimulatedFrameToDefault();
    }
    private void Flush(int milliseconds = 0)
    {
        SynchronizationContext.SetSynchronizationContext(_context); _context.Drain(); _adapter.Clock.Advance(milliseconds);
        for (var i = 0; i < 8; i++) { _context.Drain(); _adapter.Invalidator.Tick(); simulate.FrameUpdateMs(0); }
    }
    private static bool Visible(VisualElement e)
    {
        for (var p = e; p != null; p = p.parent)
            if (p.resolvedStyle.display == DisplayStyle.None || p.resolvedStyle.visibility == Visibility.Hidden || p.resolvedStyle.opacity <= 0) return false;
        return e.worldBound.width > 0 && e.worldBound.height > 0;
    }
    private VisualElement E(string tag) => rootVisualElement.Query<VisualElement>("nav." + tag).ToList().Single(Visible);
    private string Label(string tag) => E(tag) is TextElement text ? text.text : E(tag).Q<TextElement>().text;
    private void Screen(string title, int depth)
    {
        Assert.That(Label("title"), Is.EqualTo(title)); Assert.That(Label("stack"), Is.EqualTo("Стек: " + depth));
    }
    private IEnumerator Click(string tag)
    {
        var e = E(tag); simulate.MouseDown(e); yield return null; Flush(); simulate.MouseUp(e);
        for (var i = 0; i < 10; i++) { yield return null; Flush(100); }
    }
    [Test] public void HomeShowsAllLabelsAndButtonsWithinThePanel()
    {
        Screen("Home", 1);
        foreach (var tag in new[] { "visits", "products", "settings" })
        {
            var e = E(tag); Assert.That(Label(tag), Is.Not.Empty);
            Assert.That(e.worldBound.xMin, Is.GreaterThanOrEqualTo(0)); Assert.That(e.worldBound.xMax, Is.LessThanOrEqualTo(1000));
            Assert.That(e.worldBound.yMin, Is.GreaterThanOrEqualTo(0)); Assert.That(e.worldBound.yMax, Is.LessThanOrEqualTo(700));
        }
        Assert.That(Label("visits"), Is.EqualTo("Визитов: 0"));
        Assert.That(rootVisualElement.Q("nav.back"), Is.Null);
    }
    [UnityTest] public IEnumerator CounterSurvivesProductNavigationAndEveryBackClick()
    {
        yield return Click("visits"); yield return Click("visits"); Assert.That(Label("visits"), Is.EqualTo("Визитов: 2"));
        yield return Click("products"); Screen("Товары", 2);
        yield return Click("product-compose"); Screen("Compose.Net", 3);
        yield return Click("next-product"); Screen("Compose.Net +", 4);
        yield return Click("back"); Screen("Compose.Net", 3);
        yield return Click("back"); Screen("Товары", 2);
        yield return Click("back"); Screen("Home", 1); Assert.That(Label("visits"), Is.EqualTo("Визитов: 2"));
    }
    [UnityTest] public IEnumerator SettingsToggleUpdatesAndPoppedSettingsStartFresh()
    {
        yield return Click("settings"); Screen("Настройки", 2);
        Assert.That(Label("theme"), Is.EqualTo("Тема: тёмная")); yield return Click("theme");
        Assert.That(Label("theme"), Is.EqualTo("Тема: светлая")); yield return Click("back"); Screen("Home", 1);
        yield return Click("settings"); Assert.That(Label("theme"), Is.EqualTo("Тема: тёмная"));
    }
    [UnityTest] public IEnumerator EveryProductButtonOpensItsOwnRoute()
    {
        yield return Click("products");
        foreach (var item in new[] { ("compose", "Compose.Net"), ("nav3", "Navigation 3"), ("uitk", "Unity UI Toolkit") })
        {
            yield return Click("product-" + item.Item1); Screen(item.Item2, 3);
            yield return Click("back"); Screen("Товары", 2);
        }
    }
    [Test] public void BackgroundDrawsBeforeNativeTextAndIndicationDrawsLast()
    {
        var order = new List<string>();
        var element = new VisualElement { generateVisualContent = _ => order.Add("text") };
        var type = typeof(UnityReusableComposeNode).Assembly.GetType("Compose.Net.Unity.GenerateVisualContent", true)!;
        var drawing = Activator.CreateInstance(type, element)!;
        var insert = type.GetMethod("Insert")!;
        Action<MeshGenerationContext> background = _ => order.Add("background");
        Action<MeshGenerationContext> second = _ => order.Add("second-background");
        insert.Invoke(drawing, new object[] { 0, background }); insert.Invoke(drawing, new object[] { 1, second });
        type.GetProperty("OnTop")!.SetValue(drawing, (Action<MeshGenerationContext>)(_ => order.Add("indication")));
        var invoke = type.GetMethod("Invoke", BindingFlags.NonPublic | BindingFlags.Instance)!;
        element.generateVisualContent = (Action<MeshGenerationContext>)invoke.CreateDelegate(typeof(Action<MeshGenerationContext>), drawing);
        element.style.position = Position.Absolute;
        element.style.width = 100; element.style.height = 100;
        rootVisualElement.Add(element); Flush();
        order.Clear(); element.MarkDirtyRepaint(); simulate.FrameUpdateMs(0);
        Assert.That(order, Is.EqualTo(new[] { "background", "second-background", "text", "indication" }));
        order.Clear(); type.GetMethod("Remove")!.Invoke(drawing, new object[] { background });
        element.MarkDirtyRepaint(); simulate.FrameUpdateMs(0);
        Assert.That(order, Is.EqualTo(new[] { "second-background", "text", "indication" }));
    }
    private sealed class RootNode(VisualElement element) : UnityReusableComposeNode(element), IReusableComposeNode { public new bool IsRoot => true; }
    private sealed class TestAdapter : IComposeAdapter
    {
        internal readonly TestClock Clock = new(); internal readonly ComposeInvalidator Invalidator = new();
        private IComposeAdapter Native => ComposeUnityAdapter.Instance;
        public IFrameClock FrameClock => Clock;
        public ComposeInvalidator ComposeInvalidatorInstance => Invalidator;
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
    private sealed class TestClock : IFrameClock
    {
        private readonly List<TaskCompletionSource<TimeSpan>> _pending = new();
        public TimeSpan Now { get; private set; }
        public Task<TimeSpan> NextFrame(CancellationToken token)
        {
            var source = new TaskCompletionSource<TimeSpan>(); var registration = token.Register(() => source.TrySetCanceled(token));
            _pending.Add(source); return Await(source.Task, registration);
        }
        private static async Task<TimeSpan> Await(Task<TimeSpan> task, CancellationTokenRegistration registration)
        { try { return await task; } finally { registration.Dispose(); } }
        internal void Advance(int milliseconds)
        {
            Now += TimeSpan.FromMilliseconds(milliseconds); var pending = _pending.ToArray(); _pending.Clear();
            foreach (var source in pending) source.TrySetResult(Now);
        }
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
