using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Compose.Net.Kaspresso;
using KaspressoUi = Compose.Net.Kaspresso.Kaspresso;
using Compose.Net;
using Compose.Net.Unity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Box = Compose.Net.Unity.Box;
using Object = UnityEngine.Object;

// ReSharper disable CheckNamespace

public class AnimatedVisibilityLayoutTests
{
    [TestCase(false, false, false)]
    [TestCase(false, true, false)]
    [TestCase(true, false, false)]
    [TestCase(true, true, false)]
    [TestCase(false, false, true)]
    [TestCase(false, true, true)]
    public void EnterReachesNaturalSizeWithPaddedShell(bool measureBeforeEnter, bool horizontal, bool cancelBeforeLayout)
    {
        using var ui = new LayoutFixture();
        var enter = horizontal ? ui.Enter.ExpandHorizontally(Spec(), Alignment.Right, true, _ => 0)
            : ui.Enter.ExpandVertically(Spec(), Alignment.Bottom, true, _ => 0);
        if (cancelBeforeLayout)
        {
            ui.Step(enter, 0, false); ui.Clear(); ui.Size(152, 112);
            ui.CheckShell(e => e.style.overflow.keyword == StyleKeyword.Null, "cancelled enter releases clipping");
        }
        if (measureBeforeEnter) ui.Layout();
        foreach (var time in new[] { 0, 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000 })
        {
            ui.Step(enter, time); ui.Natural(120, 80);
            ui.Size(horizontal ? 32 + 120 * time / 1000f : 152, horizontal ? 112 : 32 + 80 * time / 1000f);
        }
        var exit = horizontal ? ui.Exit.ShrinkHorizontally(Spec(), Alignment.Right, true, _ => 0)
            : ui.Exit.ShrinkVertically(Spec(), Alignment.Bottom, true, _ => 0);
        foreach (var time in new[] { 0, 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000 })
        {
            ui.Step(exit, time); ui.Natural(120, 80);
            ui.Size(horizontal ? 152 - 120 * time / 1000f : 152, horizontal ? 112 : 112 - 80 * time / 1000f);
        }
        ui.Clear(); ui.Size(152, 112);
        ui.CheckContent(e => e.style.width.keyword == StyleKeyword.Null && e.style.height.keyword == StyleKeyword.Null,
            "restored automatic content size");
        ui.CheckShell(e => e.style.overflow.keyword == StyleKeyword.Null, "balanced clipping");
    }

    private static readonly Alignment[] Anchors =
    {
        Alignment.TopLeft, Alignment.TopCenter, Alignment.TopRight,
        Alignment.CenterLeft, Alignment.Center, Alignment.CenterRight,
        Alignment.BottomLeft, Alignment.BottomCenter, Alignment.BottomRight
    };

    [TestCase(0, 0, 0)] [TestCase(1, .5f, 0)] [TestCase(2, 1, 0)]
    [TestCase(3, 0, .5f)] [TestCase(4, .5f, .5f)] [TestCase(5, 1, .5f)]
    [TestCase(6, 0, 1)] [TestCase(7, .5f, 1)] [TestCase(8, 1, 1)]
    public void BothAxesKeepTheirAlignmentDuringEnterAndExit(int anchor, float xBias, float yBias)
    {
        using var ui = new LayoutFixture();
        var enter = ui.Enter.ExpandIn(Spec(), Anchors[anchor], true, _ => new Size(0, 0));
        var exit = ui.Exit.ShrinkOut(Spec(), Anchors[anchor], true, _ => new Size(0, 0));
        foreach (var elapsed in new[] { 0, 250, 500, 750 })
        {
            ui.Step(enter, elapsed);
            Check(elapsed / 1000f);
        }
        foreach (var elapsed in new[] { 0, 250, 500, 750, 1000 })
        {
            ui.Step(exit, elapsed);
            Check(1 - elapsed / 1000f);
        }
        void Check(float fraction)
        {
            ui.Size(120 * fraction + 32, 80 * fraction + 32);
            ui.CheckContent(e => Close(e.layout.x, 16 - 120 * (1 - fraction) * xBias), "horizontal anchor");
            ui.CheckContent(e => Close(e.layout.y, 16 - 80 * (1 - fraction) * yBias), "vertical anchor");
            ui.Natural(120, 80);
        }
    }

