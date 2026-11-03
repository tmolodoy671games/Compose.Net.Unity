using System;
using System.Collections.Generic;
using System.Linq;
using Compose.Net;
using Compose.Net.Unity.Runtime.Adapters.Extensions.Values;
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
    public ISharedContentHost Attach(IReusableComposeNode node, SharedContent content)
    {
        var visual = new SharedVisual(this, (UnityReusableComposeNode)node, content);
        Items.Add(visual); return visual;
    }
    internal void Sort()
    {
        Overlay.BringToFront();
        // With equal drawing order, the visible destination must receive input after a reversal.
        foreach (var item in Items.Where(item => item.InOverlay).OrderBy(item => item.Frame.ZIndex).ThenBy(item => item.Depth).ThenBy(item => item.IsVisible))
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
    private readonly VisualElement _motion = new() { name = "shared-transition-motion", pickingMode = UnityEngine.UIElements.PickingMode.Ignore };
    internal readonly VisualElement Layer = new() { name = "shared-transition-layer", pickingMode = UnityEngine.UIElements.PickingMode.Ignore, usageHints = UsageHints.DynamicTransform };
    internal VisualElement? Slot { get; private set; }
    internal SharedContentFrame Frame;
    internal bool IsVisible => _content.IsVisible;
    internal bool InOverlay => Slot != null && Layer.parent == _root.Overlay;
    private LayoutStyle? _saved;
    private StyleLength _renderWidth, _renderHeight;
    private Vector2 _natural;
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
    public SharedContentState? ParentSharedContentState => Parent?._content.State;
    public IReusableComposeNode TransitionNode { get; }
    internal SharedVisual(SharedTransitionRoot root, UnityReusableComposeNode node, SharedContent content)
    {
        _root = root; _node = node; _element = node.VisualElement; _content = content;
        node.SharedVisual = this;
        Layer.style.position = Position.Absolute;
        _motion.style.position = Position.Absolute;
        _placement.style.position = Position.Absolute;
        _motion.style.left = _motion.style.top = 0;
        Layer.Add(_placement); _placement.Add(_motion);
        // These are drawing bounds, not constraints for the content's natural layout.
        // Box must measure scaled contents at their destination size, even while the
        // overlay is smaller. UI Toolkit still performs their layout and transforms.
        foreach (var wrapper in new[] { Layer, _placement, _motion })
            Box.SetAnimatedConstraints(wrapper, Box.AnimatedConstraints.Width | Box.AnimatedConstraints.Height, true);
        TransitionNode = new UnityReusableComposeNode(_motion);
        _element.RegisterCallback<GeometryChangedEvent>(OnGeometry);
        _element.RegisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
        // Ancestor scrolling/placement doesn't emit a GeometryChangedEvent on this element.
        _measure = _element.schedule.Execute(Measure).Every(16);
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
        if (_saved != null)
        {
            // Declarative changes during a transition become fresh target constraints.
            if (_element.style.width != _renderWidth) { _saved.Width = _element.style.width; Slot!.style.width = _saved.Width; }
            if (_element.style.height != _renderHeight) { _saved.Height = _element.style.height; Slot!.style.height = _saved.Height; }
            if (_saved.Width.keyword == StyleKeyword.Undefined && _saved.Width.value.unit == LengthUnit.Pixel) _natural.x = _saved.Width.value.value;
            if (_saved.Height.keyword == StyleKeyword.Undefined && _saved.Height.value.unit == LengthUnit.Pixel) _natural.y = _saved.Height.value.value;
        }
        else _natural = _element.layout.size;
        var position = PositionInRoot(Slot ?? _element);
        if (_saved == null)
        {
            _layoutAncestor = Parent;
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
                    if (!ancestor.Frame.Active || ancestor.Frame.Clip.InheritParent) continue;
                    clip = ancestor.Frame.Clip; clipBounds = ancestor.Frame.Bounds; break;
                }
            }
            var clipping = !clip.Shape.IsUnspecified;
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
            _element.style.translate = new Translate(dx, dy);
            _element.style.scale = new Scale(new Vector3(sx, sy, 1));
            var placeholder = frame.PlaceholderSize.CalculateSize(new Size(_natural.x, _natural.y), bounds.Size);
            Slot!.style.width = placeholder.Width; Slot.style.height = placeholder.Height;
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
        Slot.style.width = _natural.x; Slot.style.height = _natural.y;
        _element.RemoveFromHierarchy(); parent.Insert(index, Slot); (parent as ILayout)?.Track(Slot);
        _motion.Add(_element);
        _element.style.position = Position.Absolute;
        _element.style.left = _element.style.top = 0;
        _element.style.right = _element.style.bottom = StyleKeyword.Auto;
        _element.style.marginLeft = _element.style.marginRight = _element.style.marginTop = _element.style.marginBottom = 0;
        _element.style.minWidth = _element.style.minHeight = 0;
        _element.style.maxWidth = _element.style.maxHeight = StyleKeyword.None;
        _element.style.transformOrigin = new UnityEngine.UIElements.TransformOrigin(0, 0);
    }
    private void End(bool force = false)
    {
        if (Slot == null) return;
        // Native reparenting releases pointer capture. Finish the current gesture
        // before restoring the hierarchy; disposal still cancels input normally.
        _restorePending = !force && HasCapturedPointer();
        if (_restorePending) return;
        var slot = Slot; var parent = slot.parent; var index = parent?.IndexOf(slot) ?? 0;
        _element.RemoveFromHierarchy();
        (parent as ILayout)?.Untrack(slot); slot.RemoveFromHierarchy();
        _saved!.Apply(_element); _saved = null; Slot = null;
        if (parent != null) { parent.Insert(index, _element); (parent as ILayout)?.Track(_element); }
        Layer.RemoveFromHierarchy();
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true; _measure.Pause(); _element.UnregisterCallback<GeometryChangedEvent>(OnGeometry);
        _element.UnregisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
        End(force: true); if (_hidden) _element.style.visibility = _visibility;
        _node.SharedVisual = null; _root.Items.Remove(this);
    }
    private sealed class LayoutStyle
    {
        internal StyleLength Width, Height;
        private readonly StyleLength _minW, _minH, _maxW, _maxH, _left, _top, _right, _bottom, _ml, _mr, _mt, _mb, _basis;
        private readonly StyleEnum<Position> _position;
        private readonly StyleFloat _grow, _shrink;
        private readonly StyleEnum<Align> _align;
        private readonly StyleTranslate _translate;
        private readonly StyleScale _scale;
        private readonly StyleTransformOrigin _origin;
        internal LayoutStyle(VisualElement element)
        {
            var s = element.style; Width = s.width; Height = s.height;
            _minW = s.minWidth; _minH = s.minHeight; _maxW = s.maxWidth; _maxH = s.maxHeight;
            _left = s.left; _top = s.top; _right = s.right; _bottom = s.bottom; _position = s.position;
            _ml = s.marginLeft; _mr = s.marginRight; _mt = s.marginTop; _mb = s.marginBottom;
            _grow = s.flexGrow; _shrink = s.flexShrink; _basis = s.flexBasis; _align = s.alignSelf;
            _translate = s.translate; _scale = s.scale; _origin = s.transformOrigin;
        }
        internal void Apply(VisualElement element)
        {
            var s = element.style; s.width = Width; s.height = Height;
            s.minWidth = _minW; s.minHeight = _minH; s.maxWidth = _maxW; s.maxHeight = _maxH;
            s.left = _left; s.top = _top; s.right = _right; s.bottom = _bottom; s.position = _position;
            s.marginLeft = _ml; s.marginRight = _mr; s.marginTop = _mt; s.marginBottom = _mb;
            s.flexGrow = _grow; s.flexShrink = _shrink; s.flexBasis = _basis; s.alignSelf = _align;
            s.translate = _translate; s.scale = _scale; s.transformOrigin = _origin;
        }
    }
}
