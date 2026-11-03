using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Compose.Net;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Runtime.Adapters.Nodes;

internal sealed class LazyGridNode : UnityReusableComposeNode, ILazyGridLayoutNode, IReusableComposeNode
{
    internal readonly LazyGridView View;
    internal readonly List<LazyGridItemNode> Items = new();
    internal LazyGridNode(bool horizontal) : this(new LazyGridView(horizontal)) { }
    private LazyGridNode(LazyGridView view) : base(view) { View = view; view.Owner = this; }
    public void ConfigureLazyGrid(ILazyGridState state, ILazyGridItemProvider items, LazyGridSettings settings) => View.Configure(state, items, settings);
    public new void Insert(int index, IReusableComposeNode child) => Reinsert(index, child);
    public new void Reinsert(int index, IReusableComposeNode child)
    {
        var item = (LazyGridItemNode)child;
        Items.Remove(item); Items.Insert(Math.Min(index, Items.Count), item); item.Owner = this;
        View.Attach(item);
    }
    public new void Remove(IReusableComposeNode child)
    {
        var item = (LazyGridItemNode)child;
        Items.Remove(item); item.VisualElement.RemoveFromHierarchy(); item.Owner = null; View.QueueLayout();
    }
    public new void FastRemove(int index, IReusableComposeNode child) => Remove(child);
    public new void Dispose() { View.Dispose(); base.Dispose(); }
}

internal sealed class LazyGridItemNode : UnityReusableComposeNode, ILazyGridItemNode, IReusableComposeNode
{
    internal LazyGridNode? Owner;
    internal int Index = -1;
    internal object? Key, ContentType;
    internal LazyGridItemNode() : base(new Box { name = "LazyGridItem" })
    {
        VisualElement.style.flexShrink = 0;
        VisualElement.RegisterCallback<GeometryChangedEvent>(_ => Owner?.View.QueueLayout());
    }
    public new IReusableComposeNode? Parent => Owner;
    public new int IndexInParent => Owner?.Items.IndexOf(this) ?? -1;
    public void ConfigureGridItem(int index, object key, object? contentType)
    { Index = index; Key = key; ContentType = contentType; Owner?.View.Attach(this); }
}

/// <summary>Vertical grids recycle whole rows through ListView's dynamic-height virtualization.
/// Horizontal grids use a small window of columns in ScrollView (UI Toolkit has no horizontal
/// collection virtualization). Both delegate input, clipping, inertia and nesting to ScrollView.
/// Item compositions remain keyed in their original parent composition, independent of native slots.</summary>
internal sealed class LazyGridView : VisualElement, IDisposable
{
    internal LazyGridNode Owner = null!;
    private readonly bool _horizontal;
    private readonly ListView? _list;
    private readonly ScrollView _scroll;
    private readonly VisualElement _overlay = new() { name = "grid-sticky-overlay" };
    private readonly Dictionary<int, Slot> _slots = new();
    private readonly GridExtentIndex _extents = new();
    private readonly List<int> _retainedIndices = new();
    private GridLines? _lines;
    private ILazyGridState? _state;
    private ILazyGridStateAdapter Bridge => (ILazyGridStateAdapter)_state!;
    private ILazyGridItemProvider? _items;
    private LazyGridSettings? _settings;
    private IVisualElementScheduledItem? _layout, _idle;
    private bool _syncing, _disposed, _stopped, _refreshing, _dirty = true;
    private float _leading, _trailing, _crossStart, _crossEnd, _spacing, _alignmentOffset;
    private float _crossAvailable = -1, _extraSpacing, _extentSpacing = float.NaN;
    private bool _lastReverse;
    // Yoga clamps very large layout dimensions. Keep native ListView coordinates bounded while
    // still letting its own dynamic-height controller virtualize each segment.
    private const int NativeLineWindow = 32768;
    private int _windowStart, _windowCount;
    private bool Windowed => !_horizontal && LineCount > NativeLineWindow;
    private float NativeBase => Windowed ? _extents.Start(_windowStart) + (_windowStart > 0 ? _leading : 0) : 0;
    private int[] _sizes = Array.Empty<int>();
    private float[] _positions = Array.Empty<float>();
    private int _pointer = -1, _pinned = -1;
    private object? _anchorKey;
    private int _anchorIndex;
    private float _anchorOffset;
    private LazyGridScrollRequest? _request;
    private int _requestFrames;
    private bool _animating, _animationStarted;
    private IDragInteraction.Start? _drag;
    private float Axis(Vector2 v) => _horizontal ? v.x : v.y;
    private float Cross(Vector2 v) => _horizontal ? v.y : v.x;
    private float Viewport => Math.Max(0, Axis(_scroll.contentViewport.layout.size));
    private float Physical => Axis(_scroll.scrollOffset) + NativeBase;
    private Scroller Scroller => _horizontal ? _scroll.horizontalScroller : _scroll.verticalScroller;
    private int Count => _items?.Count ?? 0;
    private int LineCount => _lines?.Count ?? 0;
    private int PhysicalLine(int line) => _settings!.ReverseLayout ? LineCount - 1 - line : line;
    private float Logical(float value) => _settings!.ReverseLayout ? _state!.MaxValue - value : value;