    [TestCase(0, 0)] [TestCase(0, 80)] [TestCase(120, 0)]
    [TestCase(.5f, .25f)] [TestCase(180.5f, 90.25f)] [TestCase(600, 400)]
    public void NaturalContentSizeSurvivesExpansion(float width, float height)
    {
        using var ui = new LayoutFixture(width, height);
        var enter = ui.Enter.ExpandIn(Spec(), Alignment.Center, true, _ => new Size(0, 0));
        foreach (var elapsed in new[] { 0, 500, 1000 })
        {
            ui.Step(enter, elapsed);
            ui.Natural(width, height);
            ui.Size(32 + width * elapsed / 1000f, 32 + height * elapsed / 1000f);
        }
    }

    [TestCase(0, 0, 0, 0, 0)] [TestCase(3, 9, 5, 11, 0)]
    [TestCase(0, 0, 0, 0, 2)] [TestCase(4, 4, 4, 4, 3)]
    [TestCase(1, 2, 3, 4, 1)] [TestCase(40, 8, 24, 12, 1)]
    public void AnimatedShellIncludesPaddingAndBorders(float left, float right, float top, float bottom, float border)
    {
        using var ui = new LayoutFixture();
        ui.Shell.style.paddingLeft = left; ui.Shell.style.paddingRight = right;
        ui.Shell.style.paddingTop = top; ui.Shell.style.paddingBottom = bottom;
        ui.Shell.style.borderLeftWidth = ui.Shell.style.borderRightWidth = border;
        ui.Shell.style.borderTopWidth = ui.Shell.style.borderBottomWidth = border;
        var enter = ui.Enter.ExpandIn(Spec(), Alignment.TopLeft, true, _ => new Size(0, 0));
        foreach (var elapsed in new[] { 0, 500, 1000 })
        {
            ui.Step(enter, elapsed);
            ui.Size(left + right + 2 * border + 120 * elapsed / 1000f,
                top + bottom + 2 * border + 80 * elapsed / 1000f);
        }
    }

    [TestCase(1000, 200, 100, 0)] [TestCase(1000, 200, 200, 0)]
    [TestCase(1000, 200, 700, .5f)] [TestCase(1000, 200, 1200, 1)]
    [TestCase(0, 0, 1, 1)] [TestCase(1000, 0, 2000, 1)]
    public void TimingIsAppliedOnTheFirstMeasuredFrame(int duration, int delay, int elapsed, float progress)
    {
        using var ui = new LayoutFixture();
        var enter = ui.Enter.ExpandVertically(Spec(duration, delay), Alignment.Bottom, true, height => height / 4);
        ui.Step(enter, elapsed);
        ui.Size(152, 32 + 20 + 60 * progress);
        ui.Natural(120, 80);
    }

    [TestCase(true)] [TestCase(false)]
    public void ClipFlagIsBalancedAcrossEnterAndExit(bool clip)
    {
        using var ui = new LayoutFixture();
        var enter = ui.Enter.ExpandVertically(Spec(), Alignment.Bottom, clip, _ => 0);
        var exit = ui.Exit.ShrinkVertically(Spec(), Alignment.Bottom, clip, _ => 0);
        ui.Step(enter, 500);
        ui.CheckShell(e => e.resolvedStyle.overflow == (clip ? Overflow.Hidden : Overflow.Visible), "clip flag");
        ui.Step(exit, 500);
        ui.CheckShell(e => e.resolvedStyle.overflow == (clip ? Overflow.Hidden : Overflow.Visible), "clip flag");
        ui.Clear();
        ui.CheckShell(e => e.style.overflow.keyword == StyleKeyword.Null, "restored clipping");
    }

