using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Compose.Net;

namespace Compose.Net.Unity.Runtime.Adapters;

// Frame delivery follows the Unity player loop, not a coroutine that Unity can
// stop independently when a scene or Play Mode session changes.
internal sealed class UnityFrameClock : IFrameClock
{
    private readonly Stopwatch _watch = Stopwatch.StartNew();
    private readonly object _gate = new();
    private readonly List<TaskCompletionSource<TimeSpan>> _pending = new();
    public TimeSpan Now => _watch.Elapsed;

    public Task<TimeSpan> NextFrame(CancellationToken token)
    {
        if (token.IsCancellationRequested) return Task.FromCanceled<TimeSpan>(token);
        _ = ComposeInvalidatorHolder.ComposeInvalidator;
        var source = new TaskCompletionSource<TimeSpan>();
        lock (_gate) _pending.Add(source);
        var registration = token.Register(() =>
        {
            lock (_gate) _pending.Remove(source);
            source.TrySetCanceled(token);
        });
        return Await(source.Task, registration);
    }

    private static async Task<TimeSpan> Await(Task<TimeSpan> task, CancellationTokenRegistration registration)
    { try { return await task; } finally { registration.Dispose(); } }

    internal void Tick()
    {
        TaskCompletionSource<TimeSpan>[] batch;
        lock (_gate)
        {
            if (_pending.Count == 0) return;
            batch = _pending.ToArray(); _pending.Clear();
        }
        var time = Now;
        foreach (var source in batch) source.TrySetResult(time);
    }
}
