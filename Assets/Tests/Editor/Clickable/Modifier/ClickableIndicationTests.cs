using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Compose.Net.Unity.Packages.Compose.Net.Unity.Runtime.Adapters;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;
using static Compose.Net.ComposeFunctions;
using Color = Compose.Net.Color;

namespace Compose.Net.Unity.Tests;

[TestFixture(false, false, false), TestFixture(false, true, false), TestFixture(true, false, false), TestFixture(true, true, false),
 TestFixture(false, false, true), TestFixture(false, true, true)]
public sealed partial class ClickableIndicationTests : UITestFixture
{
    private readonly bool shortOverload, row, overlay;
    public ClickableIndicationTests(bool shortOverload, bool row, bool overlay) { this.shortOverload = shortOverload; this.row = row; this.overlay = overlay; }
    private IComposer _composer = null!;
    private TestAdapter _adapter = null!;
    private ProbeIndication _probe = null!;
    private IMutableInteractionSource _source = null!;
    private readonly UiContext _context = new();
    private SynchronizationContext? _previous;
    private int _clicks;

    [SetUp] public void Mount()
    {
        _previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(_context);
        RecreatePanel(); panelSize = new Vector2(500, 300); simulate.timePerSimulatedFrameMs = 0;
        _adapter = new TestAdapter(); _source = MutableInteractionSource(); _clicks = 0;
        _composer = IComposer.Create();
        simulate.MouseMove(new Vector2(490, 290));
        Bootstrap(_composer, _adapter, false, new ComposeConfig(Logging: false), new Root(rootVisualElement), Content);
    }
    [TearDown] public void Unmount()
    {
        _composer.Dispose(); _context.Drain();
        SynchronizationContext.SetSynchronizationContext(_previous);
        simulate.ResetTimePerSimulatedFrameToDefault();
    }
    private void Click() => _clicks++;
    [Composable] private void Content()
    {
        var indication = LocalIndication.Current;
        _probe = Remember(() => new ProbeIndication(indication));
        CompositionLocalProvider(LocalIndication.Provides(_probe), () =>
        {
            if (overlay)
            {
                Box(modifier: Modifier.Size(180.Dp(), 80.Dp()).Background(Color.DodgerBlue), content: () =>
                {
                    Text("Clickable", color: Color.White);
                    var layer = Modifier.MatchParentSize().Clip(RoundedCornerShape(20.Dp())).TestTag("indication").Clickable(Click, _probe, _source);
                    if (row) Row(modifier: layer, content: () => { });
                    else Box(modifier: layer);
                });
                return;
            }
            var modifier = Remember(() => shortOverload
                ? Modifier.Size(180.Dp(), 80.Dp()).Background(Color.DodgerBlue).TestTag("indication").Clickable(Click)
                : Modifier.Size(180.Dp(), 80.Dp()).Background(Color.DodgerBlue).TestTag("indication").Clickable(Click, _probe, _source));
            if (row) Row(modifier: modifier, content: () => Text("Clickable", color: Color.White));
            else Box(modifier: modifier, content: () => Text("Clickable", color: Color.White));
        });
    }
    private void Frame()
    {
        SynchronizationContext.SetSynchronizationContext(_context);
        _context.Drain(); _adapter.Invalidator.Tick(); _adapter.Clock.Advance();
        _context.Drain(); _adapter.Invalidator.Tick(); simulate.FrameUpdateMs(16);
    }
    private IEnumerator Until(Func<bool> condition, string message)
    {
        for (var i = 0; i < 90; i++) { yield return null; Frame(); if (condition()) yield break; }
        Assert.Fail(message);
    }
    [UnityTest] public IEnumerator DefaultIndicationDrawsHoverPressAndQuickTapOnNativePanel()
    {
        yield return Until(() => _probe.Draws > 0, "Initial indication must reach native drawing");
        var target = rootVisualElement.Q("indication");
        simulate.MouseMove(target);
        yield return Until(() => _probe.Hover > 0, "Mouse hover must draw a visible highlight");
        simulate.MouseDown(target);
        yield return Until(() => _probe.Press > 0, "Held press must draw a visible ripple");
        Assert.That(_clicks, Is.Zero);
        simulate.MouseUp(target);
        yield return Until(() => _clicks == 1, "Indication recomposition must preserve the click gesture");
        simulate.MouseMove(new Vector2(490, 290));
        yield return Until(() => _probe.Hover == 0 && _probe.Press == 0, "Exit and release must fade out");
        var pressDraws = _probe.PressDraws;
        simulate.Click(target);
        yield return Until(() => _probe.PressDraws > pressDraws, "A quick tap must reach native drawing");
        Assert.That(_clicks, Is.EqualTo(2));
    }
    private sealed partial class ProbeIndication : IIndication
    {
        private readonly IIndication inner;
        internal ProbeIndication(IIndication inner) => this.inner = inner;
        internal int Draws, PressDraws;
        internal float Hover, Press;
        [Composable] public IIndicationInstance RememberUpdatedInstance(IInteractionSource source) =>
            new ProbeInstance(this, inner.RememberUpdatedInstance(source));
        private sealed class ProbeInstance(ProbeIndication owner, IIndicationInstance ripple) : IIndicationInstance
        {
            public void DrawIndication(IDrawScope scope)
            {
                ripple.DrawIndication(scope);
                owner.Draws++;
                owner.Hover = Read("HoverAlpha");
                owner.Press = 0;
                foreach (var frame in (Array)ripple.GetType().GetProperty("Presses")!.GetValue(ripple)!)
                {
                    var scale = (float)frame.GetType().GetProperty("Scale")!.GetValue(frame)!;
                    var alpha = (float)frame.GetType().GetProperty("Alpha")!.GetValue(frame)!;
                    if (scale > 0) owner.Press = Math.Max(owner.Press, alpha);
                }
                if (owner.Press > 0) owner.PressDraws++;
            }
            private float Read(string property) => (float)ripple.GetType().GetProperty(property)!.GetValue(ripple)!;
        }
    }
    private sealed class Root(VisualElement element) : UnityReusableComposeNode(element), IReusableComposeNode { public new bool IsRoot => true; }
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
            var completion = new TaskCompletionSource<TimeSpan>();
            token.Register(() => completion.TrySetCanceled(token)); _pending.Add(completion); return completion.Task;
        }
        internal void Advance()
        {
            Now += TimeSpan.FromMilliseconds(16); var pending = _pending.ToArray(); _pending.Clear();
            foreach (var item in pending) item.TrySetResult(Now);
        }
    }
    private sealed class UiContext : SynchronizationContext
    {
        private readonly System.Collections.Concurrent.ConcurrentQueue<Action> _queue = new();
        public override void Post(SendOrPostCallback callback, object? state) => _queue.Enqueue(() => callback(state));
        internal void Drain() { while (_queue.TryDequeue(out var callback)) callback(); }
    }
}