    [TestCase("ParentConstraints")] [TestCase("ExplicitContentSize")]
    [TestCase("ContentOffset")] [TestCase("FlexShrink")]
    public void RevertPreservesExistingStyles(string setting)
    {
        using var ui = new LayoutFixture();
        switch (setting)
        {
            case "ParentConstraints": ui.Shell.style.maxWidth = 600; ui.Shell.style.maxHeight = 500; break;
            case "ExplicitContentSize": ui.Content.style.width = 120; ui.Content.style.height = 80; break;
            case "ContentOffset": ui.Content.style.left = 3; ui.Content.style.top = 5; break;
            case "FlexShrink": ui.Content.style.flexShrink = .25f; break;
        }
        var width = ui.Content.style.width; var height = ui.Content.style.height;
        var left = ui.Content.style.left; var top = ui.Content.style.top; var shrink = ui.Content.style.flexShrink;
        var maxWidth = ui.Shell.style.maxWidth; var maxHeight = ui.Shell.style.maxHeight;
        ui.Step(ui.Enter.ExpandIn(Spec(), Alignment.BottomRight, true, _ => new Size(0, 0)), 500);
        ui.Clear(); ui.Clear();
        ui.CheckContent(e => e.style.width.Equals(width), "restored width");
        ui.CheckContent(e => e.style.height.Equals(height), "restored height");
        ui.CheckContent(e => e.style.left.Equals(left), "restored left");
        ui.CheckContent(e => e.style.top.Equals(top), "restored top");
        ui.CheckContent(e => e.style.flexShrink.Equals(shrink), "restored flexShrink");
        ui.CheckShell(e => e.style.maxWidth.Equals(maxWidth), "restored maxWidth");
        ui.CheckShell(e => e.style.maxHeight.Equals(maxHeight), "restored maxHeight");
    }

    [TestCase("RepeatedCycles")] [TestCase("ReverseExit")] [TestCase("ResizeAfterEnter")]
    [TestCase("RetargetBeforeLayout")] [TestCase("CancelAfterMeasurement")]
    [TestCase("ReparentBeforeLayout")] [TestCase("IndependentHosts")]
    public void TransitionLifecycleKeepsLayoutConsistent(string scenario)
    {
        using var ui = new LayoutFixture();
        var enter = ui.Enter.ExpandVertically(Spec(), Alignment.Bottom, true, _ => 0);
        var exit = ui.Exit.ShrinkVertically(Spec(), Alignment.Bottom, true, _ => 0);
        switch (scenario)
        {
            case "RepeatedCycles":
                for (var cycle = 0; cycle < 3; cycle++)
                {
                    foreach (var time in new[] { 0, 500, 1000 }) { ui.Step(enter, time); ui.Size(152, 32 + 80 * time / 1000f); }
                    foreach (var time in new[] { 0, 500, 1000 }) { ui.Step(exit, time); ui.Size(152, 112 - 80 * time / 1000f); }
                    ui.Clear(); ui.Size(152, 112);
                }
                break;
            case "ReverseExit":
                ui.Step(enter, 1000); ui.Step(exit, 600); ui.Size(152, 64);
                ui.Step(enter, 400); ui.Size(152, 64); ui.Step(enter, 1000); ui.Size(152, 112);
                break;
            case "ResizeAfterEnter":
                ui.Step(enter, 1000); ui.Child.style.height = 160; ui.Layout(); ui.Size(152, 192);
                ui.Step(exit, 500); ui.Size(152, 112); ui.Natural(120, 160);
                break;
            case "RetargetBeforeLayout":
                ui.Step(enter, 0, false); ui.Step(enter, 500); ui.Size(152, 72);
                break;
            case "CancelAfterMeasurement":
                ui.Step(enter, 400); ui.Size(152, 64); ui.Clear(); ui.Size(152, 112);
                ui.Layout(); ui.Size(152, 112);
                break;
            case "ReparentBeforeLayout":
                ui.Step(enter, 0, false);
                var other = new Box { style = { flexShrink = 0 } };
                ui.Root.Add(other); new UnityReusableComposeNode(other).Reinsert(0, ui.ContentNode);
                ui.Layout(); ui.CheckRoot(_ => Close(other.layout.height, 0), "deferred transition uses the current parent");
                ui.Clear(); ui.CheckRoot(_ => Close(other.layout.height, 80), "reparented content restores its height");
                break;
            case "IndependentHosts":
                using (var otherUi = new LayoutFixture())
                {
                    ui.Step(enter, 750);
                    otherUi.Step(otherUi.Enter.ExpandVertically(Spec(), Alignment.Top, true, _ => 0), 250);
                    ui.Size(152, 92); otherUi.Size(152, 52);
                    ui.Clear(); otherUi.Size(152, 52);
                }
                break;
        }
    }

