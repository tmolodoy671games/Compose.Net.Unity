using System;
using System.Collections.Generic;
using System.Linq;
using Compose.Net;
using Compose.Net.Unity.Runtime.Adapters.Extensions.Values;
using Compose.Net.Unity.Runtime.Adapters.Nodes;
using UnityEngine;
using UnityEngine.UIElements;
using Rect = Compose.Net.Rect;
using PickingMode = UnityEngine.UIElements.PickingMode;

namespace Compose.Net.Unity;

public partial class UnityReusableComposeNode : ISharedTransitionRootNode
{
    internal SharedVisual? SharedVisual;
    internal VisualElement LayoutElement => SharedVisual?.Slot ?? VisualElement;
    public ISharedTransitionRoot CreateSharedTransitionRoot(SharedTransitionScope scope) => new SharedTransitionRoot(VisualElement);
}

internal sealed class SharedTransitionRoot : ISharedTransitionRoot
{
    internal readonly VisualElement Element;
    internal readonly VisualElement Overlay = new() { name = "shared-transition-overlay", pickingMode = UnityEngine.UIElements.PickingMode.Ignore };
    internal readonly List<SharedVisual> Items = new();
    internal SharedTransitionRoot(VisualElement element)
    {
        Element = element;
        Overlay.style.position = Position.Absolute;
        Overlay.style.left = Overlay.style.top = Overlay.style.right = Overlay.style.bottom = 0;
        element.Add(Overlay);
    }
    public Size Size => new(Element.layout.width, Element.layout.height);
    public ISharedContentHost Attach(IReusableComposeNode node, SharedContent content)
    {
        var visual = new SharedVisual(this, (UnityReusableComposeNode)node, content);
        Items.Add(visual); return visual;
    }
    internal void Sort()
    {
        Overlay.BringToFront();
        // With equal drawing order, the visible destination must receive input after a reversal.
        foreach (var item in Items.Where(item => item.InOverlay).OrderBy(item => item.Frame.ZIndex == 0 && item.ParentSharedContentState == null &&
                item.IsSharedContent ? -1 : item.Frame.ZIndex).ThenBy(item => item.Depth).ThenBy(item => item.IsVisible))
            item.Layer.BringToFront();
    }
    public void Dispose()
    {
        foreach (var item in Items.OrderByDescending(item => item.Depth).ToArray()) item.Dispose();
        Overlay.RemoveFromHierarchy();
    }
}