    internal LazyGridView(bool horizontal)
    {
        _horizontal = horizontal;
        name = horizontal ? "LazyHorizontalGrid" : "LazyVerticalGrid";
        style.minWidth = 0; style.minHeight = 0; style.overflow = Overflow.Hidden;
        if (horizontal) _scroll = new ScrollView(ScrollViewMode.Horizontal);
        else
        {
            _list = new ListView
            {
                name = "grid-native-list", selectionType = SelectionType.None,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                fixedItemHeight = GridExtentIndex.Estimate, reorderable = false,
                showBorder = false, showAlternatingRowBackgrounds = AlternatingRowBackground.None,
                makeItem = MakeSlot, bindItem = BindSlot, unbindItem = UnbindSlot,
                destroyItem = e => e.RemoveFromHierarchy(),
            };
            _scroll = _list.Q<ScrollView>();
        }
        var native = (VisualElement?)_list ?? _scroll;
        native.style.flexGrow = 1; native.style.flexShrink = 1;
        native.style.minWidth = 0; native.style.minHeight = 0;
        native.style.alignSelf = Align.Stretch;
        hierarchy.Add(native);
        _scroll.name = "grid-native-scroll";
        _scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        _scroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        _scroll.nestedInteractionKind = ScrollView.NestedInteractionKind.ForwardScrolling;
        _scroll.contentViewport.style.minWidth = 0; _scroll.contentViewport.style.minHeight = 0;
        _scroll.contentContainer.style.flexShrink = 0;
        _overlay.style.position = Position.Absolute; _overlay.style.left = 0; _overlay.style.top = 0;
        _overlay.style.right = 0; _overlay.style.bottom = 0;
        _overlay.pickingMode = UnityEngine.UIElements.PickingMode.Ignore;
        hierarchy.Add(_overlay);
        _scroll.contentViewport.RegisterCallback<GeometryChangedEvent>(_ => QueueLayout());
        _scroll.contentContainer.RegisterCallback<GeometryChangedEvent>(_ => QueueLayout());
        Scroller.valueChanged += OnNativeScroll;
        _scroll.RegisterCallback<PointerDownEvent>(_ => ResumePhysics(), TrickleDown.TrickleDown);
        _scroll.RegisterCallback<WheelEvent>(OnWheelStart, TrickleDown.TrickleDown);
        _scroll.contentContainer.RegisterCallback<WheelEvent>(GateWheel);
        _scroll.contentContainer.RegisterCallback<PointerMoveEvent>(GateMove);
        _scroll.contentContainer.RegisterCallback<NavigationMoveEvent>(GateNavigation);
        _scroll.RegisterCallback<NavigationMoveEvent>(e => { if (e.target == _scroll) GateNavigation(e); }, TrickleDown.TrickleDown);
        _scroll.contentContainer.RegisterCallback<PointerCaptureEvent>(OnCapture);
        _scroll.contentContainer.RegisterCallback<PointerUpEvent>(e => { if (e.pointerId == _pointer) EndDrag(false); }, TrickleDown.TrickleDown);
        _scroll.contentContainer.RegisterCallback<PointerCancelEvent>(e => { if (e.pointerId == _pointer) EndDrag(true); }, TrickleDown.TrickleDown);
        _scroll.contentContainer.RegisterCallback<PointerCaptureOutEvent>(e => { if (e.pointerId == _pointer) EndDrag(true); });
        RegisterCallback<DetachFromPanelEvent>(e => { if (e.target == this) StopNative(); });
    }

    internal void Configure(ILazyGridState state, ILazyGridItemProvider items, LazyGridSettings settings)
    {
        if (_disposed) return;
        var wasEmpty = _items?.Count == 0;
        var contentChanged = !ReferenceEquals(_items, items);
        var dataChanged = _items == null || _items.Count != items.Count ||
            Owner.Items.Any(n => n.Index >= 0 && n.Index < items.Count && !Equals(n.Key, items.GetKey(n.Index)));
        var settingsChanged = settings != _settings;
        var stateChanged = !ReferenceEquals(state, _state);
        if (stateChanged)
        {
            Unsubscribe(); _state = state;
            Bridge.ValueChanged += OnStateScroll; Bridge.ScrollRequested += StopNative; Bridge.ItemsRequested += QueueLayout;
            _anchorKey = null;
        }
        if (dataChanged || settingsChanged)
        {
            _retainedIndices.Clear();
            foreach (var node in Owner.Items)
                if (node.Key != null)
                { var retained = items.FindIndexByKey(node.Key, node.Index); if (retained >= 0) _retainedIndices.Add(retained); }
        }
        _items = items; _settings = settings;
        var d = settings.Density; var p = settings.ContentPadding;
        var left = d.ToPx(p.Left); var top = d.ToPx(p.Top); var right = d.ToPx(p.Right); var bottom = d.ToPx(p.Bottom);
        if (!float.IsFinite(left + top + right + bottom) || left < 0 || top < 0 || right < 0 || bottom < 0)
            throw new ArgumentOutOfRangeException(nameof(settings.ContentPadding));
        _leading = _horizontal ? left : top; _trailing = _horizontal ? right : bottom;
        _crossStart = _horizontal ? top : left; _crossEnd = _horizontal ? bottom : right;
        _spacing = settings.MainAxisArrangement is Arrangement.Spaced spaced ? d.ToPx(spaced.Space) : 0;
        if (!float.IsFinite(_spacing)) throw new ArgumentOutOfRangeException(nameof(settings.MainAxisArrangement));
        _scroll.focusable = settings.UserScrollEnabled;
        if (!settings.UserScrollEnabled) StopNative();
        ConfigurePhysics();
        // Span metadata can change without keys/count changing. It never composes item content.
        _dirty |= dataChanged || settingsChanged || items.HasCustomSpans;
        if (!stateChanged && Bridge.PendingScroll == null && (dataChanged || settingsChanged || contentChanged) && _anchorKey != null)
        {
            var anchor = items.FindIndexByKey(_anchorKey, _anchorIndex);
            if (anchor >= 0) { _request = new(anchor, _anchorOffset, false, false, default); _requestFrames = 0; _animating = false; _animationStarted = false; }
        }
        if (wasEmpty && items.Count > 0 && Bridge.PendingScroll == null)
        { _request = new(0, 0, false, false, default); _requestFrames = 0; }
        QueueLayout();
    }