    [TestCase("EnterFirstFrame", 0)] [TestCase("EnterMidpoint", 500)]
    [TestCase("EnterCompleted", 1000)] [TestCase("ExitMidpoint", 500)]
    public void FadeAndSizeTransitionsComposeWithoutLosingProgress(string scenario, int elapsed)
    {
        using var ui = new LayoutFixture();
        var progress = elapsed / 1000f;
        if (scenario == "ExitMidpoint")
        {
            ui.Layout();
            ui.Step(ui.Exit.FadeOut(Spec(), 0) + ui.Exit.ShrinkVertically(Spec(), Alignment.Bottom, true, _ => 0), elapsed);
            ui.CheckContent(e => Math.Abs(e.resolvedStyle.opacity - (1 - progress)) < .001f, "exit alpha");
            ui.Size(152, 32 + 80 * (1 - progress));
        }
        else
        {
            ui.Step(ui.Enter.FadeIn(Spec(), 0) + ui.Enter.ExpandVertically(Spec(), Alignment.Bottom, true, _ => 0), elapsed);
            ui.CheckContent(e => Math.Abs(e.resolvedStyle.opacity - progress) < .001f, "enter alpha");
            ui.Size(152, 32 + 80 * progress);
        }
        ui.Clear(); ui.Size(152, 112);
        ui.CheckContent(e => Math.Abs(e.resolvedStyle.opacity - 1) < .001f, "restored alpha");
    }

    private static AnimationSpec Spec(int duration = 1000, int delay = 0) =>
        new(TimeSpan.FromMilliseconds(duration), TimeSpan.FromMilliseconds(delay), new LinearEasing());
    private static bool Close(float actual, float expected) => Math.Abs(actual - expected) <= .6f;

    // NUnit discovers cases; Kaspresso owns live selectors, actions, assertions,
    // named steps and hierarchy diagnostics against a real UI Toolkit panel.
    private sealed class VisibilityScreen : KScreen<VisibilityScreen>
    {
        public readonly KView RootView;
        public readonly KView ShellView;
        public readonly KView ContentView;
        public VisibilityScreen(KaspressoUi ui) : base(ui)
        {
            RootView = new KView(ui, b => b.WithTestTag("visibility-root"));
            ShellView = new KView(RootView, b => b.WithTestTag("visibility-shell"));
            ContentView = new KView(RootView, b => b.WithTestTag("visibility-content"));
        }
    }

    private sealed class ToolkitNode : IComposeTestNode
    {
        public readonly VisualElement Element;
        public ToolkitNode(VisualElement element) => Element = element;
        public string TestTag => Element.name;
        public string Text => (Element as TextElement)?.text;
        public bool IsEnabled => Element.enabledInHierarchy;
        public bool IsDisplayed
        {
            get
            {
                if (Element.panel == null || !(Element.layout.width > 0) || !(Element.layout.height > 0)) return false;
                for (var current = Element; current != null; current = current.parent)
                    if (!current.visible || current.resolvedStyle.display == DisplayStyle.None || current.resolvedStyle.opacity <= 0) return false;
                return true;
            }
        }
        public IEnumerable<IComposeTestNode> Children => Element.Children().Select(child => new ToolkitNode(child));
        public void Click() => throw new NotSupportedException("Layout scenarios use explicit custom actions through ActAsync.");
        public void ReplaceText(string text) => throw new NotSupportedException("Read-only layout fixture.");
    }

