using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Compose.Net;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Runtime.Adapters.Nodes;

internal sealed class LazyListNode : UnityReusableComposeNode, ILazyListLayoutNode, IReusableComposeNode
{
    internal readonly LazyListView View;
    internal readonly List<LazyListItemNode> Items = new();
    internal LazyListNode(bool horizontal) : this(new LazyListView(horizontal)) { }
    private LazyListNode(LazyListView view) : base(view) { View = view; view.Owner = this; }
    public void ConfigureLazyList(ILazyListState state, ILazyListItemProvider items, LazyListSettings settings) => View.Configure(state, items, settings);
    public new void Insert(int index, IReusableComposeNode child) => Reinsert(index, child);
    public new void Reinsert(int index, IReusableComposeNode child)
    {
        var item = (LazyListItemNode)child;
        Items.Remove(item); Items.Insert(Math.Min(index, Items.Count), item); item.Owner = this;
        View.Attach(item);
    }
    public new void Remove(IReusableComposeNode child)
    {
        var item = (LazyListItemNode)child;
        Items.Remove(item); item.VisualElement.RemoveFromHierarchy(); item.Owner = null; View.QueueLayout();
    }
    public new void FastRemove(int index, IReusableComposeNode child) => Remove(child);
    public new void Dispose() { View.Dispose(); base.Dispose(); }
}

internal sealed class LazyListItemNode : UnityReusableComposeNode, ILazyListItemNode, IReusableComposeNode
{
    internal LazyListNode? Owner;
    internal int Index = -1;
    internal object? Key, ContentType;
    internal LazyListItemNode() : base(new Box { name = "LazyListItem" })
    {
        VisualElement.style.flexShrink = 0;
        VisualElement.RegisterCallback<GeometryChangedEvent>(_ => Owner?.View.ItemMeasured(this));
    }
    public new IReusableComposeNode? Parent => Owner;
    public new int IndexInParent => Owner?.Items.IndexOf(this) ?? -1;
    public void ConfigureLazyItem(int index, object key, object? contentType)
    { Index = index; Key = key; ContentType = contentType; Owner?.View.Attach(this); }
}

/// <summary>ListView supplies vertical recycling and dynamic-height virtualization. UI Toolkit has
/// no horizontal collection view, so LazyRow adds only an indexed window to its native ScrollView.
/// Both use the toolkit's input, clipping, nested scrolling, inertia and elastic overscroll.</summary>
internal sealed class LazyListView : VisualElement, IDisposable
{
    internal LazyListNode Owner = null!;
    private readonly bool _horizontal;
    private readonly ListView? _list;
    private readonly ScrollView _scroll;
    private readonly SmoothWheelScroll _wheel;
    private readonly VisualElement _overlay = new() { name = "lazy-sticky-header" };
    private readonly Dictionary<int, Slot> _slots = new();
    private readonly ExtentIndex _extents = new();
    private ILazyListState? _state;
    private ILazyListStateAdapter Bridge => (ILazyListStateAdapter)_state!;
    private ILazyListItemProvider? _items;
    private LazyListSettings? _settings;
    private IVisualElementScheduledItem? _layout, _idle;
    private bool _syncing, _disposed, _stopped, _refreshing, _autoCross;
    private float _leading, _trailing, _crossStart, _crossEnd, _spacing, _alignmentOffset;
    private int _pinned = -1, _pointer = -1;
    private object? _anchorKey;
    private int _anchorIndex;
    private float _anchorOffset;
    private LazyListScrollRequest? _request;
    private int _settledPasses;
    private float _lastRequestTarget = float.NaN;
    private bool _animating, _requestAnimated;
    private IReadOnlyList<int> _retainedIndices = Array.Empty<int>();
    private IDragInteraction.Start? _drag;
    private float Axis(Vector2 v) => _horizontal ? v.x : v.y;
    private float Viewport => Math.Max(0, Axis(_scroll.contentViewport.layout.size));
    private float Physical => Axis(_scroll.scrollOffset);
    private Scroller Scroller => _horizontal ? _scroll.horizontalScroller : _scroll.verticalScroller;
    private int Count => _items?.Count ?? 0;
    private int PhysicalIndex(int index) => _settings!.ReverseLayout ? Count - 1 - index : index;
    private int LogicalIndex(int index) => PhysicalIndex(index);
    private float Logical(float value) => _settings!.ReverseLayout ? _state!.MaxValue - value : value;

