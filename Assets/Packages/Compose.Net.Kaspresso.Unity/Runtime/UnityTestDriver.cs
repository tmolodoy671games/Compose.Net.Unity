using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Compose.Net.Kaspresso.Unity
{
    /// <summary>Uses a fixture-owned simulator; the fixture retains panel lifetime ownership.</summary>
    [AiGenerated]
    public sealed class UnityTestDriver : ITestDriver
    {
        private readonly PanelSimulator _simulator;
        private readonly Func<VisualElement> _root;

        public UnityTestDriver(PanelSimulator simulator, Func<VisualElement> root = null)
        {
            _simulator = simulator ?? throw new ArgumentNullException(nameof(simulator));
            _root = root ?? (() => simulator.rootVisualElement);
        }

        public IReadOnlyList<ITestNode> Roots
        {
            get
            {
                var root = _root();
                if (root == null || root.panel == null) return Array.Empty<ITestNode>();
                if (root.panel != _simulator.panel)
                    throw new InvalidOperationException("The search root belongs to a different panel than the simulator.");
                return new ITestNode[] { new UnityTestNode(this, root) };
            }
        }

        public void Refresh() => _simulator.FrameUpdate();
        public void Click(ITestNode node)
        {
            _simulator.Click(Element(node));
        }
        public void ReplaceText(ITestNode node, string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            if (!(Element(node) is TextField field))
                throw new NotSupportedException("ReplaceText requires a TextField.");
            if (field.isReadOnly)
                throw new KaspressoAssertionException($"TextField '{field.name}' is read-only.");
            field.Focus();
            _simulator.FrameUpdate();
            field.SelectAll();
            _simulator.KeyPress(KeyCode.Backspace);
            if (text.Length > 0) _simulator.TypingText(text);
        }
        public void ScrollTo(ITestNode node)
        {
            var element = Element(node);
            var scrolled = false;
            for (var parent = element.parent; parent != null; parent = parent.parent)
            {
                if (!(parent is ScrollView scroll)) continue;
                scroll.ScrollTo(element);
                _simulator.FrameUpdate();
                scrolled = true;
            }
            if (!scrolled)
                throw new NotSupportedException($"Element '{element.name}' is not inside a ScrollView.");
        }
        private VisualElement Element(ITestNode node)
        {
            if (!(node is UnityTestNode wrapper) || !ReferenceEquals(wrapper.Owner, this))
                throw new ArgumentException("The node belongs to a different driver.", nameof(node));
            var element = wrapper.Element;
            var root = _root();
            if (element.panel == null || element.panel != _simulator.panel ||
                root == null || (element != root && !root.Contains(element)))
                throw new KaspressoAssertionException("The selected element has been detached from the test tree.");
            return element;
        }
    }
}
