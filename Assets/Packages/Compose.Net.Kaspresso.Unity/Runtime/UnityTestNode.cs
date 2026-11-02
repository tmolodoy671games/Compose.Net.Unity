using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Kaspresso.Unity
{
    [AiGenerated]
    internal sealed class UnityTestNode : ITestNode
    {
        internal UnityTestDriver Owner { get; }
        internal VisualElement Element { get; }
        internal UnityTestNode(UnityTestDriver owner, VisualElement element)
        {
            Owner = owner;
            Element = element;
        }
        public string TestTag => Element.name;
        public string Text => Element is TextField field ? field.text : (Element as TextElement)?.text;
        public bool IsEnabled => Element.enabledInHierarchy;
        public IReadOnlyList<ITestNode> Children
        {
            get
            {
                var result = new List<ITestNode>(Element.hierarchy.childCount);
                for (var i = 0; i < Element.hierarchy.childCount; i++)
                    result.Add(new UnityTestNode(Owner, Element.hierarchy[i]));
                return result;
            }
        }
        public bool IsDisplayed
        {
            get
            {
                if (Element.panel == null || Element.resolvedStyle.visibility != Visibility.Visible) return false;
                var bounds = Element.worldBound;
                if (!HasArea(bounds)) return false;
                for (var current = Element; current != null; current = current.parent)
                {
                    if (current.resolvedStyle.display == DisplayStyle.None || current.resolvedStyle.opacity <= 0)
                        return false;
                    if (current != Element && current.resolvedStyle.overflow == Overflow.Hidden)
                    {
                        var clip = current.worldBound;
                        bounds = Rect.MinMaxRect(Mathf.Max(bounds.xMin, clip.xMin), Mathf.Max(bounds.yMin, clip.yMin),
                            Mathf.Min(bounds.xMax, clip.xMax), Mathf.Min(bounds.yMax, clip.yMax));
                        if (!HasArea(bounds)) return false;
                    }
                }
                return bounds.Overlaps(Element.panel.visualTree.worldBound);
            }
        }
        private static bool HasArea(Rect rect) => rect.width > 0 && rect.height > 0 &&
            !float.IsNaN(rect.x) && !float.IsNaN(rect.y) && !float.IsInfinity(rect.x) && !float.IsInfinity(rect.y);
    }
}
