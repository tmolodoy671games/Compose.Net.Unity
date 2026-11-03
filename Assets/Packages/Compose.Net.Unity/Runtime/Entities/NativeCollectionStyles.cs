using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal static class NativeCollectionStyles
{
    internal static void ClearRowBackground(VisualElement content)
    {
        for (var element = content; element != null && element is not BaseVerticalCollectionView; element = element.parent)
            if (element.ClassListContains("unity-collection-view__item")) element.style.backgroundColor = UnityEngine.Color.clear;
    }
}
