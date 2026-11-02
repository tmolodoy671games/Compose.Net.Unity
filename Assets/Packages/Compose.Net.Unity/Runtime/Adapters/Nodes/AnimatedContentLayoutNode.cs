using System.Linq;
using Compose.Net;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Runtime.Adapters.Nodes;

internal sealed class AnimatedContentLayoutNode : UnityReusableComposeNode, IAnimatedContentLayoutNode
{
    internal AnimatedContentLayoutNode(AnimatedContent element) : base(element) { }
    public void UpdateAnimatedContent(AnimatedContentLayout layout) => ((AnimatedContent)VisualElement).Configure(layout);
}

internal sealed class AnimatedContentItemNode : UnityReusableComposeNode, IAnimatedContentItemNode
{
    internal AnimatedContentItemNode(AnimatedContentItem element) : base(element) { }
    public void UpdateAnimatedContentItem(int id, bool isTarget, bool hidden) => ((AnimatedContentItem)VisualElement).Configure(id, isTarget, hidden);
}

internal class AnimatedContent : Box
{
    private AnimatedContentLayout? _layout;
    private bool _initialized, _fixedWidth, _fixedHeight, _clip;
    internal AnimatedContent()
    {
        RegisterCallback<GeometryChangedEvent>(_ => ArrangeContent());
    }
    internal void Configure(AnimatedContentLayout layout)
    {
        if (!_initialized)
        {
            _initialized = true;
            _fixedWidth = style.width.keyword == StyleKeyword.Undefined;
            _fixedHeight = style.height.keyword == StyleKeyword.Undefined;
        }
        _layout = layout;
        tooltip = layout.Label;
        if (_clip != layout.Clip)
        {
            _clip = layout.Clip;
            if (_clip) this.Clip().Increment(); else this.Clip().Decrement();
        }
        ArrangeContent();
        schedule.Execute(MeasureContent);
    }
    internal void MeasureContent()
    {
        if (_layout == null) return;
        var target = Children().OfType<AnimatedContentItem>().FirstOrDefault(item => item.Id == _layout.TargetId && !item.Hidden);
        if (target == null || !float.IsFinite(target.layout.width) || !float.IsFinite(target.layout.height)) return;
        var items = Children().OfType<AnimatedContentItem>().Where(item => !item.Hidden && float.IsFinite(item.layout.width)).ToArray();
        var measured = new Size(items.Max(item => item.FullSize.Width), items.Max(item => item.FullSize.Height));
        var targetSize = new Size(_fixedWidth ? contentRect.width : target.FullSize.Width,
            _fixedHeight ? contentRect.height : target.FullSize.Height);
        measured = new Size(_fixedWidth ? contentRect.width : measured.Width, _fixedHeight ? contentRect.height : measured.Height);
        _layout.MeasureTarget(targetSize, measured);
        ArrangeContent();
    }
    private void ArrangeContent()
    {
        if (_layout == null || _layout.CurrentSize.IsUnspecified) return;
        var box = resolvedStyle;
        if (!_fixedWidth) style.width = _layout.CurrentSize.Width + box.paddingLeft + box.paddingRight + box.borderLeftWidth + box.borderRightWidth;
        if (!_fixedHeight) style.height = _layout.CurrentSize.Height + box.paddingTop + box.paddingBottom + box.borderTopWidth + box.borderBottomWidth;
        var width = _fixedWidth ? contentRect.width : _layout.CurrentSize.Width;
        var height = _fixedHeight ? contentRect.height : _layout.CurrentSize.Height;
        foreach (var item in Children().OfType<AnimatedContentItem>())
        {
            item.style.position = Position.Absolute;
            item.style.left = box.paddingLeft + (width - item.layout.width) * AnimatedContentLayout.Bias(_layout.ContentAlignment, true);
            item.style.top = box.paddingTop + (height - item.layout.height) * AnimatedContentLayout.Bias(_layout.ContentAlignment, false);
        }
    }
}

internal class AnimatedContentItem : Box
{
    internal Size FullSize
    {
        get
        {
            var content = Children().OfType<Box>().FirstOrDefault();
            if (content == null) return new Size(0, 0);
            // Expand/shrink constrain the transition shell. Measure its content before that
            // constraint, otherwise the animated clip becomes the next size target.
            var children = content.Children().Where(child => child is not BoxPull).ToArray();
            if (children.Length == 0) return new Size(0, 0);
            return new Size(
                children.Max(child => child.layout.width + child.resolvedStyle.marginLeft + child.resolvedStyle.marginRight),
                children.Max(child => child.layout.height + child.resolvedStyle.marginTop + child.resolvedStyle.marginBottom));
        }
    }
    internal int Id;
    internal bool IsTarget, Hidden;
    internal AnimatedContentItem()
    {
        style.flexShrink = 0;
        RegisterCallback<GeometryChangedEvent>(_ => (parent as AnimatedContent)?.MeasureContent());
    }
    internal void Configure(int id, bool target, bool hidden)
    {
        var changed = IsTarget != target || Hidden != hidden;
        Id = id; IsTarget = target; Hidden = hidden;
        if (changed) (parent as AnimatedContent)?.MeasureContent();
    }
}