    private VisualElement MakeSlot()
    {
        var slot = new Slot(); slot.style.flexShrink = 0;
        if (!_horizontal) slot.style.minHeight = GridExtentIndex.Estimate;
        slot.RegisterCallback<GeometryChangedEvent>(_ => QueueLayout());
        slot.RegisterCallback<AttachToPanelEvent>(_ => NativeCollectionStyles.ClearRowBackground(slot));
        return slot;
    }
    private void BindSlot(VisualElement element, int physicalLine)
    {
        NativeCollectionStyles.ClearRowBackground(element);
        if (_lines == null) return;
        var slot = (Slot)element; slot.Line = PhysicalLine(physicalLine + _windowStart);
        _slots[slot.Line] = slot; ConfigureSlot(slot);
        foreach (var node in Owner.Items) Attach(node);
        QueueLayout();
    }
    private void UnbindSlot(VisualElement element, int ignored)
    {
        var slot = (Slot)element;
        if (_slots.TryGetValue(slot.Line, out var active) && active == slot) _slots.Remove(slot.Line);
        slot.Clear(); slot.Line = -1; slot.style.minHeight = GridExtentIndex.Estimate;
        if (!_refreshing) QueueLayout();
    }
    private void ConfigureSlot(Slot slot)
    {
        slot.style.flexDirection = _horizontal ? FlexDirection.Column : FlexDirection.Row;
        slot.style.alignItems = _settings!.ReverseLayout ? Align.FlexEnd : Align.FlexStart;
        slot.style.flexShrink = 0;
        var gap = PhysicalLine(slot.Line) + 1 == LineCount ? 0 : _spacing + _extraSpacing;
        if (_horizontal)
        {
            slot.style.position = Position.Absolute;
            slot.style.top = _crossStart; slot.style.height = _crossAvailable;
            slot.style.left = _leading + _alignmentOffset + _extents.Start(PhysicalLine(slot.Line)) + PhysicalLine(slot.Line) * _extraSpacing;
        }
        else
        {
            slot.style.paddingTop = PhysicalLine(slot.Line) == 0 ? _leading + _alignmentOffset : 0;
            slot.style.paddingBottom = Math.Max(0, gap) + (PhysicalLine(slot.Line) + 1 == LineCount ? _trailing : 0);
            slot.style.marginBottom = Math.Min(0, gap);
            slot.style.paddingLeft = _crossStart; slot.style.paddingRight = _crossEnd;
        }
    }
    internal void Attach(LazyGridItemNode node)
    {
        if (_lines == null || node.Index < 0 || node.Index >= Count) return;
        var line = _lines.LineOf(node.Index);
        if (!_slots.TryGetValue(line, out var slot))
        {
            if (node.Index == _pinned)
            {
                var pinned = node.VisualElement;
                pinned.style.position = Position.Absolute;
                pinned.style.marginLeft = 0; pinned.style.marginTop = 0;
                if (_horizontal) { pinned.style.top = _crossStart; pinned.style.height = _crossAvailable; }
                else { pinned.style.left = _crossStart; pinned.style.width = _crossAvailable; }
                if (pinned.parent != _overlay) _overlay.Add(pinned);
            }
            return;
        }
        var cells = _lines.Cells(line);
        var cell = cells.First(c => c.Index == node.Index);
        var element = node.VisualElement;
        element.style.position = Position.Relative;
        element.style.left = StyleKeyword.Null; element.style.top = StyleKeyword.Null;
        element.style.translate = StyleKeyword.Null;
        var end = cell.Slot + cell.Span - 1;
        var size = _positions[end] + _sizes[end] - _positions[cell.Slot];
        var order = Array.FindIndex(cells, c => c.Index == node.Index);
        var previousEnd = order == 0 ? 0 : _positions[cells[order - 1].Slot + cells[order - 1].Span - 1] + _sizes[cells[order - 1].Slot + cells[order - 1].Span - 1];
        var gap = _positions[cell.Slot] - previousEnd;
        if (_horizontal) { element.style.height = size; element.style.marginTop = gap; }
        else { element.style.width = size; element.style.marginLeft = gap; }
        // A keyed item may move between native recycled rows without losing its composition.
        var before = slot.Children().Count(e => e.GetReusableComposeNode() is LazyGridItemNode n && n.Index < node.Index);
        if (element.parent != slot || slot.IndexOf(element) != before)
        { element.RemoveFromHierarchy(); slot.Insert(Math.Min(before, slot.childCount), element); }
        if (!_syncing) QueueLayout();
    }
    internal void QueueLayout()
    {
        if (_disposed || _layout != null) return;
        _layout = schedule.Execute(() => { _layout = null; LayoutItems(); });
    }
    private void RebuildTracks(float available)
    {
        var spacing = _settings!.CrossAxisArrangement is Arrangement.Spaced spaced ? _settings.Density.ToPx(spaced.Space) : 0;
        var sizes = _settings.Cells.CalculateCrossAxisCellSizes(_settings.Density, (int)Math.Round(available), (int)Math.Round(spacing)).ToArray();
        if (sizes.Length == 0 || sizes.Any(s => s < 0)) throw new ArgumentException("GridCells must return at least one nonnegative track size.");
        var lines = new GridLines(_items!, sizes.Length);
        var changed = _lines == null || !_sizes.SequenceEqual(sizes) || !_lines.SameStructure(lines) || _extentSpacing != _spacing || _lastReverse != _settings.ReverseLayout;
        _extentSpacing = _spacing; _lastReverse = _settings.ReverseLayout;
        _sizes = sizes; _positions = Arrange(_settings.CrossAxisArrangement, available, sizes.Select(s => (float)s).ToArray(), spacing);
        _crossAvailable = available; _dirty = false;
        _lines = lines;
        if (!changed) { foreach (var slot in _slots.Values) ConfigureSlot(slot); return; }
        _extents.Reset(lines.Count, _spacing);
        _refreshing = true;
        try
        {
            foreach (var node in Owner.Items) node.VisualElement.RemoveFromHierarchy();
            if (_horizontal) foreach (var slot in _slots.Values) slot.RemoveFromHierarchy();
            _slots.Clear();
            if (_list != null)
            {
                var target = Math.Min(Bridge.PendingScroll?.Index ?? _request?.Index ?? _anchorIndex, Math.Max(0, Count - 1));
                var targetLine = Count == 0 ? 0 : PhysicalLine(lines.LineOf(target));
                _windowCount = Math.Min(NativeLineWindow, lines.Count);
                _windowStart = Math.Clamp(targetLine - _windowCount / 2, 0, Math.Max(0, lines.Count - _windowCount));
                _scroll.scrollOffset = Vector2.zero; _list.itemsSource = new IndexSource(_windowCount); _list.Rebuild();
            }
        }
        finally { _refreshing = false; }
        if (Bridge.PendingScroll == null && _request?.Completion.IsCompleted != false && _anchorKey != null)
        {
            var anchor = _items!.FindIndexByKey(_anchorKey, _anchorIndex);
            if (anchor >= 0) { _request = new(anchor, _anchorOffset, false, false, default); _requestFrames = 0; _animating = false; _animationStarted = false; }
        }
    }
    private void LayoutItems()
    {
        if (_disposed || _state == null || _settings == null || panel == null || !float.IsFinite(Viewport) || Viewport <= 0) return;
        var cross = Cross(_scroll.contentViewport.layout.size) - _crossStart - _crossEnd;
        if (!float.IsFinite(cross)) throw new InvalidOperationException("Lazy grids require a bounded cross axis.");
        cross = Math.Max(0, cross);
        _syncing = true;
        try
        {
            if (_dirty || Math.Abs(_crossAvailable - cross) > .1f) RebuildTracks(cross);
            if (Bridge.PendingScroll is { } requested && requested != _request)
            { _request?.Cancel(); _request = requested; _requestFrames = 0; _animating = false; _animationStarted = false; }
            var anchorLine = Count == 0 ? -1 : PhysicalLine(_lines!.LineOf(Math.Min(_anchorIndex, Count - 1)));
            float AnchorDistance() => anchorLine < 0 ? 0 : _settings.ReverseLayout
                ? _extents.Total - _extents.Start(anchorLine) - _extents.Size(anchorLine) : _extents.Start(anchorLine);
            var anchorBefore = AnchorDistance();
            foreach (var slot in _slots.Values)
            {
                if (slot.Line < 0 || slot.childCount == 0) continue;
                var size = slot.Children().Max(e => Axis(e.layout.size));
                if (float.IsFinite(size) && size > 0)
                { _extents.Measure(PhysicalLine(slot.Line), size); if (!_horizontal) slot.style.minHeight = 0; }
            }
            var estimate = _leading + _extents.Total + _trailing;
            var free = Math.Max(0, Viewport - estimate);
            var arrangement = _settings.MainAxisArrangement is Arrangement.Spaced spaced ? spaced.Alignment : _settings.MainAxisArrangement;
            _alignmentOffset = arrangement == Arrangement.Center ? free / 2 : arrangement == Arrangement.Bottom || arrangement == Arrangement.Right ? free : 0;
            _extraSpacing = arrangement == Arrangement.SpaceBetween && LineCount > 1 ? free / (LineCount - 1) :
                arrangement == Arrangement.SpaceAround && LineCount > 0 ? free / LineCount :
                arrangement == Arrangement.SpaceEvenly ? free / (LineCount + 1) : 0;
            if (arrangement == Arrangement.SpaceAround) _alignmentOffset = _extraSpacing / 2;
            if (arrangement == Arrangement.SpaceEvenly) _alignmentOffset = _extraSpacing;
            foreach (var slot in _slots.Values) ConfigureSlot(slot);
            if (_horizontal)
            {
                _scroll.contentContainer.style.width = Math.Max(Viewport, estimate);
                _scroll.contentContainer.style.height = Cross(_scroll.contentViewport.layout.size);
                var preserved = _state.Value + (anchorLine >= 0 && _request?.Completion.IsCompleted != false ? AnchorDistance() - anchorBefore : 0);
                Bridge.UpdateLayout(Viewport, Math.Max(Viewport, estimate)); Scroller.highValue = _state.MaxValue;
                if (Math.Abs(_state.Value - preserved) > .1f) Bridge.SetNativeValue(preserved);
                SetPhysical(Logical(_state.Value));
                var start = _extents.IndexAt(Math.Max(0, Physical - _leading - _alignmentOffset));
                var end = _extents.IndexAt(Math.Max(0, Physical + Viewport - _leading - _alignmentOffset));
                var wanted = new HashSet<int>();
                for (var i = Math.Max(0, start - 1); i < Math.Min(LineCount, end + 2); i++) wanted.Add(PhysicalLine(i));
                foreach (var line in _slots.Keys.ToArray()) if (!wanted.Contains(line)) { _slots[line].RemoveFromHierarchy(); _slots.Remove(line); }
                foreach (var line in wanted)
                {
                    if (!_slots.TryGetValue(line, out var slot)) { slot = (Slot)MakeSlot(); slot.Line = line; _slots.Add(line, slot); _scroll.Add(slot); }
                    ConfigureSlot(slot);
                }
            }
            else
            {
                var nativeExtent = _scroll.contentContainer.layout.height;
                var remaining = Windowed && _windowStart + _windowCount < LineCount
                    ? _extents.Total - _extents.Start(_windowStart + _windowCount) + _trailing : 0;
                Bridge.UpdateLayout(Viewport, Math.Max(Viewport, Windowed ? NativeBase + nativeExtent + remaining : nativeExtent));
                if (_request?.Completion.IsCompleted != false || _animating) SetPhysical(Logical(_state.Value));
            }
            var indices = _slots.Keys.Where(l => l >= 0 && l < LineCount).SelectMany(l => _lines!.Cells(l).Select(c => c.Index)).ToList();
            _pinned = -1;
            foreach (var header in _items!.StickyHeaderIndices) { if (header > _state.FirstVisibleItemIndex) break; _pinned = header; }
            if (_pinned >= 0 && !indices.Contains(_pinned)) indices.Add(_pinned);
            if (_request?.Completion.IsCompleted == false)
            {
                foreach (var retained in _retainedIndices)
                    if (retained < Count && !indices.Contains(retained)) indices.Add(retained);
            }
            else _retainedIndices.Clear();
            indices.Sort();
            if (indices.Count != 0 || Count == 0) Bridge.SetComposedItems(indices);
            foreach (var node in Owner.Items) Attach(node);
            PositionRequest();
            Publish();
            if (Windowed && _request?.Completion.IsCompleted != false && Count > 0)
            {
                var local = _scroll.scrollOffset.y;
                if ((_windowStart > 0 && local < Viewport) || (_windowStart + _windowCount < LineCount && local > Scroller.highValue - Viewport))
                {
                    var line = PhysicalLine(_lines!.LineOf(_state.FirstVisibleItemIndex));
                    _request = new(_state.FirstVisibleItemIndex, _state.FirstVisibleItemScrollOffset, false, false, default);
                    _requestFrames = 0; MoveNativeWindow(line); QueueLayout();
                }
            }
        }
        finally { _syncing = false; }
    }
    private void Publish()
    {
        var visible = new List<LazyGridItemInfo>();
        var first = int.MaxValue; var firstOffset = 0f;
        foreach (var node in Owner.Items)
        {
            if (node.Index < 0 || node.Index >= Count || node.VisualElement.panel == null) continue;
            var line = _lines!.LineOf(node.Index);
            var cell = _lines.Cells(line).First(c => c.Index == node.Index);
            var element = node.VisualElement;
            var size = Axis(element.layout.size);
            var start = ItemStart(node) - Physical;
            var lineSize = _extents.Size(PhysicalLine(line));
            if (_settings!.ReverseLayout) start += lineSize - size;
            if (node.Index == _pinned)
            {
                var desired = _settings.ReverseLayout ? Math.Min(start, Viewport - size) : Math.Max(start, 0);
                foreach (var other in Owner.Items)
                    if (other.Index > node.Index && _items!.StickyHeaderIndices.Contains(other.Index))
                    {
                        var next = ItemStart(other) - Physical;
                        desired = _settings.ReverseLayout ? Math.Max(desired, next + Axis(other.VisualElement.layout.size)) : Math.Min(desired, next - size);
                    }
                if (element.parent == _overlay)
                {
                    element.style.translate = StyleKeyword.Null;
                    if (_horizontal) element.style.left = desired; else element.style.top = desired;
                }
                else element.style.translate = _horizontal ? new Translate(desired - start, 0) : new Translate(0, desired - start);
                start = desired;
            }
            if (!float.IsFinite(start + size) || start + size <= 0 || start >= Viewport || size <= 0) continue;
            var cross = _crossStart + _positions[cell.Slot];
            var offset = _horizontal ? new Offset(start - _leading, cross) : new Offset(cross, start - _leading);
            visible.Add(new(node.Index, _items!.GetKey(node.Index), offset, _horizontal ? cell.Slot : line,
                _horizontal ? line : cell.Slot, new Size(element.layout.width, element.layout.height), _items.GetContentType(node.Index), cell.Span));
            var lineStart = ItemStart(node) - Physical;
            var logicalOffset = _settings.ReverseLayout ? Viewport - lineStart - lineSize - _trailing : lineStart - _leading;
            var lineFirst = _lines.Cells(line)[0].Index;
            // Items entirely inside the leading padding don't determine the scroll position.
            if (logicalOffset + lineSize > 0 && lineFirst < first)
            { first = lineFirst; firstOffset = Math.Max(0, -logicalOffset); }
        }
        visible.Sort((a, b) => a.Index.CompareTo(b.Index));
        if (first == int.MaxValue) first = Count == 0 ? 0 : Math.Min(_state!.FirstVisibleItemIndex, Count - 1);
        _anchorIndex = first; _anchorOffset = firstOffset; _anchorKey = Count == 0 ? null : _items!.GetKey(first);
        var before = _settings!.ReverseLayout ? _trailing : _leading;
        var after = _settings.ReverseLayout ? _leading : _trailing;
        Bridge.PublishLayout(new(visible.AsReadOnly(), Count, new Size(_scroll.contentViewport.layout.width, _scroll.contentViewport.layout.height),
            -before, Viewport - before, _settings.Orientation, _settings.ReverseLayout, before, after, _spacing, _sizes.Length), first, firstOffset);
    }
    private float ItemStart(LazyGridItemNode node)
    {
        var line = _lines!.LineOf(node.Index);
        if (_horizontal) return _leading + _alignmentOffset + _extents.Start(PhysicalLine(line)) + PhysicalLine(line) * _extraSpacing;
        if (_slots.TryGetValue(line, out var slot) && slot.panel != null)
            return Axis(slot.worldBound.position - _scroll.contentViewport.worldBound.position) + Physical + (!_horizontal && PhysicalLine(line) == 0 ? _leading + _alignmentOffset : 0);
        return _leading + _alignmentOffset + _extents.Start(PhysicalLine(line));
    }
    private void PositionRequest()
    {
        if (_request == null || _request.Completion.IsCompleted || _animating) return;
        if (Count == 0) { _request.Complete(); return; }
        var index = Math.Min(_request.Index, Count - 1); var line = _lines!.LineOf(index);
        if (Windowed && (PhysicalLine(line) < _windowStart || PhysicalLine(line) >= _windowStart + _windowCount))
        { MoveNativeWindow(PhysicalLine(line)); QueueLayout(); return; }
        var node = Owner.Items.FirstOrDefault(n => n.Index == index);
        var measured = node != null && node.VisualElement.panel != null && Axis(node.VisualElement.layout.size) > 0;
        if (_request.Animated && !_animationStarted)
        {
            _animationStarted = true; _animating = true;
            var estimated = _leading + _alignmentOffset + _extents.Start(PhysicalLine(line));
            var physicalTarget = _settings!.ReverseLayout ? estimated + _extents.Size(PhysicalLine(line)) - Viewport + _trailing - _request.ScrollOffset : estimated - _leading + _request.ScrollOffset;
            _ = AnimateRequest(_request, Logical(Math.Clamp(physicalTarget, 0, _state!.MaxValue))); return;
        }
        if (!measured)
        {
            if (_list != null) _list.ScrollToItem(PhysicalLine(line) - _windowStart);
            else
            {
                var estimated = _leading + _alignmentOffset + _extents.Start(PhysicalLine(line));
                var target = _settings!.ReverseLayout ? estimated + _extents.Size(PhysicalLine(line)) - Viewport + _trailing - _request.ScrollOffset : estimated - _leading + _request.ScrollOffset;
                Bridge.SetNativeValue(Logical(Math.Clamp(target, 0, _state!.MaxValue))); SetPhysical(Logical(_state!.Value));
            }
            _requestFrames = 0; QueueLayout(); return;
        }
        var start = ItemStart(node!); var size = _extents.Size(PhysicalLine(line));
        var targetPhysical = _settings!.ReverseLayout ? start + size - Viewport + _trailing - _request.ScrollOffset : start - _leading + _request.ScrollOffset;
        if (_request.EnsureVisible) targetPhysical = start < Physical ? start : start + size > Physical + Viewport ? start + size - Viewport : Physical;
        targetPhysical = Math.Clamp(targetPhysical, 0, _state!.MaxValue);
        var error = Math.Abs(Physical - targetPhysical);
        SetPhysical(targetPhysical); Bridge.SetNativeValue(Logical(targetPhysical));
        if (error > .1f) _requestFrames = 0;
        if (++_requestFrames >= 4) _request.Complete(); else QueueLayout();
    }
    private async Task AnimateRequest(LazyGridScrollRequest request, float target)
    {
        try { await _state!.AnimateScrollTo(target, request.AnimationSpec); if (_request == request) { _requestFrames = 2; _animating = false; QueueLayout(); } }
        catch (OperationCanceledException) { request.Cancel(); if (_request == request) _animating = false; }
        catch (Exception exception) { request.Fail(exception); if (_request == request) _animating = false; }
    }
    private void MoveNativeWindow(int physicalLine)
    {
        _refreshing = true;
        try
        {
            foreach (var node in Owner.Items) node.VisualElement.RemoveFromHierarchy();
            _slots.Clear();
            _windowStart = Math.Clamp(physicalLine - _windowCount / 2, 0, Math.Max(0, LineCount - _windowCount));
            _scroll.scrollOffset = Vector2.zero;
            _list!.Rebuild();
        }
        finally { _refreshing = false; }
    }
    private void SetPhysical(float value) => _scroll.scrollOffset = _horizontal ? new Vector2(value, 0) : new Vector2(0, Math.Max(0, value - NativeBase));
    private void OnStateScroll(float value)
    {
        if (_syncing || _state == null || _settings == null) return;
        _syncing = true; try { SetPhysical(Logical(value)); } finally { _syncing = false; }
        QueueLayout();
    }
    private void OnNativeScroll(float nativeValue)
    {
        if (_syncing || _refreshing || _state == null || _settings == null) return;
        if (!Windowed && Math.Abs(Math.Max(0, Scroller.highValue) - _state.MaxValue) > .6f) { QueueLayout(); return; }
        _syncing = true;
        try { Bridge.SetNativeValue(Logical(nativeValue + NativeBase)); Bridge.SetNativeScrollInProgress(true); }
        finally { _syncing = false; }
        _idle?.Pause(); _idle = schedule.Execute(() => { if (_drag == null && !_disposed) Bridge.SetNativeScrollInProgress(false); }).StartingIn(80);
        QueueLayout();
    }
    private static float[] Arrange(Arrangement arrangement, float available, float[] sizes, float spacing)
    {
        var total = sizes.Sum() + Math.Max(0, sizes.Length - 1) * spacing;
        var free = Math.Max(0, available - total);
        var alignment = arrangement is Arrangement.Spaced spaced ? spaced.Alignment : arrangement;
        var cursor = alignment == Arrangement.Center ? free / 2 : alignment == Arrangement.Bottom || alignment == Arrangement.Right ? free : 0;
        if (arrangement == Arrangement.SpaceBetween && sizes.Length > 1) spacing += free / (sizes.Length - 1);
        if (arrangement == Arrangement.SpaceAround && sizes.Length > 0) { spacing += free / sizes.Length; cursor = free / sizes.Length / 2; }
        if (arrangement == Arrangement.SpaceEvenly) { spacing += free / (sizes.Length + 1); cursor = free / (sizes.Length + 1); }
        var result = new float[sizes.Length];
        for (var i = 0; i < sizes.Length; i++) { result[i] = cursor; cursor += sizes[i] + spacing; }
        return result;
    }
    private void ConfigurePhysics()
    {
        if (_settings == null) return;
        _scroll.scrollDecelerationRate = _settings.UserScrollEnabled && !_stopped ? _settings.FlingBehavior?.DecelerationRate ?? .135f : 0;
        _scroll.touchScrollBehavior = _settings.OverscrollEffect == null || !_settings.UserScrollEnabled || _stopped ? ScrollView.TouchScrollBehavior.Clamped : ScrollView.TouchScrollBehavior.Elastic;
        if (_settings.OverscrollEffect != null) _scroll.elasticity = _settings.OverscrollEffect.Elasticity;
    }
    private void StopNative()
    {
        _stopped = true; ConfigurePhysics();
        if (_pointer >= 0) _scroll.contentContainer.ReleasePointer(_pointer);
        EndDrag(true); _idle?.Pause(); if (_state != null) Bridge.SetNativeScrollInProgress(false);
    }
    private void ResumePhysics() { if (_settings?.UserScrollEnabled != true) return; _stopped = false; ConfigurePhysics(); }
    private void OnWheelStart(WheelEvent e)
    {
        ResumePhysics();
        if (e.target == _scroll || e.target == _scroll.contentViewport || e.target == _scroll.contentContainer) GateWheel(e);
    }
    private void GateWheel(WheelEvent e)
    {
        if (_settings?.UserScrollEnabled == true) return;
        e.StopPropagation(); if (parent == null) return;
        // MouseEventBase.GetPooled(IMouseEvent) does not copy WheelEvent.delta.
        var systemEvent = new Event { type = EventType.ScrollWheel, mousePosition = e.mousePosition,
            delta = new Vector2(e.delta.x, e.delta.y), modifiers = e.modifiers };
        using var next = WheelEvent.GetPooled(systemEvent); next.target = parent; parent.SendEvent(next);
    }
    private void GateMove(PointerMoveEvent e)
    {
        if (_settings?.UserScrollEnabled == true || e.pointerType == UnityEngine.UIElements.PointerType.mouse) return;
        e.StopPropagation(); if (parent == null) return;
        using var next = PointerMoveEvent.GetPooled(e); next.target = parent; parent.SendEvent(next);
    }
    private void GateNavigation(NavigationMoveEvent e)
    {
        if (_settings?.UserScrollEnabled == true) return;
        e.StopPropagation(); if (parent == null) return;
        using var next = NavigationMoveEvent.GetPooled(e.move, e.modifiers); next.target = parent; parent.SendEvent(next);
    }
    private void OnCapture(PointerCaptureEvent e)
    {
        if (_settings?.UserScrollEnabled != true || e.target != _scroll.contentContainer || e.pointerId == UnityEngine.UIElements.PointerId.mousePointerId) return;
        _pointer = e.pointerId; _drag = new IDragInteraction.Start(); Bridge.EmitDragInteraction(_drag); Bridge.SetNativeScrollInProgress(true);
    }
    private void EndDrag(bool cancel)
    {
        if (_drag == null) return;
        Bridge.EmitDragInteraction(cancel ? new IDragInteraction.Cancel(_drag) : new IDragInteraction.Stop(_drag));
        _drag = null; _pointer = -1; Bridge.SetNativeScrollInProgress(false);
    }
    private void Unsubscribe()
    {
        if (_state == null) return;
        _state.ScrollTo(_state.Value);
        StopNative(); Bridge.ValueChanged -= OnStateScroll; Bridge.ScrollRequested -= StopNative; Bridge.ItemsRequested -= QueueLayout;
        _request?.Cancel(); Bridge.PendingScroll?.Cancel();
    }
    public void Dispose()
    {
        if (_disposed) return;
        Unsubscribe(); _disposed = true; _layout?.Pause(); _idle?.Pause();
        Scroller.valueChanged -= OnNativeScroll; _slots.Clear();
    }
    private sealed class Slot : VisualElement { internal int Line = -1; }
    private sealed class IndexSource(int count) : IList
    {
        public int Count => count; public bool IsReadOnly => true; public bool IsFixedSize => true;
        public bool IsSynchronized => false; public object SyncRoot => this;
        public object this[int index] { get => index; set => throw new NotSupportedException(); }
        public int IndexOf(object value) => value is int i && i >= 0 && i < count ? i : -1;
        public bool Contains(object value) => IndexOf(value) >= 0;
        public void CopyTo(Array array, int index) { for (var i = 0; i < count; i++) array.SetValue(i, index + i); }
        public IEnumerator GetEnumerator() { for (var i = 0; i < count; i++) yield return i; }
        public int Add(object value) => throw new NotSupportedException(); public void Clear() => throw new NotSupportedException();
        public void Insert(int index, object value) => throw new NotSupportedException(); public void Remove(object value) => throw new NotSupportedException(); public void RemoveAt(int index) => throw new NotSupportedException();
    }
}

