// ReSharper disable CheckNamespace

using UnityEngine.UIElements;

namespace Compose.Net.Unity;

public class Column : VisualElement
{
}

public class Row : VisualElement
{
}

public interface ILayout
{
    void Track(VisualElement child);
    void Untrack(VisualElement child);
}

public class Spacer : VisualElement
{
}

// Content container of a node (scroll, animateContentSize).
public class Content : VisualElement
{
}

public class Text : Label
{
    public Text()
    {
        ClearClassList();
        
        style.flexBasis = StyleKeyword.Auto;
        style.flexGrow = 0;
        style.flexShrink = 0;
        style.fontSize = 14;
        
        // Text has no implicit box spacing in Compose. Explicit modifiers and
        // containing components own padding; Label defaults must not shift its center.
        style.marginLeft = style.marginRight = style.marginTop = style.marginBottom = 0;
        style.paddingLeft = style.paddingRight = style.paddingTop = style.paddingBottom = 0;
    }
}