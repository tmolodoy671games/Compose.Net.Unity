using UnityEngine.UIElements;

namespace Compose.Net.Unity;

public partial class UnityReusableComposeNode
{
    // Node factories own Row/Column styles. Forward their updates to the native
    // scrolling content as well, including updates that do not resize the viewport.
    internal void SyncScrollContentLayout()
    {
        if (_contentContainer?.GetFirstAncestorOfType<ScrollView>() == null) return;
        _contentContainer.style.flexDirection = VisualElement.style.flexDirection;
        _contentContainer.style.alignItems = VisualElement.style.alignItems;
        _contentContainer.style.justifyContent = VisualElement.style.justifyContent;
    }
}
