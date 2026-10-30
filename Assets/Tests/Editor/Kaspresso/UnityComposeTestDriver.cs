using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Compose.Net;
using Compose.Net.Kaspresso;
using Compose.Net.Unity;
using Compose.Net.Unity.Packages.Compose.Net.Unity.Runtime.Adapters;
using UnityEngine;
using UnityEngine.UIElements;
using static Compose.Net.ComposeFunctions;
using Object = UnityEngine.Object;

// ReSharper disable CheckNamespace

// Owns a real editor panel. Reflection is limited to UI Toolkit's internal panel/clock APIs.
internal sealed class UnityComposeTestDriver : IComposeTestDriver, IDisposable
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private readonly ScriptableObject _owner = ScriptableObject.CreateInstance<PanelOwner>();
    private readonly IPanel _panel;
    private readonly MethodInfo _layout;
    private readonly IComposer _composer = IComposer.Create();
    private readonly TestAdapter _adapter;
    private readonly Dictionary<object, (object Callback, long Origin)> _animationStarts = new();
    private long _elapsed;
    private readonly List<string> _layoutFailures = new();
    public Kaspresso Ui { get; }
    public VisualElement Root => _panel.visualTree;
    public IEnumerable<IComposeTestNode> Roots => new[] { new Node(Root) };

    public UnityComposeTestDriver(bool useProductionClock = false)
    {
        _adapter = new TestAdapter(useProductionClock);
        var panelType = typeof(VisualElement).Assembly.GetType("UnityEngine.UIElements.Panel", true);
        _panel = (IPanel)panelType.GetMethod("CreateEditorPanel", BindingFlags.Static | Flags)
            .Invoke(null, new object[] { _owner });
        _layout = panelType.GetMethod("ValidateLayout", Flags);
        Root.style.width = 1000;
        Root.style.height = 1000;
        Ui = new Kaspresso(this, timeout: TimeSpan.Zero, log: NUnit.Framework.TestContext.WriteLine);
        Application.logMessageReceived += OnLog;
    }

    public void SetContent([Composable] Action content, bool preview = false)
    {
        Bootstrap(_composer, _adapter, preview, new ComposeConfig(Logging: false),
            new UnityReusableComposeNode(Root), content);
        Pump();
        _panel.GetType().GetMethod("TickSchedulingUpdaters", Flags).Invoke(_panel, null);
        Pump();
    }

    private void Pump()
    {
        _adapter.Invalidator.Tick();
        _layout.Invoke(_panel, null);
        _layout.Invoke(_panel, null);
        CheckLayoutFailures();
    }

    private void OnLog(string message, string stack, LogType type)
    {
        if (message.Contains("failed to stabilize") || message.Contains("Layout update is struggling"))
            _layoutFailures.Add(message);
    }

    private void CheckLayoutFailures()
    {
        if (_layoutFailures.Count > 0)
            throw new KaspressoAssertionException(string.Join("\n", _layoutFailures));
    }

    public Task WaitForIdleAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Pump();
        return Task.CompletedTask;
    }

    public Task AdvanceAsync(int milliseconds)
    {
        // Capture start times before advancing: an assertion must never advance animation time.
        TickAnimations(0);
        for (var remaining = milliseconds; remaining > 0;)
        {
            var delta = Math.Min(50, remaining);
            remaining -= delta;
            _elapsed += delta;
            _adapter.Clock.Advance(delta);
            Pump();
            TickAnimations(delta);
            _layout.Invoke(_panel, null);
            CheckLayoutFailures();
        }
        return Task.CompletedTask;
    }

    private void TickAnimations(int delta)
    {
        foreach (var element in Descendants(Root).ToArray())
        {
            var property = typeof(VisualElement).GetProperty("runningAnimations", Flags);
            if (property == null) throw new MissingMemberException("VisualElement.runningAnimations");
            if (property.GetValue(element) is not IEnumerable running) continue;
            foreach (var animation in running.Cast<object>().ToArray())
            {
                var type = animation.GetType();
                var start = (long)type.GetField("m_StartTimeMs", Flags).GetValue(animation);
                // UI Toolkit pools ValueAnimation objects; a retarget can reuse the same
                // object with a new callback, so object identity alone is not a new run.
                var callback = type.GetProperty("valueUpdated").GetValue(animation);
                if (!_animationStarts.TryGetValue(animation, out var run) || !ReferenceEquals(run.Callback, callback))
                {
                    _animationStarts[animation] = run = (callback, _elapsed - delta);
                }
                type.GetMethod("UnityEngine.UIElements.Experimental.IValueAnimationUpdate.Tick", Flags)
                    .Invoke(animation, new object[] { start + _elapsed - run.Origin });
            }
        }
    }

    public Task SizeAsync(string tag, float width, float height) => Ui.OnNodeWithTag(tag).AssertAsync(node =>
    {
        var rect = ((Node)node).Element.layout;
        if (Math.Abs(rect.width - width) >= .6f || Math.Abs(rect.height - height) >= .6f ||
            float.IsNaN(rect.width) || float.IsNaN(rect.height))
            throw new KaspressoAssertionException($"{tag}: expected {width} x {height}, actual {rect.width} x {rect.height}");
        return true;
    }, $"has layout size {width} x {height}");

    public Task OpacityAsync(string tag, float opacity) => Ui.OnNodeWithTag(tag).AssertAsync(
        node => Math.Abs(((Node)node).Element.resolvedStyle.opacity - opacity) < .02f,
        $"has opacity {opacity}");

    private static IEnumerable<VisualElement> Descendants(VisualElement root)
    {
        yield return root;
        foreach (var child in root.Children())
        foreach (var descendant in Descendants(child)) yield return descendant;
    }

    public void Dispose()
    {
        Application.logMessageReceived -= OnLog;
        _composer.Dispose();
        ((IDisposable)_panel).Dispose();
        Object.DestroyImmediate(_owner);
    }

    private sealed class PanelOwner : ScriptableObject { }

    internal sealed class Node : IComposeTestNode
    {
        public VisualElement Element { get; }
        public Node(VisualElement element) => Element = element;
        public string TestTag => Element.name;
        public string Text => (Element as TextElement)?.text;
        public bool IsEnabled => Element.enabledInHierarchy;
        public bool IsDisplayed => Element.panel != null && Ancestors().All(e =>
            e.resolvedStyle.display != DisplayStyle.None && e.resolvedStyle.visibility == Visibility.Visible) &&
            Element.worldBound.width > 0 && Element.worldBound.height > 0;
        public IEnumerable<IComposeTestNode> Children => Element.Children().Select(e => new Node(e));
        private IEnumerable<VisualElement> Ancestors()
        {
            for (var element = Element; element != null; element = element.parent) yield return element;
        }
        public void Click()
        {
            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown,
                       button = 0, mousePosition = Element.worldBound.center }))
            { down.target = Element; Element.SendEvent(down); }
            using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp,
                       button = 0, mousePosition = Element.worldBound.center }))
            { up.target = Element; Element.SendEvent(up); }
        }
        public void ReplaceText(string text)
        {
            if (Element is TextField field) field.value = text;
            else throw new NotSupportedException("The selected node is not an editable text field.");
        }
    }

    private sealed class TestAdapter(bool useProductionClock) : IComposeAdapter
    {
        private readonly IComposeAdapter _unity = ComposeUnityAdapter.Instance;
        public readonly ComposeInvalidator Invalidator = new();
        public readonly TestClock Clock = new();
        public IDensity Density => _unity.Density;
        public ComposeInvalidator ComposeInvalidatorInstance => Invalidator;
        public IFrameClock FrameClock => useProductionClock ? _unity.FrameClock : Clock;
        public IModifiersFactory ModifiersFactory => _unity.ModifiersFactory;
        public IReusableNodeFactory NodeFactory => _unity.NodeFactory;
        public IComposeLogger Logger => _unity.Logger;
        public IEnterTransitionsFactory EnterTransitionsFactory => _unity.EnterTransitionsFactory;
        public IExitTransitionsFactory ExitTransitionsFactory => _unity.ExitTransitionsFactory;
        public IBrushFactory BrushFactory => _unity.BrushFactory;
        public int Framerate() => 60;
        public IDisposable StartTimeCoroutine(IEnumerable<TimeSpan?> coroutine) =>
            throw new NotSupportedException("Tests use the manual frame clock.");
    }

    private sealed class TestClock : IFrameClock
    {
        private readonly List<TaskCompletionSource<TimeSpan>> _pending = new();
        public TimeSpan Now { get; private set; }
        public Task<TimeSpan> NextFrame(CancellationToken token)
        {
            var source = new TaskCompletionSource<TimeSpan>();
            var registration = token.Register(() => source.TrySetCanceled());
            _pending.Add(source);
            return Complete();
            async Task<TimeSpan> Complete()
            {
                try { return await source.Task; }
                finally { registration.Dispose(); }
            }
        }
        public void Advance(int milliseconds)
        {
            Now += TimeSpan.FromMilliseconds(milliseconds);
            var pending = _pending.ToArray();
            _pending.Clear();
            foreach (var source in pending) source.TrySetResult(Now);
        }
    }
}