// Sparse Fenwick prefix sums: O(log N) lookup/update and storage proportional to measured items.
// The horizontal ScrollView needs only estimated bounds; it still owns all scrolling mechanics.
internal sealed class GridExtentIndex
{
    internal const float Estimate = 48;
    private readonly Dictionary<int, float> _tree = new(), _sizes = new();
    private int _count; private float _spacing;
    internal void Reset(int count, float spacing) { _count = count; _spacing = spacing; _tree.Clear(); _sizes.Clear(); }
    internal float Size(int index) => _sizes.TryGetValue(index, out var size) ? size : Estimate;
    internal void Measure(int index, float size)
    {
        if (index < 0 || index >= _count || !float.IsFinite(size) || size < 0) return;
        var delta = size - Size(index); if (Math.Abs(delta) < .01f) return;
        _sizes[index] = size;
        for (var i = index + 1; i <= _count; i += i & -i) _tree[i] = (_tree.TryGetValue(i, out var sum) ? sum : 0) + delta;
    }
    internal float Start(int index)
    {
        var sum = index * (Estimate + _spacing);
        for (var i = index; i > 0; i -= i & -i) if (_tree.TryGetValue(i, out var value)) sum += value;
        return sum;
    }
    internal float Total => _count == 0 ? 0 : Start(_count) - _spacing;
    internal int IndexAt(float offset)
    {
        var lo = 0; var hi = Math.Max(0, _count - 1);
        while (lo < hi) { var mid = lo + (hi - lo + 1) / 2; if (Start(mid) <= offset) lo = mid; else hi = mid - 1; }
        return lo;
    }
}
