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
        
        style.marginBottom = 2;
        style.marginLeft = 2;
        style.marginRight = 4;
        style.marginTop = 4;

        style.paddingBottom = 4;
        style.paddingLeft = 1;
        style.paddingRight = 2;
        style.paddingTop = 4;
    }
}