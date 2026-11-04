using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

// Coalesce wheel input into one animated target. Mouse scrolling stays within
// bounds; native elasticity is reserved for touch. Tick at the panel frame rate.
[global::Unity.Scripting.LifecycleManagement.NoAutoStaticsCleanup]
internal sealed class SmoothWheelScroll
{
    private static readonly ConditionalWeakTable<ScrollView, SmoothWheelScroll> Controllers = new();
    private readonly ScrollView _view;
    private readonly Func<bool> _enabled;
    private IVisualElementScheduledItem? _animation;
    private double _lastTime;
    private float _target, _lastPosition, _speed;
    private bool _active;
    private readonly Action _begin;
    internal bool IsActive => _active;
    private float _lineHeight = 18;
    private static readonly CustomStyleProperty<string> LineHeight = new("--unity-scroll-view-line-height");
    private bool Horizontal => _view.mode == ScrollViewMode.Horizontal;
    private Scroller Scroller => Horizontal ? _view.horizontalScroller : _view.verticalScroller;
    private float Position => Horizontal ? _view.scrollOffset.x : _view.scrollOffset.y;

    internal SmoothWheelScroll(ScrollView view, Func<bool> enabled, Action begin)
    {
        _view = view; _enabled = enabled; _begin = begin;
        Controllers.Add(view, this);
        view.elasticAnimationIntervalMs = 0;
        // Bubble after descendants have had their chance, but before native wheel handling.
        view.contentViewport.RegisterCallback<WheelEvent>(OnWheel);
        view.RegisterCallback<WheelEvent>(e => { if (e.target == view) OnWheel(e); }, TrickleDown.TrickleDown);
        view.RegisterCallback<PointerDownEvent>(e =>
        { if (e.pointerType != UnityEngine.UIElements.PointerType.mouse) Cancel(); }, TrickleDown.TrickleDown);
        view.RegisterCallback<DetachFromPanelEvent>(e => { if (e.target == view) Cancel(); });
        view.RegisterCallback<CustomStyleResolvedEvent>(_ =>
        {
            // The toolkit's line-height custom property is a dimension (e.g. 18px),
            // not a float; requesting a float logs a type mismatch on every style pass.
            _lineHeight = 18;
            if (!view.customStyle.TryGetValue(LineHeight, out var text)) return;
            if (text.EndsWith("px", StringComparison.Ordinal)) text = text.Substring(0, text.Length - 2);
            if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var height) && height > 0)
                _lineHeight = height;
        });
    }

    private void OnWheel(WheelEvent e)
    {
        if (!_enabled() || e.isPropagationStopped || Scroller.highValue <= 0) return;
        var mac = Application.platform is RuntimePlatform.OSXEditor or RuntimePlatform.OSXPlayer;
        if (mac ? e.commandKey : e.ctrlKey) return;
        var delta = Horizontal ? e.delta.x : e.delta.y;
        if (Horizontal && Mathf.Abs(e.delta.y) > Mathf.Abs(delta) && !AncestorCanScroll(false, e.delta.y))
            delta = e.delta.y;
        if (delta == 0) return;
        var step = _view.mouseWheelScrollSize;
        if (step <= 0 || Mathf.Approximately(step, 18)) step = _lineHeight;
        AddDelta(delta * step);
        // Even an exhausted root must not fall through to native elastic wheel input.
        e.StopPropagation();
    }

    private float AddDelta(float delta)
    {
        if (!_enabled()) return delta;
        var max = Mathf.Max(0, Scroller.highValue);
        var position = Mathf.Clamp(Position, 0, max);
        var start = _active ? Mathf.Clamp(_target, 0, max) : position;
        var next = Mathf.Clamp(start + delta, 0, max);
        var consumed = next - start;
        if (consumed != 0)
        {
            if (!_active)
            {
                // Cancel programmatic/native motion before taking ownership of the offset.
                _begin();
                _lastTime = Time.realtimeSinceStartupAsDouble;
                _lastPosition = Position;
                _active = true;
            }
            _target = next;
            // Retarget from the current position, retaining all pending wheel distance.
            // A bounded 100 ms tail avoids a growing backlog after fast wheel bursts.
            _speed = Mathf.Abs(_target - Position) / .1f;
            _view.touchScrollBehavior = ScrollView.TouchScrollBehavior.Clamped;
            _view.scrollDecelerationRate = 0;
            if (_animation == null) _animation = _view.schedule.Execute(Tick).Every(0);
            else if (!_animation.isActive) _animation.Resume();
        }
        var remaining = delta - consumed;
        if (Mathf.Abs(remaining) > .001f)
        {
            // Forward only the unconsumed distance, including when one event crosses an edge.
            for (var parent = _view.parent; parent != null; parent = parent.parent)
                if (parent is ScrollView view && Controllers.TryGetValue(view, out var controller) &&
                    controller.Horizontal == Horizontal && controller._enabled())
                    return controller.AddDelta(remaining);
        }
        return remaining;
    }

    private bool AncestorCanScroll(bool horizontal, float delta)
    {
        for (var parent = _view.parent; parent != null; parent = parent.parent)
            if (parent is ScrollView view && view.mode != (horizontal ? ScrollViewMode.Vertical : ScrollViewMode.Horizontal))
            {
                var scroller = horizontal ? view.horizontalScroller : view.verticalScroller;
                if (delta < 0 ? scroller.value > 0 : scroller.value < scroller.highValue) return true;
            }
        return false;
    }

    private void Tick() => TickAt(Time.realtimeSinceStartupAsDouble);

    private void TickAt(double now)
    {
        if (!_active || !_enabled()) { Cancel(); return; }
        // Geometry, scrollbars and raw state updates can reposition a view independently.
        // Never drag them back to an obsolete wheel target.
        if (Mathf.Abs(Position - _lastPosition) > .1f) { Cancel(); return; }
        var dt = (float)(now - _lastTime); _lastTime = now;
        if (dt <= 0) return;
        _target = Mathf.Clamp(_target, 0, Mathf.Max(0, Scroller.highValue));
        var next = Mathf.MoveTowards(Position, _target, _speed * dt);
        _view.scrollOffset = Horizontal ? new Vector2(next, 0) : new Vector2(0, next);
        _lastPosition = Position;
        if (Mathf.Abs(next - _target) < .001f) Cancel();
    }

    internal void Cancel() { _animation?.Pause(); _active = false; }
}
