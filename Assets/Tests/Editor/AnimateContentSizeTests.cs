using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Compose.Net;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;
using Object = UnityEngine.Object;

namespace Compose.Net.Unity.Tests;

// Real UI Toolkit layout and animation ticks, without a window or a rendered scene.
public class AnimateContentSizeTests
{
    private Fixture _ui;

    [TearDown]
    public void TearDown() => _ui?.Dispose();

    [Test] public void FirstSingleChildSnaps() { Create(); Add(80, 30); First(80, 30); }
    [Test] public void FirstEmptyContentSnapsToZero() { Create(); First(0, 0); }
    [Test] public void FirstColumnWithTwoChildrenMeasuresBoth() { Create(); Add(80, 30); Add(50, 40); First(80, 70); }
    [Test] public void FirstColumnWithThreeChildrenMeasuresAll() { Create(); Add(80, 30); Add(100, 40); Add(50, 20); First(100, 90); }
    [Test] public void FirstRowWithTwoChildrenMeasuresBoth() { Create(new Row()); Add(80, 30); Add(50, 40); First(130, 40); }
    [Test] public void FirstRowWithThreeChildrenMeasuresAll() { Create(new Row()); Add(80, 30); Add(50, 40); Add(20, 10); First(150, 40); }
    [Test] public void FirstBoxWithTwoChildrenUsesMaximumExtents() { Create(new Box()); Add(80, 30); Add(50, 60); First(80, 60); }
    [Test] public void FirstBoxWithThreeChildrenUsesMaximumExtents() { Create(new Box()); Add(80, 30); Add(100, 20); Add(50, 60); First(100, 60); }
    [Test] public void FirstNestedColumnIncludesGrandchildren() { Create(); var group = Group(FlexDirection.Column, Child(40, 20), Child(60, 30)); Add(group); Add(30, 10); First(60, 60); }
    [Test] public void FirstNestedRowIncludesGrandchildren() { Create(); Add(Group(FlexDirection.Row, Child(40, 20), Child(60, 30))); Add(30, 10); First(100, 40); }
    [Test] public void FirstHiddenChildDoesNotContribute() { Create(); Add(80, 30); Add(200, 90).style.display = DisplayStyle.None; First(80, 30); }
    [Test] public void FirstAbsoluteChildDoesNotContribute() { Create(); Add(80, 30); Add(200, 90).style.position = Position.Absolute; First(80, 30); }
    [Test] public void FirstZeroSizedChildDoesNotHideSiblings() { Create(); Add(0, 0); Add(80, 30); First(80, 30); }
    [Test] public void FirstFractionalSizeIsPreserved() { Create(); Add(80.5f, 30.25f); First(80.5f, 30.25f); }
    [Test] public void FirstSizeIncludesPadding() { Create(); Padding(4, 4, 4, 4); Add(80, 30); First(88, 38); }
    [Test] public void FirstSizeIncludesAsymmetricPadding() { Create(); Padding(2, 7, 3, 11); Add(80, 30); First(89, 44); }
    [Test] public void FirstSizeIncludesHostBorders() { Create(); Borders(_ui.Host, 2); Add(80, 30); First(84, 34); }
    [Test] public void FirstSizeCountsContentBordersOnce() { Create(); Add(80, 30); _ui.Attach(); Borders(_ui.Content, 2); First(84, 34); }
    [Test] public void FirstSizeIncludesContentMargins() { Create(); Add(80, 30); _ui.Attach(); _ui.Content.style.marginLeft = 3; _ui.Content.style.marginBottom = 7; First(83, 37); }
    [Test] public void FirstSizeIncludesEachChildMargin() { Create(); Add(80, 30).style.marginBottom = 5; Add(50, 40).style.marginTop = 7; First(80, 82); }
    [Test] public void FirstCenteredSizeDoesNotJumpOnLaterLayoutPasses()
    {
        Create(new Box()); _ui.Root.style.alignItems = Align.Center; _ui.Root.style.justifyContent = Justify.Center;
        _ui.Host.style.alignSelf = Align.Auto; Add(80, 30); Add(40, 60); First(80, 60);
        var first = _ui.Host.layout; _ui.Layout(); _ui.Layout();
        Assert.That(_ui.Host.layout, Is.EqualTo(first)); Assert.That(_ui.Animation, Is.Null);
    }
    [Test] public void WidthGrowthInterpolates() { var child = Initial(80, 30); child.style.width = 160; AnimateTo(160, 30, 120, 30); }
    [Test] public void WidthShrinkInterpolates() { var child = Initial(160, 30); child.style.width = 80; AnimateTo(80, 30, 120, 30); }
    [Test] public void HeightGrowthInterpolates() { var child = Initial(80, 30); child.style.height = 90; AnimateTo(80, 90, 80, 60); }
    [Test] public void HeightShrinkInterpolates() { var child = Initial(80, 90); child.style.height = 30; AnimateTo(80, 30, 80, 60); }
    [Test] public void BothDimensionsInterpolate() { var child = Initial(80, 30); child.style.width = 160; child.style.height = 90; AnimateTo(160, 90, 120, 60); }
    [Test] public void ResizingSecondColumnChildAnimatesWholeContent() { Create(); Add(80, 30); var second = Add(50, 40); First(80, 70); second.style.height = 80; AnimateTo(80, 110, 80, 90); }
    [Test] public void ResizingSecondRowChildAnimatesWholeContent() { Create(new Row()); Add(80, 30); var second = Add(50, 40); First(130, 40); second.style.width = 90; AnimateTo(170, 40, 150, 40); }
    [Test] public void ResizingSecondBoxChildAnimatesMaximumExtent() { Create(new Box()); Add(80, 30); var second = Add(50, 60); First(80, 60); second.style.width = 160; AnimateTo(160, 60, 120, 60); }
    [Test] public void AddingSecondChildAnimates() { Initial(80, 30); Add(50, 40); AnimateTo(80, 70, 80, 50); }
    [Test] public void AddingThirdChildAnimates() { Create(); Add(80, 30); Add(50, 40); First(80, 70); Add(100, 20); AnimateTo(100, 90, 90, 80); }
    [Test] public void RemovingTallestBoxChildAnimates() { Create(new Box()); Add(80, 30); var second = Add(50, 90); First(80, 90); _ui.Remove(second); AnimateTo(80, 30, 80, 60); }
    [Test] public void RemovingAllChildrenAnimatesToEmptySize() { var child = Initial(80, 30); _ui.Remove(child); AnimateTo(0, 0, 40, 15); }
    [Test] public void HidingSecondChildAnimates() { Create(); Add(80, 30); var second = Add(50, 40); First(80, 70); second.style.display = DisplayStyle.None; AnimateTo(80, 30, 80, 50); }
    [Test] public void ShowingSecondChildAnimates() { Create(); Add(80, 30); var second = Add(50, 40); second.style.display = DisplayStyle.None; First(80, 30); second.style.display = DisplayStyle.Flex; AnimateTo(80, 70, 80, 50); }
    [Test] public void ReorderingChildrenWithEqualBoundsDoesNotAnimate() { Create(); Add(80, 30); var second = Add(50, 40); First(80, 70); _ui.Reorder(second, 0); _ui.Layout(); _ui.Size(80, 70); Assert.That(_ui.Animation, Is.Null); }
    [Test] public void UnchangedGeometryDoesNotRestartAnimation()
    {
        var child = Initial(80, 30); child.style.width = 160; _ui.Layout(); var animation = _ui.Animation;
        var stopped = 0; animation.onAnimationCompleted = () => stopped++;
        using (var geometry = GeometryChangedEvent.GetPooled(_ui.Content.layout, _ui.Content.layout)) _ui.Content.SendEvent(geometry);
        Assert.That(stopped, Is.Zero); Assert.That(_ui.Animation, Is.SameAs(animation));
    }
    [Test] public void RetargetingStartsAtCurrentDisplayedSize()
    {
        var child = Initial(80, 30); child.style.width = 160; _ui.Layout(); _ui.Tick(500); _ui.Size(120, 30);
        child.style.width = 200; _ui.Layout(); _ui.Size(120, 30); _ui.Tick(500); _ui.Size(160, 30); _ui.Finish(); _ui.Size(200, 30);
    }
    [Test] public void RetargetingStopsPreviousAnimation() { var child = Initial(80, 30); child.style.width = 160; _ui.Layout(); var stopped = 0; _ui.Animation.onAnimationCompleted = () => stopped++; child.style.width = 200; _ui.Layout(); Assert.That(stopped, Is.EqualTo(1)); }
    [Test] public void ZeroDurationSnapsWithoutNaN() { Create(spec: Spec(0)); var child = Add(80, 30); First(80, 30); child.style.width = 160; _ui.Layout(); _ui.Size(160, 30); Assert.That(_ui.Animation == null || !_ui.Animation.isRunning, Is.True); }
    [Test] public void DelayHoldsSizeBeforeInterpolation() { Create(spec: Spec(1000, 200)); var child = Add(80, 30); First(80, 30); child.style.width = 160; _ui.Layout(); _ui.Tick(100); _ui.Size(80, 30); _ui.Tick(700); _ui.Size(120, 30); _ui.Finish(); _ui.Size(160, 30); }
    [Test] public void PaddingChangeAnimatesWithoutChildResize() { Initial(80, 30); Padding(10, 10, 10, 10); AnimateTo(100, 50, 90, 40); }
    [Test] public void BorderChangeAnimatesWithoutChildResize() { Initial(80, 30); Borders(_ui.Host, 4); AnimateTo(88, 38, 84, 34); }
    [Test] public void UpdatingSpecPreservesInitialSizeWithoutFlash() { Initial(80, 30); _ui.Update(Spec(2000)); _ui.Layout(); _ui.Size(80, 30); Assert.That(_ui.Animation, Is.Null); }
    [Test] public void UpdatingSpecRetimesRunningAnimation()
    {
        var child = Initial(80, 30); child.style.width = 160; _ui.Layout(); _ui.Tick(500); _ui.Update(Spec(2000));
        _ui.Layout(); _ui.Size(120, 30); Assert.That(_ui.Animation.durationMs, Is.EqualTo(2000)); _ui.Tick(1000); _ui.Size(140, 30); _ui.Finish(); _ui.Size(160, 30);
    }
    [Test] public void DetachingStopsRunningAnimation() { var child = Initial(80, 30); child.style.width = 160; _ui.Layout(); var animation = _ui.Animation; _ui.Detach(); Assert.That(animation.isRunning, Is.False); }
    [Test] public void DetachingRestoresOriginalHostDimensions() { Create(); _ui.Host.style.width = 250; _ui.Host.style.height = 150; Add(80, 30); _ui.Attach(); _ui.Layout(); _ui.Detach(); Assert.That(_ui.Host.style.width.value.value, Is.EqualTo(250)); Assert.That(_ui.Host.style.height.value.value, Is.EqualTo(150)); }
    [Test] public void DetachingRestoresSharedContentPositionAndShrink()
    {
        Create(); Add(80, 30); var shared = _ui.UnityNode.AcquireContentContainer(); shared.style.position = Position.Relative; shared.style.flexShrink = 0.5f;
        First(80, 30); _ui.Detach(); Assert.That(shared.style.position.value, Is.EqualTo(Position.Relative)); Assert.That(shared.style.flexShrink.value, Is.EqualTo(0.5f)); _ui.UnityNode.ReleaseContentContainer();
    }
    [Test] public void ReattachingResetsFirstMeasurementForNewContent() { var child = Initial(80, 30); _ui.Detach(); child.style.width = 160; _ui.Attach(); _ui.Layout(); _ui.Size(160, 30); Assert.That(_ui.Animation, Is.Null); }
    [Test] public void IndependentHostsDoNotShareAnimationState()
    {
        var child = Initial(80, 30); using var other = new Fixture(new Column(), Spec()); other.Add(Child(60, 20)); other.Attach(); other.Layout();
        child.style.width = 160; _ui.Layout(); _ui.Tick(500); other.Size(60, 20); Assert.That(other.Animation, Is.Null); _ui.Size(120, 30);
    }

