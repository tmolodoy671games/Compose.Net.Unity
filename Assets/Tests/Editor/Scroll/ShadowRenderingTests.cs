using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Object = UnityEngine.Object;
using System.Collections;
using Compose.Net;
using Compose.Net.Unity.Packages.Compose.Net.Unity.Runtime.Adapters;
using Compose.Net.Unity.Samples.Behaviors.Content;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;
using static Compose.Net.ComposeFunctions;
using Color = Compose.Net.Color;

namespace Compose.Net.Unity.Tests;

public sealed partial class ShadowRenderingTests : UITestFixture
{
    private IComposer _composer = null!;
    private DemoAdapter _adapter = null!;
    private bool _inset;
    [SetUp] public void Setup() { panelSize = new Vector2(300, 240); simulate.needsRendering = true; }
    [TearDown] public void Teardown() => _composer?.Dispose();
    [Composable] private void Content()
    {
        var shape = RoundedCornerShape(16.Dp());
        var red = Shadow(8.Dp(), 0.Dp(), new Color(1, 0, 0, 1), new Offset(-7, -7));
        var blue = Shadow(8.Dp(), 0.Dp(), new Color(0, 0, 1, 1), new Offset(7, 7));
        var modifier = Modifier.Size(120.Dp(), 100.Dp()).Background(new Color(.5f, .5f, .5f, 1), shape);
        modifier = _inset ? modifier.InnerShadow(shape, red).InnerShadow(shape, blue)
            : modifier.DropShadow(shape, red).DropShadow(shape, blue);
        Box(modifier: Modifier.FillMaxSize().Background(Color.White), contentAlignment: Alignment.Center,
            content: () => Box(modifier: modifier.TestTag("shadow"), content: () => { }));
    }
    private IEnumerator Mount(bool inset)
    {
        _inset = inset; _adapter = new DemoAdapter(); _composer = IComposer.Create();
        Bootstrap(_composer, _adapter, false, new ComposeConfig(Logging: false), new RootNode(rootVisualElement), Content);
        for (var i = 0; i < 12; i++) { yield return null; _adapter.Invalidator.Tick(); simulate.FrameUpdate(); }
    }
    [UnityTest] public IEnumerator OuterLayersRenderBeyondShapedBackgroundWithoutTintingCenter()
    {
        yield return Mount(false);
        Assert.That(rootVisualElement.Q("shadow").resolvedStyle.overflow, Is.EqualTo(Overflow.Visible));
        var pixels = Capture();
        try
        {
            var left = pixels.GetPixel(84, 120); var right = pixels.GetPixel(216, 120);
            Assert.That(left.r - left.b, Is.GreaterThan(.1f), "Red shadow outside left edge");
            Assert.That(right.b - right.r, Is.GreaterThan(.1f), "Blue shadow outside right edge");
            var center = pixels.GetPixel(150, 120);
            Assert.That(center.r, Is.EqualTo(center.b).Within(.02f), "Shadow must not tint its native background");
        }
        finally { Object.DestroyImmediate(pixels); }
    }
    [UnityTest] public IEnumerator ZeroSpreadInsetKeepsBothColorsAndDoesNotClipChildren()
    {
        yield return Mount(true);
        Assert.That(rootVisualElement.Q("shadow").resolvedStyle.overflow, Is.EqualTo(Overflow.Visible));
        var pixels = Capture();
        try
        {
            var left = pixels.GetPixel(94, 120); var right = pixels.GetPixel(206, 120);
            Assert.That(left.b - left.r, Is.GreaterThan(.1f), "Blue inner edge");
            Assert.That(right.r - right.b, Is.GreaterThan(.1f), "Red inner edge");
        }
        finally { Object.DestroyImmediate(pixels); }
    }
    private Texture2D Capture()
    {
        var render = rootVisualElement.CaptureToRenderTexture(); var previous = RenderTexture.active;
        var texture = new Texture2D(render.width, render.height, TextureFormat.RGBA32, false);
        try { RenderTexture.active = render; texture.ReadPixels(new UnityEngine.Rect(0, 0, render.width, render.height), 0, 0); texture.Apply(); return texture; }
        finally { RenderTexture.active = previous; render.Release(); Object.DestroyImmediate(render); }
    }
    private sealed class RootNode(VisualElement element) : UnityReusableComposeNode(element), IReusableComposeNode
    { public new bool IsRoot => true; }
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