// Native hierarchy, Yoga layout, transforms and rounded overflow clips do all rendering.
// The placeholder is also the composition node's logical location while its view is in the overlay.
internal sealed class SharedVisual : ISharedContentHost
{
    private readonly SharedTransitionRoot _root;
    private readonly UnityReusableComposeNode _node;
    private readonly SharedContent _content;
    private readonly VisualElement _element;
    private readonly VisualElement _placement = new() { pickingMode = UnityEngine.UIElements.PickingMode.Ignore };
    private readonly VisualElement _resize = new() { name = "shared-transition-resize", pickingMode = UnityEngine.UIElements.PickingMode.Ignore, usageHints = UsageHints.DynamicTransform };
    private readonly VisualElement _motion = new() { name = "shared-transition-motion", pickingMode = UnityEngine.UIElements.PickingMode.Ignore };
    internal readonly VisualElement Layer = new() { name = "shared-transition-layer", pickingMode = UnityEngine.UIElements.PickingMode.Ignore, usageHints = UsageHints.DynamicTransform };
    internal VisualElement? Slot { get; private set; }
    internal SharedContentFrame Frame;
    internal bool IsVisible => _content.IsVisible;
    internal bool IsSharedContent => _content.IsSharedBounds || _content.IsSharedElement;
    internal bool InOverlay => Slot != null && Layer.parent == _root.Overlay;
    private LayoutStyle? _saved;
    private IPath? _clipPath, _resolvedClip;
    public IPath? ClipPathInOverlay => Frame.RenderInOverlay ? _resolvedClip : null;
    private StyleLength _renderWidth, _renderHeight;
    private Vector2 _natural, _naturalParentSize;
    private VisualElement? _layoutParent;
    private SharedVisual? _layoutAncestor;
    private Vector2 _offsetInAncestor;
    private bool _disposed, _hidden, _restorePending;
    private StyleEnum<Visibility> _visibility;
    private readonly IVisualElementScheduledItem _measure;
    private SharedVisual? Parent
    {
        get
        {
            for (var current = (Slot ?? _element).parent; current != null && current != _root.Element; current = current.parent)
            {
                var candidate = _root.Items.FirstOrDefault(item => item != this && item._element == current);
                if (candidate != null) return candidate;
            }
            return null;
        }
    }
    internal int Depth => Parent is { } parent ? parent.Depth + 1 : 0;
    public SharedContentState? ParentSharedContentState
    {
        get
        {
            for (var ancestor = Parent; ancestor != null; ancestor = ancestor.Parent)
                if (ancestor._content.IsSharedBounds) return ancestor._content.State;
            return null;
        }
    }
    public IReusableComposeNode TransitionNode { get; }
    internal SharedVisual(SharedTransitionRoot root, UnityReusableComposeNode node, SharedContent content)
    {
        if (node.SharedVisual != null)
            throw new InvalidOperationException("Use a separate layout for each shared or overlay modifier.");
        _root = root; _node = node; _element = node.VisualElement; _content = content;
        node.SharedVisual = this;
        Layer.style.position = Position.Absolute;
        _motion.style.position = Position.Absolute;
        _placement.style.position = Position.Absolute;
        _motion.style.left = _motion.style.top = 0;
        _resize.style.position = Position.Absolute;
        _resize.style.left = _resize.style.top = 0;
        _resize.style.transformOrigin = new UnityEngine.UIElements.TransformOrigin(0, 0);
        Layer.Add(_placement); _placement.Add(_motion); _motion.Add(_resize);
        // These are drawing bounds, not constraints for the content's natural layout.
        // Box must measure scaled contents at their destination size, even while the
        // overlay is smaller. UI Toolkit still performs their layout and transforms.
        foreach (var wrapper in new[] { Layer, _placement, _motion, _resize })
            Box.SetAnimatedConstraints(wrapper, Box.AnimatedConstraints.Width | Box.AnimatedConstraints.Height, true);
        TransitionNode = new UnityReusableComposeNode(_motion);
        _element.RegisterCallback<GeometryChangedEvent>(OnGeometry);
        _element.RegisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
        // Ancestor scrolling/placement doesn't emit a GeometryChangedEvent on this element.
        _measure = _element.schedule.Execute(Measure).Every(16);
    }
    internal static StyleLength AuthoredSize(VisualElement element, bool width)
    {
        var saved = (element.GetReusableComposeNode() as UnityReusableComposeNode)?.SharedVisual?._saved;
        return saved != null ? width ? saved.Width : saved.Height : width ? element.style.width : element.style.height;
    }
    internal static void SetAuthoredSize(VisualElement element, bool width, StyleLength value)
    {
        var shared = (element.GetReusableComposeNode() as UnityReusableComposeNode)?.SharedVisual;
        if (shared?._saved != null)
        {
            // An authored target can equal today's animated size. Track the write explicitly;
            // comparing style values cannot distinguish it from our last render in that case.
            if (width) { shared._saved.Width = value; shared.Slot!.style.width = value; }
            else { shared._saved.Height = value; shared.Slot!.style.height = value; }
            Box.Refresh(shared.Slot!);
            shared.Measure();
        }
        else if (width) element.style.width = value;
        else element.style.height = value;
    }
    internal static StyleLength AuthoredBound(VisualElement element, Box.BoundProperty property)
    {
        var saved = (element.GetReusableComposeNode() as UnityReusableComposeNode)?.SharedVisual?._saved;
        return saved?.GetBound(property) ?? Box.AuthoredBound(element, property);
    }
    internal static void SetAuthoredBound(VisualElement element, Box.BoundProperty property, StyleLength value)
    {
        var shared = (element.GetReusableComposeNode() as UnityReusableComposeNode)?.SharedVisual;
        if (shared?._saved != null)
        {
            shared._saved.SetBound(property, value);
            Box.SetAuthoredBound(shared.Slot!, property, value); Box.Refresh(shared.Slot!);
            shared.Measure();
        }
        else Box.SetAuthoredBound(element, property, value);
    }
    private void OnGeometry(GeometryChangedEvent _) => Measure();
    private void OnCaptureOut(PointerCaptureOutEvent _)
    {
        if (_restorePending) _element.schedule.Execute(() => { if (_restorePending) End(); });
    }
    private bool HasCapturedPointer()
    {
        if (_element.panel is not { } panel) return false;
        for (var id = 0; id < UnityEngine.UIElements.PointerId.maxPointers; id++)
            if (panel.GetCapturingElement(id) is VisualElement captured &&
                (captured == _element || _element.Contains(captured))) return true;
        return false;
    }
    private Vector2 PositionInRoot(VisualElement view)
    {
        var point = Vector2.zero;
        for (var current = view; current != null && current != _root.Element;)
        {
            var moved = _root.Items.FirstOrDefault(item => item._element == current && item.Slot != null);
            if (moved != null) current = moved.Slot!;
            point += current.layout.position;
            // Native ScrollView offsets are transforms, unlike AnimatedContent's enter/exit layers.
            if (current.name == "unity-content-container" && current.parent?.name == "unity-content-viewport")
                point += (Vector2)current.resolvedStyle.translate;
            current = current.parent;
        }
        return point;
    }
    public void Measure()
    {
        if (_disposed || _element.panel == null) return;
        if (_restorePending) End();
        // Retained navigation entries collapse their native layout while hidden. Keep the
        // last natural bounds until the entry returns and is measured at its real size.
        for (var current = Slot ?? _element; current != null && current != _root.Element; current = current.parent)
        {
            var moved = _root.Items.FirstOrDefault(item => item._element == current && item.Slot != null);
            if (moved != null) current = moved.Slot!;
            if (current is AnimatedContentItem { Hidden: true }) return;
        }
        if (_saved != null)
        {
            // Declarative changes during a transition become fresh target constraints.
            if (_element.style.width != _renderWidth) { _saved.Width = _element.style.width; Slot!.style.width = _saved.Width; }
            if (_element.style.height != _renderHeight) { _saved.Height = _element.style.height; Slot!.style.height = _saved.Height; }
            _saved.CaptureConstraints(_element);
            var ancestorAnimating = _layoutAncestor is { Slot: not null };
            var available = ancestorAnimating ? _naturalParentSize : Slot!.parent.contentRect.size;
            if (!ancestorAnimating && Frame.PlaceholderSize == PlaceholderSize.ContentSize)
            {
                // The logical slot still participates in native flex/grid layout. Its used
                // dimensions include flex-shrink, percentages and min/max constraints.
                var size = Slot!.layout.size;
                if (float.IsFinite(size.x) && float.IsFinite(size.y)) _natural = size;
            }
            else _natural = _saved.ResolveSize(_natural, available, Slot!.parent is Box);
        }
        else _natural = _element.layout.size;
        var position = PositionInRoot(Slot ?? _element);
        if (_saved == null)
        {
            _layoutAncestor = Parent;
            _naturalParentSize = _element.parent?.contentRect.size ?? _natural;
            if (_layoutAncestor?._content.LayoutBounds is { } ancestorBounds)
                _offsetInAncestor = position - new Vector2(ancestorBounds.Offset.X, ancestorBounds.Offset.Y);
        }
        else if (_layoutAncestor is { Slot: not null } ancestor &&
                 ancestor._content.LayoutBounds is { } ancestorBounds)
        {
            // Reparenting/remeasuring a shared ancestor changes its live layout.
            // Keep the child's natural local destination, and follow only the
            // ancestor's natural root position, not its animated drawing bounds.
            position = new Vector2(ancestorBounds.Offset.X, ancestorBounds.Offset.Y) + _offsetInAncestor;
        }
        _content.Measure(new Rect(new Offset(position.x, position.y), new Size(_natural.x, _natural.y)));
    }
    public void Render(SharedContentFrame frame)
    {
        if (_disposed) return;
        Frame = frame;
        if (frame.Active || Slot != null && HasCapturedPointer())
        {
            Begin();
            var parent = frame.RenderInOverlay ? _root.Overlay : Slot!;
            if (Layer.parent != parent) parent.Add(Layer);
            var bounds = frame.Bounds;
            var origin = frame.RenderInOverlay ? Vector2.zero : PositionInRoot(Slot!);
            var clip = frame.Clip;
            var clipBounds = bounds;
            if (clip.InheritParent)
            {
                for (var ancestor = Parent; ancestor != null; ancestor = ancestor.Parent)
                {
                    if (!ancestor._content.IsSharedBounds || !ancestor.Frame.Active || ancestor.Frame.Clip.InheritParent) continue;
                    clip = ancestor.Frame.Clip; clipBounds = ancestor.Frame.Bounds; break;
                }
            }
            var clipping = !clip.Shape.IsUnspecified;
            _resolvedClip = clipping ? ResolveClip(clip.Shape, clipBounds) : null;
            var layerBounds = clipping ? clipBounds : bounds;
            Layer.style.left = layerBounds.Offset.X - origin.x; Layer.style.top = layerBounds.Offset.Y - origin.y;
            Layer.style.width = layerBounds.Size.Width; Layer.style.height = layerBounds.Size.Height;
            Layer.style.overflow = clipping ? Overflow.Hidden : Overflow.Visible;
            if (clipping)
            {
                RoundedCorners.Apply(Layer, clip.Shape, new Vector2(layerBounds.Size.Width, layerBounds.Size.Height));
            }
            _placement.style.left = bounds.Offset.X - layerBounds.Offset.X;
            _placement.style.top = bounds.Offset.Y - layerBounds.Offset.Y;
            _motion.style.width = bounds.Size.Width; _motion.style.height = bounds.Size.Height;
            _placement.style.width = bounds.Size.Width; _placement.style.height = bounds.Size.Height;
            var mode = frame.ResizeMode;
            var sx = 1f; var sy = 1f; var dx = 0f; var dy = 0f;
            var width = mode.Remeasure ? bounds.Size.Width : _natural.x;
            var height = mode.Remeasure ? bounds.Size.Height : _natural.y;
            if (!mode.Remeasure && width > 0 && height > 0)
            {
                sx = bounds.Size.Width / width; sy = bounds.Size.Height / height;
                switch (mode.ContentScale)
                {
                    case ContentScale.FillWidth: sy = sx; break;
                    case ContentScale.FillHeight: sx = sy; break;
                    case ContentScale.Fit: sx = sy = Math.Min(sx, sy); break;
                    case ContentScale.Crop: sx = sy = Math.Max(sx, sy); break;
                    case ContentScale.None: sx = sy = 1; break;
                    case ContentScale.Inside: sx = sy = Math.Min(1, Math.Min(sx, sy)); break;
                }
                dx = (bounds.Size.Width - width * sx) * AnimatedContentLayout.Bias(mode.Alignment, true);
                dy = (bounds.Size.Height - height * sy) * AnimatedContentLayout.Bias(mode.Alignment, false);
            }
            _element.style.width = _renderWidth = width; _element.style.height = _renderHeight = height;
            // Keep authored Scale/Offset/TransformOrigin on the live content. Shared resizing
            // belongs to its own layer, otherwise entering/exiting the overlay resets them.
            _resize.style.width = width; _resize.style.height = height;
            _resize.style.translate = new Translate(dx, dy);
            _resize.style.scale = new Scale(new Vector3(sx, sy, 1));
            UpdatePlaceholder(bounds.Size);
            _root.Sort();
            _restorePending = !frame.Active;
        }
        else End();
        if (frame.Hidden != _hidden)
        {
            if (frame.Hidden) { _visibility = _element.style.visibility; _element.style.visibility = Visibility.Hidden; }
            else _element.style.visibility = _visibility;
            _hidden = frame.Hidden;
        }
    }
    private void UpdatePlaceholder(Size animatedSize)
    {
        var slot = Slot!;
        if (Frame.PlaceholderSize == PlaceholderSize.ContentSize)
        {
            // Preserve the original flex basis. Using an already shrunken width as the new
            // basis shrinks it a second time and turns an ordinary return into a retarget.
            slot.style.width = _saved!.Width.keyword == StyleKeyword.Undefined ? _saved.Width : new StyleLength(_natural.x);
            slot.style.height = _saved.Height.keyword == StyleKeyword.Undefined ? _saved.Height : new StyleLength(_natural.y);
            slot.style.flexGrow = _element.style.flexGrow; slot.style.flexShrink = _element.style.flexShrink;
            slot.style.flexBasis = _element.style.flexBasis;
        }
        else
        {
            var placeholder = Frame.PlaceholderSize.CalculateSize(new Size(_natural.x, _natural.y), animatedSize);
            slot.style.width = placeholder.Width; slot.style.height = placeholder.Height;
            slot.style.flexGrow = slot.style.flexShrink = 0; slot.style.flexBasis = StyleKeyword.Auto;
        }
    }
    private IPath ResolveClip(Shape shape, Rect bounds)
    {
        var p = (_clipPath ??= ComposeFunctions.Path()).Reset();
        var radii = RoundedCorners.Resolve(shape, new Vector2(bounds.Size.Width, bounds.Size.Height));
        var x = bounds.Offset.X; var y = bounds.Offset.Y;
        var r = x + bounds.Size.Width; var b = y + bounds.Size.Height;
        var tl = radii.x; var tr = radii.y; var bl = radii.z; var br = radii.w;
        const float k = .55228475f;
        p.MoveTo(x + tl, y).LineTo(r - tr, y).CubicTo(r - tr + tr*k, y, r, y + tr - tr*k, r, y + tr)
            .LineTo(r, b - br).CubicTo(r, b - br + br*k, r - br + br*k, b, r - br, b)
            .LineTo(x + bl, b).CubicTo(x + bl - bl*k, b, x, b - bl + bl*k, x, b - bl)
            .LineTo(x, y + tl).CubicTo(x, y + tl - tl*k, x + tl - tl*k, y, x + tl, y).Close();
        return p;
    }
    private void Begin()
    {
        _restorePending = false;
        if (Slot != null || _element.parent == null) return;
        var parent = _element.parent;
        var index = parent.IndexOf(_element);
        var align = _element.style.alignSelf;
        var justify = _element.style.justifySelf;
        (parent as ILayout)?.Untrack(_element);
        // Untrack first: Box may have temporarily assigned intrinsic text dimensions
        // and incoming constraints. Restore the authored styles, not those overrides.
        _saved = new LayoutStyle(_element);
        Slot = new VisualElement { name = "shared-transition-placeholder", pickingMode = UnityEngine.UIElements.PickingMode.Ignore };
        Slot.SetReusableComposeNode(_node);
        _saved.Apply(Slot);
        Slot.style.alignSelf = align; Slot.style.justifySelf = justify;
        UpdatePlaceholder(Frame.Bounds.Size);
        Slot.RegisterCallback<GeometryChangedEvent>(OnGeometry);
        _element.RemoveFromHierarchy(); parent.Insert(index, Slot); (parent as ILayout)?.Track(Slot);
        _layoutParent = parent;
        _layoutParent.RegisterCallback<GeometryChangedEvent>(OnGeometry);
        _resize.Add(_element);
        _element.style.position = Position.Absolute;
        _element.style.left = _element.style.top = 0;
        _element.style.right = _element.style.bottom = StyleKeyword.Auto;
        _element.style.marginLeft = _element.style.marginRight = _element.style.marginTop = _element.style.marginBottom = 0;
        _element.style.minWidth = _element.style.minHeight = 0;
        _element.style.maxWidth = _element.style.maxHeight = StyleKeyword.None;

    }
    private void End(bool force = false)
    {
        if (Slot == null) return;
        // Native reparenting releases pointer capture. Finish the current gesture
        // before restoring the hierarchy; disposal still cancels input normally.
        _restorePending = !force && HasCapturedPointer();
        if (_restorePending) return;
        _layoutParent?.UnregisterCallback<GeometryChangedEvent>(OnGeometry); _layoutParent = null;
        var slot = Slot; slot.UnregisterCallback<GeometryChangedEvent>(OnGeometry); var parent = slot.parent; var index = parent?.IndexOf(slot) ?? 0;
        _element.RemoveFromHierarchy();
        (parent as ILayout)?.Untrack(slot); slot.RemoveFromHierarchy();
        _saved!.Apply(_element, restore: true); _saved = null; Slot = null;
        if (parent != null) { parent.Insert(index, _element); (parent as ILayout)?.Track(_element); }
        Layer.RemoveFromHierarchy(); _resolvedClip = null;
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true; _measure.Pause(); _element.UnregisterCallback<GeometryChangedEvent>(OnGeometry);
        _element.UnregisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
        End(force: true); if (_hidden) _element.style.visibility = _visibility;
        _clipPath?.Dispose(); _clipPath = _resolvedClip = null;
        _node.SharedVisual = null; _root.Items.Remove(this);
    }
    private sealed class LayoutStyle
    {
        internal StyleLength Width, Height;
        private StyleLength _minW, _minH, _maxW, _maxH;
        private readonly StyleLength _left, _top, _right, _bottom, _ml, _mr, _mt, _mb, _basis;
        private readonly StyleEnum<Position> _position;
        private readonly StyleFloat _grow, _shrink;
        private readonly StyleEnum<Align> _align, _justify;
        private readonly StyleGridLine _columnStart, _columnEnd, _rowStart, _rowEnd;
        internal LayoutStyle(VisualElement element)
        {
            var s = element.style; Width = s.width; Height = s.height;
            _minW = s.minWidth; _minH = s.minHeight; _maxW = s.maxWidth; _maxH = s.maxHeight;
            _left = s.left; _top = s.top; _right = s.right; _bottom = s.bottom; _position = s.position;
            _ml = s.marginLeft; _mr = s.marginRight; _mt = s.marginTop; _mb = s.marginBottom;
            _grow = s.flexGrow; _shrink = s.flexShrink; _basis = s.flexBasis; _align = s.alignSelf; _justify = s.justifySelf;
            _columnStart = s.gridColumnStart; _columnEnd = s.gridColumnEnd; _rowStart = s.gridRowStart; _rowEnd = s.gridRowEnd;
        }
        internal StyleLength GetBound(Box.BoundProperty property) => property switch
        {
            Box.BoundProperty.MinWidth => _minW, Box.BoundProperty.MaxWidth => _maxW,
            Box.BoundProperty.MinHeight => _minH, _ => _maxH
        };
        internal void SetBound(Box.BoundProperty property, StyleLength value)
        {
            switch (property)
            {
                case Box.BoundProperty.MinWidth: _minW = value; break;
                case Box.BoundProperty.MaxWidth: _maxW = value; break;
                case Box.BoundProperty.MinHeight: _minH = value; break;
                case Box.BoundProperty.MaxHeight: _maxH = value; break;
            }
        }
        internal void CaptureConstraints(VisualElement element)
        {
            var s = element.style;
            if (s.minWidth != new StyleLength(0f)) _minW = s.minWidth;
            if (s.minHeight != new StyleLength(0f)) _minH = s.minHeight;
            if (s.maxWidth.keyword != StyleKeyword.None) _maxW = s.maxWidth;
            if (s.maxHeight.keyword != StyleKeyword.None) _maxH = s.maxHeight;
            s.minWidth = s.minHeight = 0;
            s.maxWidth = s.maxHeight = StyleKeyword.None;
        }
        internal Vector2 ResolveSize(Vector2 natural, Vector2 available, bool constrained)
        {
            static float Length(StyleLength value, float parent, float fallback) =>
                value.keyword == StyleKeyword.Undefined
                    ? value.value.unit == LengthUnit.Percent ? parent * value.value.value / 100 : value.value.value
                    : fallback;
            static float Dimension(StyleLength value, StyleLength minimum, StyleLength maximum, float parent, float fallback, bool constrain)
            {
                if (!float.IsFinite(parent)) return fallback;
                var min = Math.Max(0, Length(minimum, parent, 0));
                var max = Length(maximum, parent, float.PositiveInfinity);
                if (constrain) max = Math.Min(max, Math.Max(0, parent));
                return Math.Max(0, Math.Max(Math.Min(min, max), Math.Min(max, Length(value, parent, fallback))));
            }
            return new Vector2(Dimension(Width, _minW, _maxW, available.x, natural.x, constrained),
                Dimension(Height, _minH, _maxH, available.y, natural.y, constrained));
        }
        internal void Apply(VisualElement element, bool restore = false)
        {
            var s = element.style; s.width = Width; s.height = Height;
            s.minWidth = _minW; s.minHeight = _minH; s.maxWidth = _maxW; s.maxHeight = _maxH;
            s.left = _left; s.top = _top; s.right = _right; s.bottom = _bottom; s.position = _position;
            s.marginLeft = _ml; s.marginRight = _mr; s.marginTop = _mt; s.marginBottom = _mb;
            // These properties were only copied to the placeholder, never overridden on
            // the content. Preserve declarative updates made while it lived in the overlay.
            if (!restore)
            {
                s.flexGrow = _grow; s.flexShrink = _shrink; s.flexBasis = _basis; s.alignSelf = _align; s.justifySelf = _justify;
                s.gridColumnStart = _columnStart; s.gridColumnEnd = _columnEnd; s.gridRowStart = _rowStart; s.gridRowEnd = _rowEnd;
            }
        }
    }
}