    private void Create(VisualElement host = null, AnimationSpec? spec = null) => _ui = new Fixture(host ?? new Column(), spec ?? Spec());
    private VisualElement Add(float width, float height) => Add(Child(width, height));
    private VisualElement Add(VisualElement child) { _ui.Add(child); return child; }
    private VisualElement Initial(float width, float height) { Create(); var child = Add(width, height); First(width, height); return child; }
    private void First(float width, float height) { _ui.Attach(); _ui.Layout(); _ui.Size(width, height); Assert.That(_ui.Animation, Is.Null, "Initial layout must not animate."); }
    private void AnimateTo(float width, float height, float middleWidth, float middleHeight)
    {
        _ui.Layout(); Assert.That(_ui.Animation, Is.Not.Null, "A later content change must animate.");
        _ui.Tick(500); _ui.Size(middleWidth, middleHeight); _ui.Finish(); _ui.Size(width, height);
    }
    private void Padding(float left, float right, float top, float bottom) { var s = _ui.Host.style; s.paddingLeft = left; s.paddingRight = right; s.paddingTop = top; s.paddingBottom = bottom; }
    private static void Borders(VisualElement element, float size) { var s = element.style; s.borderLeftWidth = size; s.borderRightWidth = size; s.borderTopWidth = size; s.borderBottomWidth = size; }
    private static VisualElement Child(float width, float height) => new() { style = { width = width, height = height, flexShrink = 0 } };
    private static VisualElement Group(FlexDirection direction, params VisualElement[] children)
    {
        var group = new VisualElement { style = { flexDirection = direction, alignItems = Align.FlexStart, flexShrink = 0 } };
        foreach (var child in children) group.Add(child); return group;
    }
    private static AnimationSpec Spec(int duration = 1000, int delay = 0) => new(TimeSpan.FromMilliseconds(duration), TimeSpan.FromMilliseconds(delay), new LinearEasing());
    private sealed class LinearEasing : IEasing { public float Transform(float fraction) => fraction; }