    private sealed class ToolkitDriver : IComposeTestDriver
    {
        private readonly VisualElement _root;
        private readonly Action _validate;
        public bool MeasureOnIdle;
        public ToolkitDriver(VisualElement root, Action validate) { _root = root; _validate = validate; }
        public IEnumerable<IComposeTestNode> Roots { get { yield return new ToolkitNode(_root); } }
        public Task WaitForIdleAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            // Explicitly hold layout for scenarios that cancel or retarget before
            // measurement. Ordinary actions/assertions settle actual Yoga layout.
            if (MeasureOnIdle) _validate();
            return Task.CompletedTask;
        }
    }

    private sealed class LayoutFixture : IDisposable
    {
        private readonly ScriptableObject _owner = ScriptableObject.CreateInstance<PanelOwner>();
        private readonly IPanel _panel;
        private readonly ToolkitDriver _driver;
        private readonly KaspressoUi _ui;
        private readonly VisibilityScreen _screen;
        private Action _revert;
        public readonly Box Shell = new() { name = "visibility-shell", style = { flexShrink = 0, paddingTop = 16, paddingBottom = 16, paddingLeft = 16, paddingRight = 16 } };
        public readonly Box Content = new() { name = "visibility-content" };
        public readonly VisualElement Child;
        public readonly UnityReusableComposeNode ContentNode;
        public readonly IEnterTransitionsFactory Enter;
        public readonly IExitTransitionsFactory Exit;
        public VisualElement Root => _panel.visualTree;

        public LayoutFixture(float width = 120, float height = 80)
        {
            var panelType = typeof(VisualElement).Assembly.GetType("UnityEngine.UIElements.Panel", true);
            _panel = (IPanel)panelType.GetMethod("CreateEditorPanel", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Invoke(null, new object[] { _owner });
            var validate = panelType.GetMethod("ValidateLayout", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Root.name = "visibility-root";
            Root.style.width = 1000; Root.style.height = 1000;
            Root.style.alignItems = Align.Center; Root.style.justifyContent = Justify.Center;
            // The control surface is measured before inserting transition content.
            // Kaspresso can act on it while the new content is still unmeasured.
            validate.Invoke(_panel, null);
            _driver = new ToolkitDriver(Root, () => validate.Invoke(_panel, null));
            _ui = new KaspressoUi(_driver, timeout: TimeSpan.Zero, log: NUnit.Framework.TestContext.WriteLine);
            _screen = new VisibilityScreen(_ui);
            Root.Add(Shell);
            ContentNode = new UnityReusableComposeNode(Content);
            new UnityReusableComposeNode(Shell).Insert(0, ContentNode);
            Child = new VisualElement { style = { width = width, height = height, flexShrink = 0 } };
            ContentNode.Insert(0, new UnityReusableComposeNode(Child));
            var assembly = typeof(UnityReusableComposeNode).Assembly;
            Enter = (IEnterTransitionsFactory)Activator.CreateInstance(assembly.GetType(
                "Compose.Net.Unity.Runtime.Adapters.Transitions.Enter.EnterTransitionsFactoryImpl", true), true);
            Exit = (IExitTransitionsFactory)Activator.CreateInstance(assembly.GetType(
                "Compose.Net.Unity.Runtime.Adapters.Transitions.Exit.ExitTransitionsFactoryImpl", true), true);
        }
        private void Act(string name, Action action, bool measure)
        {
            _driver.MeasureOnIdle = false;
            _ui.TestCase().RunAsync(test => test.StepAsync(name, () => _screen.RootView.ActAsync(_ =>
            {
                action(); _driver.MeasureOnIdle = measure;
            }))).GetAwaiter().GetResult();
        }
        public void Layout() => Act("Settle UI Toolkit layout", () => { }, true);
        public void Step(IEnterTransition transition, int elapsed, bool layout = true) => Act($"Enter at {elapsed} ms", () =>
        {
            Revert(); var time = TimeSpan.FromMilliseconds(elapsed);
            transition.Apply(ContentNode, time); _revert = () => transition.Revert(ContentNode, time);
        }, layout);
        public void Step(IExitTransition transition, int elapsed, bool layout = true) => Act($"Exit at {elapsed} ms", () =>
        {
            Revert(); var time = TimeSpan.FromMilliseconds(elapsed);
            transition.Apply(ContentNode, time); _revert = () => transition.Revert(ContentNode, time);
        }, layout);
        private void Revert() { var revert = _revert; _revert = null; revert?.Invoke(); }
        public void Clear() => Act("Cancel transition and restore layout", Revert, true);
        public void Size(float width, float height) => CheckShell(e => Close(e.layout.width, width) && Close(e.layout.height, height), $"size {width} x {height}");
        public void Natural(float width, float height) => CheckContent(e => Close(e.layout.width, width) && Close(e.layout.height, height), $"natural size {width} x {height}");
        public void CheckContent(Func<VisualElement, bool> predicate, string expectation) => Check(_screen.ContentView, predicate, expectation);
        public void CheckShell(Func<VisualElement, bool> predicate, string expectation) => Check(_screen.ShellView, predicate, expectation);
        public void CheckRoot(Func<VisualElement, bool> predicate, string expectation) => Check(_screen.RootView, predicate, expectation);
        private void Check(KView view, Func<VisualElement, bool> predicate, string expectation) =>
            _ui.TestCase().RunAsync(test => test.StepAsync(expectation,
                () => view.AssertAsync(node => predicate(((ToolkitNode)node).Element), expectation))).GetAwaiter().GetResult();
        public void Dispose() { Revert(); ((IDisposable)_panel).Dispose(); Object.DestroyImmediate(_owner); }
    }

    private sealed class PanelOwner : ScriptableObject { }
    private sealed class LinearEasing : IEasing { public float Transform(float fraction) => fraction; }
}
