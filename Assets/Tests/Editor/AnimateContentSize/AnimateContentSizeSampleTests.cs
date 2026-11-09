using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Compose.Net;
using Compose.Net.Unity.Packages.Compose.Net.Unity.Runtime.Adapters;
using Compose.Net.Unity.Samples.Behaviors.Content;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;
using UnityEditor;
using static Compose.Net.ComposeFunctions;

namespace Compose.Net.Unity.Samples.Tests;

public sealed partial class AnimateContentSizeSampleTests : UITestFixture
{
    private IComposer? _composer;
    private Adapter _adapter = null!;
    private RecordingIndication _indication = null!;
    [Composable] private Action _sample = null!;

    [TearDown] public void Cleanup()
    {
        _composer?.Dispose();
        simulate.ResetTimePerSimulatedFrameToDefault();
    }

    [Composable] private void Content()
    {
        _indication.Native = LocalIndication.Current;
        CompositionLocalProvider(LocalIndication.Provides(_indication), _sample);
    }

    [UnityTest] public IEnumerator QuickClicksRemainVisibleInRuntimeComposition() => QuickClicks(false);
    [UnityTest] public IEnumerator QuickClicksRemainVisibleInEditorPreview() => QuickClicks(true);

    private IEnumerator QuickClicks(bool preview)
    {
        panelSize = new Vector2(1200, 800);
        rootVisualElement.style.width = 1200; rootVisualElement.style.height = 800;
        simulate.timePerSimulatedFrameMs = 0;
        _adapter = new Adapter(); _indication = new RecordingIndication(); _composer = IComposer.Create();
        // Exercise the sample's actual composable rather than maintaining a second copy of it.
        var layout = typeof(AnimateContentSizeSample).GetMethod("Layout", BindingFlags.Static | BindingFlags.NonPublic)!;
        _sample = (Action)layout.CreateDelegate(typeof(Action));
        Bootstrap(_composer, _adapter, preview, new ComposeConfig(Logging: false), new Root(rootVisualElement), Content);
        Flush(); yield return null; Flush();
        var text = rootVisualElement.Q<Compose.Net.Unity.Text>();
        var host = text.parent.parent;
        for (var click = 0; click < 3; click++)
        {
            var before = text.text;
            simulate.MouseDown(host);
            var point = host.WorldToLocal(host.worldBound.center);
            yield return WaitFor(() => Math.Abs(_indication.Center.X - point.x) < 1 &&
                Math.Abs(_indication.Center.Y - point.y) < 1 && _indication.Scale == 0,
                "The press must reach the actual indication before advancing its clock");
            simulate.MouseUp(host);
            yield return WaitFor(() => _indication.Releases > click && text.text != before,
                "The release must reach the interaction source and toggle the sample");
            for (var frame = 0; frame < 3; frame++) { yield return null; Flush(); }
            Assert.That(text.text, Is.Not.EqualTo(before), "The actual sample must still toggle its text");
            _adapter.Clock.Advance(75); yield return null; Flush();
            Assert.That(_indication.Alpha, Is.GreaterThan(0.05f), "A quick click must remain visible after release");
            Assert.That(_indication.Scale, Is.GreaterThan(0));
            Assert.That(_indication.Size.Width, Is.GreaterThan(0));
            Assert.That(_indication.Size.Height, Is.GreaterThan(0));
            _adapter.Clock.Advance(400); yield return null; Flush();
            Assert.That(_indication.Alpha, Is.Zero, "The ripple must finish fading");
        }
    }

    private IEnumerator WaitFor(Func<bool> condition, string message)
    {
        var deadline = EditorApplication.timeSinceStartup + 2;
        do { yield return null; Flush(); }
        while (!condition() && EditorApplication.timeSinceStartup < deadline);
        Assert.That(condition(), Is.True, message);
    }

    private void Flush()
    {
        for (var i = 0; i < 8; i++) { _adapter.Invalidator.Tick(); simulate.FrameUpdateMs(0); }
    }

    private sealed partial class RecordingIndication : IIndication
    {
        internal IIndication Native = null!;
        internal float Alpha, Scale;
        internal Size Size;
        internal Offset Center;
        internal int Releases;
        [Composable] public IIndicationInstance RememberUpdatedInstance(IInteractionSource source)
        {
            LaunchedEffect(source, scope => source.Interactions.Collect(interaction =>
            {
                if (interaction is IPressInteraction.Release) Releases++;
            }, scope.Token));
            return new Instance(this, Native.RememberUpdatedInstance(source));
        }
        private sealed record Instance(RecordingIndication Owner, IIndicationInstance Inner) : IIndicationInstance
        {
            public void DrawIndication(IDrawScope scope)
            {
                (Owner.Center, Owner.Alpha, Owner.Scale) = RecordedRipple.LatestPress(Inner);
                Owner.Size = scope.Size;
                Inner.DrawIndication(scope);
            }
        }
    }

    private sealed class Clock : IFrameClock
    {
        private readonly List<TaskCompletionSource<TimeSpan>> _pending = new();
        public TimeSpan Now { get; private set; }
        public Task<TimeSpan> NextFrame(CancellationToken token)
        {
            var source = new TaskCompletionSource<TimeSpan>();
            token.Register(() => source.TrySetCanceled(token));
            _pending.Add(source); return source.Task;
        }
        internal void Advance(int milliseconds)
        {
            Now += TimeSpan.FromMilliseconds(milliseconds);
            var waiting = _pending.ToArray(); _pending.Clear();
            foreach (var source in waiting) source.TrySetResult(Now);
        }
    }
    private sealed class Root(VisualElement element) : UnityReusableComposeNode(element), IReusableComposeNode
    { public new bool IsRoot => true; }
    private sealed class Adapter : IComposeAdapter
    {
        internal readonly ComposeInvalidator Invalidator = new();
        internal readonly Clock Clock = new();
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
}
