using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Compose.Net;
using Compose.Net.MaterialDemo;
using Compose.Net.Unity.Packages.Compose.Net.Unity.Runtime.Adapters;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static Compose.Net.ComposeFunctions;

namespace Compose.Net.Unity.MaterialDemo.Editor;

public sealed partial class MaterialDesignWindow : EditorWindow
{
    private IComposer? _composer;
    private readonly EditorAdapter _adapter = new();
    [MenuItem("Tools/Compose.Net/Material Design")]
    public static void Open()
    {
        var window = GetWindow<MaterialDesignWindow>();
        window.titleContent = new GUIContent("Material Design");
        window.minSize = new Vector2(680, 620);
        window.Show();
    }
    private void CreateGUI()
    {
        _composer?.Dispose(); rootVisualElement.Clear();
        _composer = IComposer.Create();
        Bootstrap(_composer, _adapter, false, new ComposeConfig(Logging: false), new Root(rootVisualElement), Content);
    }
    [Composable] private void Content() => MaterialDesignLab.Content();
    private void Update() { ComposeInvalidatorHolder.ComposeInvalidator.Tick(); Repaint(); }
    private void OnDisable() { _composer?.Dispose(); _composer = null; }
    private sealed class Root(VisualElement element) : UnityReusableComposeNode(element), IReusableComposeNode
    { public new bool IsRoot => true; }
    // MonoBehaviour coroutines do not advance outside Play Mode. Drive the same
    // Compose animation contract from the editor update loop instead.
    [global::Unity.Scripting.LifecycleManagement.NoAutoStaticsCleanup]
    private sealed class EditorAdapter : IComposeAdapter
    {
        // Adapter selection is shared by Compose hosts. An animation started in
        // another window must keep receiving frames after this window closes.
        private static readonly EditorClock Clock = new();
        private IComposeAdapter Native => ComposeUnityAdapter.Instance;
        public IFrameClock FrameClock => Clock;
        public IDensity Density => Native.Density;
        public ComposeInvalidator? ComposeInvalidatorInstance => Native.ComposeInvalidatorInstance;
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
    private sealed class EditorClock : IFrameClock
    {
        private readonly Stopwatch _watch = Stopwatch.StartNew();
        private readonly List<TaskCompletionSource<TimeSpan>> _pending = new();
        public EditorClock() => EditorApplication.update += Tick;
        public TimeSpan Now => _watch.Elapsed;
        public Task<TimeSpan> NextFrame(CancellationToken token)
        {
            if (token.IsCancellationRequested) return Task.FromCanceled<TimeSpan>(token);
            var source = new TaskCompletionSource<TimeSpan>();
            var registration = token.Register(() => source.TrySetCanceled(token));
            _pending.Add(source);
            return Await(source.Task, registration);
        }
        private static async Task<TimeSpan> Await(Task<TimeSpan> task, CancellationTokenRegistration registration)
        { try { return await task; } finally { registration.Dispose(); } }
        internal void Tick()
        {
            if (_pending.Count == 0) return;
            var batch = _pending.ToArray(); _pending.Clear(); var time = Now;
            foreach (var source in batch) source.TrySetResult(time);
        }
    }

}
