using System;
using System.Collections.Generic;
using Compose.Net.Unity.Packages.Compose.Net.Unity.Runtime.Adapters;
using UnityEditor;

namespace Compose.Net.Unity.Preview;

// Each preview owns its update loop. No MonoBehaviour coroutine or Play Mode is needed.
internal sealed class UnityPreviewAdapter : IComposeAdapter, IDisposable
{
    private readonly List<EditorCoroutine> _coroutines = new();
    private bool _disposed;
    private IComposeAdapter Native => ComposeUnityAdapter.Instance;
    public IDensity Density => Native.Density;
    public ComposeInvalidator? ComposeInvalidatorInstance { get; } = new ComposeInvalidator();
    public IModifiersFactory ModifiersFactory => Native.ModifiersFactory;
    public IReusableNodeFactory NodeFactory => Native.NodeFactory;
    public IComposeLayouts Layouts => Native.Layouts;
    public IComposeLogger Logger => Native.Logger;
    public IEnterTransitionsFactory EnterTransitionsFactory => Native.EnterTransitionsFactory;
    public IExitTransitionsFactory ExitTransitionsFactory => Native.ExitTransitionsFactory;
    public IBrushFactory BrushFactory => Native.BrushFactory;
    public int Framerate() => 60;

    public IDisposable StartTimeCoroutine(IEnumerable<TimeSpan?> coroutine)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(UnityPreviewAdapter));
        var task = new EditorCoroutine(coroutine.GetEnumerator());
        _coroutines.Add(task);
        // Match StartCoroutine: execute until the first yield immediately.
        task.Tick(EditorApplication.timeSinceStartup);
        return task;
    }

    public void Tick()
    {
        foreach (var task in _coroutines.ToArray()) task.Tick(EditorApplication.timeSinceStartup);
        _coroutines.RemoveAll(task => task.IsDisposed);
        ComposeInvalidatorInstance!.Tick();
    }

    public void Dispose()
    {
        _disposed = true;
        foreach (var task in _coroutines.ToArray()) task.Dispose();
        _coroutines.Clear();
    }

    private sealed class EditorCoroutine : IDisposable
    {
        private readonly IEnumerator<TimeSpan?> _iterator;
        private double _next;
        public bool IsDisposed { get; private set; }
        public EditorCoroutine(IEnumerator<TimeSpan?> iterator) => _iterator = iterator;
        public void Tick(double now)
        {
            if (IsDisposed || now < _next) return;
            try
            {
                if (!_iterator.MoveNext()) { Dispose(); return; }
                _next = now + Math.Max(0, _iterator.Current?.TotalSeconds ?? 0);
            }
            catch { Dispose(); throw; }
        }
        public void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;
            _iterator.Dispose();
        }
    }
}