    internal LazyListView(bool horizontal)
    {
        _horizontal = horizontal;
        name = horizontal ? "LazyRow" : "LazyColumn";
        style.minWidth = 0; style.minHeight = 0; style.overflow = Overflow.Hidden;
        if (horizontal) _scroll = new ScrollView(ScrollViewMode.Horizontal);
        else
        {
            _list = new ListView
            {
                name = "lazy-native-list", selectionType = SelectionType.None,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                fixedItemHeight = ExtentIndex.Estimate, reorderable = false,
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
        _scroll.name = "lazy-native-scroll";
        _scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        _scroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        _scroll.nestedInteractionKind = ScrollView.NestedInteractionKind.ForwardScrolling;
        _scroll.contentViewport.style.minWidth = 0; _scroll.contentViewport.style.minHeight = 0;
        _scroll.contentContainer.style.flexShrink = 0;
        if (horizontal) _scroll.contentContainer.style.flexDirection = FlexDirection.Row;
        _overlay.style.position = Position.Absolute; _overlay.style.left = 0; _overlay.style.top = 0;
        _overlay.style.right = 0; _overlay.style.bottom = 0;
        _overlay.pickingMode = UnityEngine.UIElements.PickingMode.Ignore;
        hierarchy.Add(_overlay);
        _scroll.contentViewport.RegisterCallback<GeometryChangedEvent>(_ => QueueLayout());
        _scroll.contentContainer.RegisterCallback<GeometryChangedEvent>(_ => QueueLayout());
        Scroller.valueChanged += OnNativeScroll;
        _scroll.RegisterCallback<PointerDownEvent>(_ => { _wheel?.Cancel(); ResumePhysics(); }, TrickleDown.TrickleDown);
        _scroll.RegisterCallback<WheelEvent>(OnWheelStart, TrickleDown.TrickleDown);
        // Bubble before ScrollView's default action. Inner scroll views may consume first.
        _scroll.contentContainer.RegisterCallback<WheelEvent>(GateWheel);
        _wheel = new SmoothWheelScroll(_scroll, () => _settings?.UserScrollEnabled == true, BeginWheel);
        _scroll.contentContainer.RegisterCallback<PointerMoveEvent>(GateMove);
        _scroll.contentContainer.RegisterCallback<NavigationMoveEvent>(GateNavigation);
        _scroll.RegisterCallback<NavigationMoveEvent>(e => { if (e.target == _scroll) GateNavigation(e); }, TrickleDown.TrickleDown);
        _scroll.contentContainer.RegisterCallback<PointerCaptureEvent>(OnCapture);
        _scroll.contentContainer.RegisterCallback<PointerUpEvent>(e => { if (e.pointerId == _pointer) EndDrag(false); }, TrickleDown.TrickleDown);
        _scroll.contentContainer.RegisterCallback<PointerCancelEvent>(e => { if (e.pointerId == _pointer) EndDrag(true); }, TrickleDown.TrickleDown);
        _scroll.contentContainer.RegisterCallback<PointerCaptureOutEvent>(e => { if (e.pointerId == _pointer) EndDrag(true); });
        RegisterCallback<DetachFromPanelEvent>(e => { if (e.target == this) StopNative(); });
    }

    internal void Configure(ILazyListState state, ILazyListItemProvider items, LazyListSettings settings)
    {
        if (_disposed) return;
        var oldItems = _items;
        var dataChanged = oldItems == null || oldItems.Count != items.Count ||
            Owner.Items.Any(n => n.Index >= 0 && n.Index < items.Count && !Equals(n.Key, items.GetKey(n.Index)));
        var settingsChanged = settings != _settings;
        var stateChanged = !ReferenceEquals(state, _state);
        if (stateChanged)
        {
            Unsubscribe(); _state = state;
            Bridge.ValueChanged += OnStateScroll; Bridge.ScrollRequested += OnProgramScrollRequested; Bridge.ItemsRequested += QueueLayout;
            _anchorKey = null;
            _request = Bridge.PendingScroll ?? new LazyListScrollRequest(state.FirstVisibleItemIndex, state.FirstVisibleItemScrollOffset, false, false, default);
            _settledPasses = 0; _lastRequestTarget = float.NaN; _requestAnimated = _animating = false;
        }
        _items = items; _settings = settings;
        var crossStyle = _horizontal ? style.height : style.width;
        _autoCross = crossStyle.keyword is StyleKeyword.Null or StyleKeyword.Auto;
        var d = settings.Density; var p = settings.ContentPadding;
        var left = d.ToPx(p.Left); var top = d.ToPx(p.Top); var right = d.ToPx(p.Right); var bottom = d.ToPx(p.Bottom);
        if (!float.IsFinite(left + top + right + bottom) || left < 0 || top < 0 || right < 0 || bottom < 0) throw new ArgumentOutOfRangeException(nameof(settings.ContentPadding));
        _leading = _horizontal ? left : top; _trailing = _horizontal ? right : bottom;
        _crossStart = _horizontal ? top : left; _crossEnd = _horizontal ? bottom : right;
        _spacing = settings.MainAxisArrangement is Arrangement.Spaced spaced ? d.ToPx(spaced.Space) : 0;
        if (!float.IsFinite(_spacing)) throw new ArgumentOutOfRangeException(nameof(settings.MainAxisArrangement));
        _scroll.focusable = settings.UserScrollEnabled;
        if (!settings.UserScrollEnabled) StopNative();
        ConfigurePhysics();
        if (dataChanged || settingsChanged)
        {
            var anchor = _anchorKey == null ? -1 : items.FindIndexByKey(_anchorKey, _anchorIndex);
            _extents.Reset(items.Count, _spacing);
            foreach (var node in Owner.Items)
                if (node.Index >= 0 && node.Index < Count && Equals(node.Key, items.GetKey(node.Index)))
                    _extents.Measure(PhysicalIndex(node.Index), Axis(node.VisualElement.layout.size));
            if (oldItems != null && dataChanged)
            {
                var retained = Owner.Items.Where(n => n.Key != null).Select(n => items.FindIndexByKey(n.Key!, n.Index)).Where(i => i >= 0).Distinct().OrderBy(i => i).ToArray();
                _retainedIndices = retained; Bridge.SetComposedItems(retained);
            }
            if (_list != null)
            {
                _refreshing = true;
                try
                {
                    foreach (var node in Owner.Items) node.VisualElement.RemoveFromHierarchy();
                    _slots.Clear();
                    _list.itemsSource = new IndexSource(items.Count);
                    _list.Rebuild();
                }
                finally { _refreshing = false; }
            }
            if (!stateChanged && Bridge.PendingScroll == null && anchor >= 0)
            {
                _request = new LazyListScrollRequest(anchor, _anchorOffset, false, false, default);
                _settledPasses = 0; _lastRequestTarget = float.NaN;
                _requestAnimated = _animating = false;
            }
        }
        QueueLayout();
    }

    private VisualElement MakeSlot()
    {
        var slot = new Slot(); slot.style.height = ExtentIndex.Estimate; slot.style.minHeight = 0;
        slot.RegisterCallback<GeometryChangedEvent>(_ => QueueLayout());
        slot.RegisterCallback<AttachToPanelEvent>(_ => NativeCollectionStyles.ClearRowBackground(slot));
        return slot;
    }
    private void BindSlot(VisualElement element, int physicalIndex)
    {
        NativeCollectionStyles.ClearRowBackground(element);
        if (_settings == null) return;
        var slot = (Slot)element; slot.Index = LogicalIndex(physicalIndex);
        _slots[slot.Index] = slot; ConfigureSlot(slot, physicalIndex);
        foreach (var node in Owner.Items) if (node.Index == slot.Index && Equals(node.Key, _items!.GetKey(node.Index))) Attach(node);
        QueueLayout();
    }
    private void UnbindSlot(VisualElement element, int ignored)
    {
        var slot = (Slot)element;
        if (_slots.TryGetValue(slot.Index, out var active) && active == slot) _slots.Remove(slot.Index);
        slot.Clear(); slot.Index = -1; slot.style.height = ExtentIndex.Estimate; slot.style.minHeight = 0;
        if (!_refreshing) QueueLayout();
    }
    private void ConfigureSlot(Slot slot, int physicalIndex)
    {
        slot.style.flexShrink = 0;
        slot.style.paddingTop = physicalIndex == 0 ? _leading + _alignmentOffset : 0;
        slot.style.paddingBottom = physicalIndex + 1 == Count ? _trailing : Math.Max(0, _spacing);
        slot.style.paddingLeft = _crossStart; slot.style.paddingRight = _crossEnd;
        slot.style.alignItems = Align.Stretch;
        slot.style.height = Math.Max(1, _extents.Size(physicalIndex) + slot.style.paddingTop.value.value + (physicalIndex + 1 == Count ? _trailing : _spacing));
        if (slot.childCount == 1) slot[0].style.top = slot.style.paddingTop;
    }
    internal void Attach(LazyListItemNode node)
    {
        if (_settings == null || node.Index < 0 || node.Index >= Count) return;
        var element = node.VisualElement;
        if (node.Index == _pinned && element.parent == _overlay) return;
        var box = (Box)element;
        box.UnboundedWidth = _horizontal;
        box.UnboundedHeight = !_horizontal;
        box.ContentAlignment = _horizontal
            ? _settings.CrossAxisAlignment == Alignment.Bottom ? Alignment.BottomLeft : _settings.CrossAxisAlignment == Alignment.CenterVertically ? Alignment.CenterLeft : Alignment.TopLeft
            : _settings.CrossAxisAlignment == Alignment.Right ? Alignment.TopRight : _settings.CrossAxisAlignment == Alignment.CenterHorizontally ? Alignment.TopCenter : Alignment.TopLeft;
        if (_horizontal)
        {
            element.style.position = Position.Absolute;
            element.style.top = _crossStart;
            element.style.height = _autoCross ? StyleKeyword.Auto : Math.Max(0, _scroll.contentViewport.layout.height - _crossStart - _crossEnd);
            element.style.left = _leading + _alignmentOffset + _extents.Start(PhysicalIndex(node.Index));
            if (element.parent != _scroll.contentContainer) _scroll.Add(element);
        }
        else if (_slots.TryGetValue(node.Index, out var slot))
        {
            // The native slot retains an estimated height until dynamic measurement settles.
            // An absolute intrinsic grid can grow beyond that estimate without a flex constraint.
            element.style.position = Position.Absolute;
            element.style.left = _crossStart;
            element.style.right = _crossEnd;
            element.style.top = slot.style.paddingTop;
            if (element.parent != slot) { slot.Clear(); slot.Add(element); }

        }
        if (!_syncing) QueueLayout();
    }
    internal void ItemMeasured(LazyListItemNode node) => QueueLayout();
    internal void QueueLayout()
    {
        if (_disposed || _layout != null) return;
        _layout = schedule.Execute(() => { _layout = null; LayoutItems(); });
    }
    private void LayoutItems()
    {
        if (_disposed || _state == null || _settings == null || panel == null || !float.IsFinite(Viewport) || Viewport <= 0) return;
        if (Bridge.PendingScroll is { } requested && requested != _request)
        { _retainedIndices = Bridge.ComposedItems.ToArray(); _request?.Cancel(); _request = requested; _settledPasses = 0; _lastRequestTarget = float.NaN; _animating = false; _requestAnimated = false; }
        _syncing = true;
        try
        {
            foreach (var node in Owner.Items)
            {
                if (node.Index < 0 || node.Index >= Count || node.VisualElement.panel == null || !Equals(node.Key, _items!.GetKey(node.Index))) continue;
                var measuredSize = Axis(node.VisualElement.layout.size);
                if (float.IsFinite(measuredSize) && measuredSize > 0) _extents.Measure(PhysicalIndex(node.Index), measuredSize);
            }
            if (_autoCross)
            {
                var crossSize = 0f;
                foreach (var node in Owner.Items)
                    foreach (var child in node.VisualElement.Children())
                    {
                        var extent = _horizontal ? child.layout.height : child.layout.width;
                        if (float.IsFinite(extent)) crossSize = Math.Max(crossSize, extent);
                    }
                if (_horizontal) style.minHeight = crossSize + _crossStart + _crossEnd;
                else style.minWidth = crossSize + _crossStart + _crossEnd;
            }
            var estimate = _leading + _extents.Total + _trailing;
            var arrangement = _settings.MainAxisArrangement is Arrangement.Spaced spaced ? spaced.Alignment : _settings.MainAxisArrangement;
            var free = Math.Max(0, Viewport - estimate);
            _alignmentOffset = arrangement == Arrangement.Center ? free / 2 : arrangement == Arrangement.Bottom || arrangement == Arrangement.Right ? free : 0;
            if (_horizontal)
            {
                _scroll.contentContainer.style.width = Math.Max(Viewport, estimate);
                _scroll.contentContainer.style.height = _scroll.contentViewport.layout.height;
                Bridge.UpdateLayout(Viewport, Math.Max(Viewport, estimate));
                Scroller.highValue = _state.MaxValue;
                var physical = Logical(_state.Value);
                SetPhysical(physical);
                var start = _extents.IndexAt(Math.Max(0, physical - _leading - _alignmentOffset));
                var end = _extents.IndexAt(Math.Max(0, physical + Viewport - _leading - _alignmentOffset));
                var indices = new List<int>();
                for (var i = Math.Max(0, start - 1); i < Math.Min(Count, end + 2); i++) indices.Add(LogicalIndex(i));
                AddStickyIndex(indices);
                indices.Sort(); Bridge.SetComposedItems(indices);
                foreach (var node in Owner.Items) Attach(node);
            }
            else
            {
                // The collection controller owns contentContainer.paddingTop and height.
                // Edge padding belongs to the first/last native item, outside its Compose content.
                foreach (var pair in _slots)
                {
                    ConfigureSlot(pair.Value, PhysicalIndex(pair.Key));

                }
                var extent = Count == 0 ? Viewport : Math.Max(Viewport, _scroll.contentContainer.layout.height);
                Bridge.UpdateLayout(Viewport, extent);
                var indices = _slots.Keys.Where(i => i >= 0 && i < Count).ToList();
                if (_request?.Completion.IsCompleted == false)
                    foreach (var retained in _retainedIndices) if (retained >= 0 && retained < Count && !indices.Contains(retained)) indices.Add(retained);
                AddStickyIndex(indices); indices.Sort(); Bridge.SetComposedItems(indices);
            }
            PositionRequest();
            Publish();
        }
        finally { _syncing = false; }
    }
    private void AddStickyIndex(List<int> indices)
    {
        _pinned = -1;
        var first = _state!.FirstVisibleItemIndex;
        foreach (var index in _items!.StickyHeaderIndices)
        { if (index > first) break; _pinned = index; }
        if (_pinned >= 0 && !indices.Contains(_pinned)) indices.Add(_pinned);
    }
    private void Publish()
    {
        var visible = new List<LazyListItemInfo>();
        var first = int.MaxValue; var firstOffset = 0f;
        foreach (var node in Owner.Items)
        {
            if (node.Index < 0 || node.Index >= Count || node.VisualElement.panel == null || !Equals(node.Key, _items!.GetKey(node.Index))) continue;
            var size = Axis(node.VisualElement.layout.size);
            if (!float.IsFinite(size) || size <= 0) continue;
            var offset = ItemPhysicalStart(node) - Physical;
            if (offset + size <= 0 || offset >= Viewport || size <= 0) continue;
            visible.Add(new(node.Index, _items!.GetKey(node.Index), offset - _leading, size, _items.GetContentType(node.Index)));
            var logicalOffset = _settings!.ReverseLayout ? Viewport - offset - size - _trailing : offset - _leading;
            if (node.Index < first) { first = node.Index; firstOffset = Math.Max(0, -logicalOffset); }
        }
        visible.Sort((a, b) => a.Index.CompareTo(b.Index));
        if (first == int.MaxValue) first = Count == 0 ? 0 : Math.Min(_state!.FirstVisibleItemIndex, Count - 1);
        if (Count == 0 && Bridge.PendingScroll is { } pending)
        { first = pending.Index; firstOffset = pending.ScrollOffset; }
        if (first != _state!.FirstVisibleItemIndex && _items!.StickyHeaderIndices.Count > 0) QueueLayout();
        _anchorIndex = first; _anchorOffset = firstOffset;
        _anchorKey = Count == 0 ? null : _items!.GetKey(first);
        var before = _settings!.ReverseLayout ? _trailing : _leading;
        var after = _settings.ReverseLayout ? _leading : _trailing;
        PinHeader();
        var pinned = Owner.Items.FirstOrDefault(n => n.Index == _pinned && n.VisualElement.parent == _overlay);
        if (pinned != null && _pinned >= 0 && _pinned < Count && float.IsFinite(Axis(pinned.VisualElement.layout.size)) && Axis(pinned.VisualElement.layout.size) > 0)
        {
            visible.RemoveAll(item => item.Index == _pinned);
            var offset = _horizontal ? pinned.VisualElement.style.left.value.value : pinned.VisualElement.style.top.value.value;
            visible.Add(new(_pinned, _items!.GetKey(_pinned), offset - _leading, Axis(pinned.VisualElement.layout.size), _items.GetContentType(_pinned)));
            visible.Sort((a, b) => a.Index.CompareTo(b.Index));
        }
        Bridge.PublishLayout(new(visible.AsReadOnly(), Count,
            new Size(_scroll.contentViewport.layout.width, _scroll.contentViewport.layout.height),
            -before, Viewport - before, _settings.Orientation, _settings.ReverseLayout, before, after, _spacing), first, firstOffset);
    }
    private float ItemPhysicalStart(LazyListItemNode node)
    {
        if (_horizontal) return _leading + _alignmentOffset + _extents.Start(PhysicalIndex(node.Index));
        if (_slots.TryGetValue(node.Index, out var slot))
            return slot.worldBound.y + slot.resolvedStyle.paddingTop - _scroll.contentViewport.worldBound.y + Physical;
        return _leading + _alignmentOffset + _extents.Start(PhysicalIndex(node.Index));
    }
    private void PinHeader()
    {
        foreach (var node in Owner.Items)
        {
            // The overlay shares the keyed composition; the native slot retains its measured extent.
            if (node.Index != _pinned) { node.VisualElement.style.translate = StyleKeyword.Null; continue; }
            var start = ItemPhysicalStart(node) - Physical;
            var size = Axis(node.VisualElement.layout.size);
            if (!float.IsFinite(size) || size <= 0) continue;
            var desired = _settings!.ReverseLayout ? Math.Min(start, Viewport - size) : Math.Max(start, 0);
            foreach (var other in Owner.Items)
                if (other.Index > node.Index && _items!.StickyHeaderIndices.Contains(other.Index))
                {
                    var next = ItemPhysicalStart(other) - Physical;
                    if (other.VisualElement.panel != null && (_settings.ReverseLayout ? next <= Viewport : next >= 0))
                        desired = _settings.ReverseLayout ? Math.Max(desired, next + Axis(other.VisualElement.layout.size)) : Math.Min(desired, next - size);
                }
            node.VisualElement.style.position = Position.Absolute;
            node.VisualElement.style.left = _horizontal ? desired : _crossStart;
            node.VisualElement.style.top = _horizontal ? _crossStart : desired;
            if (!_horizontal) node.VisualElement.style.width = Math.Max(0, layout.width - _crossStart - _crossEnd);
            if (node.VisualElement.parent != _overlay) _overlay.Add(node.VisualElement);
            node.VisualElement.style.translate = StyleKeyword.Null;
            node.VisualElement.BringToFront();
        }
    }
    private void PositionRequest()
    {
        if (_request == null || _request.Completion.IsCompleted || _animating) return;
        if (Count == 0) return; // Keep an initial/requested index while asynchronous data is empty.
        var index = Math.Min(_request.Index, Count - 1);
        var node = Owner.Items.FirstOrDefault(n => n.Index == index);
        var measured = node != null && node.VisualElement.panel != null && Axis(node.VisualElement.layout.size) > 0 && (_horizontal || _slots.TryGetValue(index, out var targetSlot) && (node.VisualElement.parent == targetSlot || index == _pinned && node.VisualElement.parent == _overlay));
        if (_request.Animated && !_requestAnimated)
        {
            var startEstimate = measured ? ItemPhysicalStart(node!) : _leading + _extents.Start(PhysicalIndex(index));
            var sizeEstimate = measured ? Axis(node!.VisualElement.layout.size) : _extents.Size(PhysicalIndex(index));
            var targetEstimate = _settings!.ReverseLayout ? startEstimate + sizeEstimate - Viewport + _trailing - _request.ScrollOffset : startEstimate - _leading + _request.ScrollOffset;
            if (_request.EnsureVisible && measured)
                targetEstimate = startEstimate - _request.ScrollOffset < Physical ? startEstimate - _request.ScrollOffset : startEstimate + sizeEstimate + _request.ScrollOffset > Physical + Viewport ? startEstimate + sizeEstimate + _request.ScrollOffset - Viewport : Physical;
            _requestAnimated = true; _animating = true;
            _ = AnimateRequest(_request, Logical(Math.Clamp(targetEstimate, 0, _state!.MaxValue))); return;
        }
        if (!measured)
        {
            if (_list != null) _list.ScrollToItem(PhysicalIndex(index));
            else
            {
                var estimated = _leading + _extents.Start(PhysicalIndex(index));
                var target = _settings!.ReverseLayout ? estimated + _extents.Size(PhysicalIndex(index)) - Viewport + _trailing - _request.ScrollOffset : estimated - _leading + _request.ScrollOffset;
                Bridge.SetNativeValue(Logical(Math.Clamp(target, 0, _state!.MaxValue)));
                SetPhysical(Logical(_state!.Value));
            }
            _settledPasses = 0; QueueLayout(); return;
        }
        var start = ItemPhysicalStart(node!); var size = Axis(node!.VisualElement.layout.size);
        var physicalTarget = _settings!.ReverseLayout ? start + size - Viewport + _trailing - _request.ScrollOffset : start - _leading + _request.ScrollOffset;
        if (_request.EnsureVisible)
            physicalTarget = start - _request.ScrollOffset < Physical ? start - _request.ScrollOffset : start + size + _request.ScrollOffset > Physical + Viewport ? start + size + _request.ScrollOffset - Viewport : Physical;
        physicalTarget = Math.Clamp(physicalTarget, 0, _state!.MaxValue);
        SetPhysical(physicalTarget); Bridge.SetNativeValue(Logical(physicalTarget));
        // Native dynamic-height virtualization may refine its estimate on the following layout.
        if (Math.Abs(physicalTarget - _lastRequestTarget) < .2f && Math.Abs(Physical - physicalTarget) < .2f) _settledPasses++;
        else _settledPasses = 0;
        _lastRequestTarget = physicalTarget;
        if (_settledPasses >= 8) { _request.Complete(); _retainedIndices = Array.Empty<int>(); }
        QueueLayout();
    }
    private async Task AnimateRequest(LazyListScrollRequest request, float target)
    {
        try { await _state!.AnimateScrollTo(target, request.AnimationSpec); if (_request == request) { _animating = false; QueueLayout(); } }
        catch (OperationCanceledException) { request.Cancel(); _animating = false; }
        catch (Exception exception) { request.Fail(exception); _animating = false; }
    }
    private void SetPhysical(float value) => _scroll.scrollOffset = _horizontal ? new Vector2(value, 0) : new Vector2(0, value);
    private void OnStateScroll(float value)
    {
        if (_syncing || _state == null || _settings == null) return;
        _syncing = true;
        try { SetPhysical(Logical(value)); }
        finally { _syncing = false; }
        QueueLayout();
    }
    private void OnNativeScroll(float ignored)
    {
        if (_syncing || _refreshing || _animating || _state == null || _settings == null) return;
        if (Math.Abs(Math.Max(0, Scroller.highValue) - _state.MaxValue) > .6f) { QueueLayout(); return; }
        _syncing = true;
        try { Bridge.SetNativeValue(Logical(Physical)); Bridge.SetNativeScrollInProgress(true); }
        finally { _syncing = false; }
        _idle?.Pause(); _idle = schedule.Execute(() => { if (_drag == null && !_disposed) Bridge.SetNativeScrollInProgress(false); }).StartingIn(80);
        QueueLayout();
    }
    private void ConfigurePhysics()
    {
        if (_settings == null) return;
        _scroll.scrollDecelerationRate = _settings.UserScrollEnabled && !_stopped ? _settings.FlingBehavior?.DecelerationRate ?? .135f : 0;
        _scroll.touchScrollBehavior = _settings.OverscrollEffect == null || !_settings.UserScrollEnabled || _stopped ? ScrollView.TouchScrollBehavior.Clamped : ScrollView.TouchScrollBehavior.Elastic;
        if (_settings.OverscrollEffect != null) _scroll.elasticity = _settings.OverscrollEffect.Elasticity;
        if (_wheel?.IsActive == true)
        {
            _scroll.scrollDecelerationRate = 0;
            _scroll.touchScrollBehavior = ScrollView.TouchScrollBehavior.Clamped;
        }
    }
    private void OnProgramScrollRequested()
    {
        // A raw pixel request also supersedes an outstanding item jump. The adapter's own
        // AnimateScrollTo call is part of the current item request and must retain it.
        if (!_animating) { _request?.Cancel(); Bridge.PendingScroll?.Cancel(); }
        StopNative();
    }
    private void BeginWheel()
    {
        StopNative();
        _syncing = true;
        try { Bridge.SetNativeValue(Logical(Physical)); Bridge.SetNativeScrollInProgress(true); }
        finally { _syncing = false; }
    }
    private void StopNative()
    {
        _wheel?.Cancel();
        _stopped = true; ConfigurePhysics();
        if (_pointer >= 0) _scroll.contentContainer.ReleasePointer(_pointer);
        EndDrag(true); _idle?.Pause(); if (_state != null) Bridge.SetNativeScrollInProgress(false);
    }
    private void ResumePhysics()
    {
        if (_settings?.UserScrollEnabled != true) return;
        if (_request?.Completion.IsCompleted == false) { _request.Cancel(); _state!.ScrollTo(_state.Value); _animating = false; }
        _stopped = false; ConfigurePhysics();
    }
    private void OnWheelStart(WheelEvent e)
    {
        ResumePhysics();
        if (e.target == _scroll || e.target == _scroll.contentViewport || e.target == _scroll.contentContainer) GateWheel(e);
    }
    private void GateWheel(WheelEvent e)
    {
        if (_settings?.UserScrollEnabled == true) return;
        e.StopPropagation(); if (parent == null) return;
        using var next = WheelEvent.GetPooled(e); next.target = parent; parent.SendEvent(next);
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
        StopNative(); Bridge.ValueChanged -= OnStateScroll; Bridge.ScrollRequested -= OnProgramScrollRequested; Bridge.ItemsRequested -= QueueLayout;
        _request?.Cancel();
    }
    public void Dispose()
    {
        if (_disposed) return;
        Unsubscribe(); _disposed = true; _layout?.Pause(); _idle?.Pause();
        Scroller.valueChanged -= OnNativeScroll; _slots.Clear();
    }
    private sealed class Slot : VisualElement { internal int Index = -1; }
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
internal sealed class ExtentIndex
{
    internal const float Estimate = 48;
    private readonly Dictionary<int, float> _tree = new(), _sizes = new();
    private int _count; private float _spacing;
    internal void Reset(int count, float spacing) { _count = count; _spacing = spacing; _tree.Clear(); _sizes.Clear(); }
    internal float Size(int index) => _sizes.TryGetValue(index, out var size) ? size : Estimate;
    internal void Measure(int index, float size)
    {
        if (index < 0 || index >= _count || !float.IsFinite(size) || size < 0) return;
        var delta = Math.Max(1, size + _spacing) - Math.Max(1, Size(index) + _spacing);
        _sizes[index] = size; if (Math.Abs(delta) < .01f) return;
        for (var i = index + 1; i <= _count; i += i & -i) _tree[i] = (_tree.TryGetValue(i, out var sum) ? sum : 0) + delta;
    }
    internal float Start(int index)
    {
        var sum = index * Math.Max(1, Estimate + _spacing);
        for (var i = index; i > 0; i -= i & -i) if (_tree.TryGetValue(i, out var value)) sum += value;
        return sum;
    }
    internal float Total => _count == 0 ? 0 : Start(_count - 1) + Size(_count - 1);
    internal int IndexAt(float offset)
    {
        var lo = 0; var hi = Math.Max(0, _count - 1);
        while (lo < hi) { var mid = lo + (hi - lo + 1) / 2; if (Start(mid) <= offset) lo = mid; else hi = mid - 1; }
        return lo;
    }
}