    private sealed class PanelOwner : ScriptableObject { }

    private sealed class Fixture : IDisposable
    {
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private readonly ScriptableObject _owner = ScriptableObject.CreateInstance<PanelOwner>();
        private readonly IPanel _panel;
        private readonly MethodInfo _validate;
        private readonly IModifier.Node _node;
        private readonly Type _modifierType = typeof(UnityReusableComposeNode).Assembly.GetType("Compose.Net.Unity.AnimateContentSizeModifierImpl", true);
        private bool _attached;
        private bool _initialFrame;
        private readonly Dictionary<VisualElement, UnityReusableComposeNode> _children = new();
        public readonly VisualElement Host;
        public readonly UnityReusableComposeNode UnityNode;
        public VisualElement Root => _panel.visualTree;
        public VisualElement Content { get; private set; }

        public Fixture(VisualElement host, AnimationSpec spec)
        {
            var panelType = typeof(VisualElement).Assembly.GetType("UnityEngine.UIElements.Panel", true);
            _panel = (IPanel)panelType.GetMethod("CreateEditorPanel", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Invoke(null, new object[] { _owner });
            _validate = panelType.GetMethod("ValidateLayout", Flags);
            Root.style.width = 1000; Root.style.height = 1000; Root.style.alignItems = Align.FlexStart;
            Host = host; Host.style.flexDirection = host is Row ? FlexDirection.Row : FlexDirection.Column;
            Host.style.alignItems = Align.FlexStart; Host.style.alignSelf = Align.FlexStart; Host.style.flexShrink = 0;
            Root.Add(Host); UnityNode = new UnityReusableComposeNode(Host);
            _node = Modifier(spec).Create();
        }

        private IModifierNodeElement Modifier(AnimationSpec spec) => (IModifierNodeElement)Activator.CreateInstance(_modifierType, spec);
        public void Update(AnimationSpec spec) => Modifier(spec).Update(_node);
        public void Add(VisualElement child) { var node = new UnityReusableComposeNode(child); _children.Add(child, node); UnityNode.Insert(HostChildrenCount(), node); }
        private int HostChildrenCount() => (_attached ? Content : Host).Children().Count(x => x.GetType().Name != "BoxPull");
        public void Remove(VisualElement child) { UnityNode.Remove(_children[child]); _children.Remove(child); }
        public void Reorder(VisualElement child, int index) => UnityNode.Reinsert(index, _children[child]);
        public void Attach()
        {
            if (_attached) return;
            typeof(IModifier.Node).GetMethod("Attach", Flags).Invoke(_node, new object[] { UnityNode });
            Content = Host.Children().First(x => x.GetType().Name != "BoxPull"); _attached = true; _initialFrame = true;
        }
        public void Detach()
        {
            if (!_attached) return;
            typeof(IModifier.Node).GetMethod("Detach", Flags).Invoke(_node, null); _attached = false;
        }
        public void Layout()
        {
            _validate.Invoke(_panel, null);
            // Finish the first layout's scheduled initialization, as a panel frame does.
            if (_initialFrame)
            {
                _panel.GetType().GetMethod("TickSchedulingUpdaters", Flags).Invoke(_panel, null);
                _initialFrame = false;
            }
            _validate.Invoke(_panel, null);
        }
        public void Size(float width, float height)
        {
            Assert.That(Host.layout.width, Is.EqualTo(width).Within(0.6f), "host width");
            Assert.That(Host.layout.height, Is.EqualTo(height).Within(0.6f), "host height");
        }
        public ValueAnimation<float> Animation => (ValueAnimation<float>)_node.GetType().GetField("_animation", Flags).GetValue(_node);
        public void Tick(int elapsed)
        {
            var animation = Animation; Assert.That(animation, Is.Not.Null);
            var start = (long)animation.GetType().GetField("m_StartTimeMs", Flags).GetValue(animation);
            var tick = animation.GetType().GetMethod("UnityEngine.UIElements.Experimental.IValueAnimationUpdate.Tick", Flags);
            tick.Invoke(animation, new object[] { start + elapsed }); Layout();
        }
        public void Finish() => Tick(Animation.durationMs + 1);
        public void Dispose() { Detach(); _node.Dispose(); UnityNode.Dispose(); ((IDisposable)_panel).Dispose(); Object.DestroyImmediate(_owner); }
    }
}
