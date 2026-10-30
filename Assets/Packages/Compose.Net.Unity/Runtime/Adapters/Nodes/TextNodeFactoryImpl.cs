using Compose.Net;
using Compose.Net.Unity.Runtime.Adapters.Extensions;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Runtime.Adapters.Nodes;

internal class TextNodeFactoryImpl : ITextNodeFactory
{
    public IReusableComposeNode CreateNode()
    {
        return new UnityReusableComposeNode(new Text());
    }

    public void Apply(
        IReusableComposeNode node,
        string text,
        bool softWrap,
        FontStyle fontStyle,
        FontWeight fontWeight,
        Sp fontSize,
        Color color,
        TextAlign textAlign,
        Compose.Net.TextOverflow overflow = Compose.Net.TextOverflow.Clip,
        int maxLines = int.MaxValue,
        int minLines = 1,
        Sp letterSpacing = default,
        System.Action<TextLayoutResult>? onTextLayout = null
    )
    {
        var it = node.VisualElement<Text>();
        it.text = text;
        it.style.whiteSpace = softWrap ? WhiteSpace.Normal : WhiteSpace.NoWrap;
        it.style.unityFontStyleAndWeight =
            FontStyleUtils.ToUnityFontStyle(fontStyle, fontWeight);
        it.style.unityTextAlign = textAlign.ToTextAnchor();
        it.style.fontSize = fontSize.Value;
        it.style.color = color.ToUnityColor();
        it.style.letterSpacing = letterSpacing.IsUnspecified ? 0 : letterSpacing.Value;
        it.ConfigureLayout(overflow, maxLines, minLines, onTextLayout);
    }
}
