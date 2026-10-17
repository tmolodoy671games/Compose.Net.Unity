// ReSharper disable CheckNamespace

using UnityEngine.UIElements;

namespace Compose.Net.Unity;

public class PickingMode
{
    private readonly VisualElement _element;
    private int _count;

    internal PickingMode(VisualElement element)
    {
        _element = element;
    }

    public void Increment()
    {
        if (_count == 0)
            _element.pickingMode = UnityEngine.UIElements.PickingMode.Position;
        _count++;
    }

    public void Decrement()
    {
        _count--;
        if (_count == 0)
            _element.pickingMode = UnityEngine.UIElements.PickingMode.Ignore;
    }
}

public static partial class VisualElementExtensions
{
    public static PickingMode PickingMode(this VisualElement element)
    {
        const string key = "Compose.Net.Unity.ComposePickingMode";
        var userData = element.UserData();
        if (userData.GetOrDefault(key, null) is { } cachedInstance)
            return (PickingMode)cachedInstance!;
        var newInstance = new PickingMode(element);
        userData[key] = newInstance;
        return newInstance;
    }
}