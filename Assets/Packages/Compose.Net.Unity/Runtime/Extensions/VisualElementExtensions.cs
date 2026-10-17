// ReSharper disable CheckNamespace

using Compose.Net;
using StableCollections;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

public static partial class VisualElementExtensions
{
    public static IMutableStableMap<object?, object?> UserData(this VisualElement visualElement)
    {
        if (visualElement.userData is IMutableStableMap<object?, object?> dictionary)
            return dictionary;
        var newDictionary = MutableStableMapOf<object?, object?>();
        visualElement.userData = newDictionary;
        return newDictionary;
    }

    public static VisualElement? GetOrNull(this VisualElement visualElement, int index)
    {
        if (index < 0 || index >= visualElement.childCount)
            return null;
        return visualElement[index];
    }

    public static VisualElement VisualElement(this EventBase evt)
    {
        return (VisualElement)evt.target;
    }

    public static ILayoutCoordinates LayoutCoordinates(this VisualElement visualElement)
    {
        const string key = "Compose.Net.Unity_LayoutCoordinates";
        if (visualElement.UserData().GetOrNull(key) is ILayoutCoordinates existingCoordinates)
            return existingCoordinates;
        var newCoordinates = new LayoutCoordinatesImpl(visualElement);
        visualElement.UserData()[key] = newCoordinates;
        return newCoordinates;
    }
}