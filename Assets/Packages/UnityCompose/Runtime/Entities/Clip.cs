// ReSharper disable CheckNamespace

using SharpExtensions;
using UnityEngine.UIElements;

namespace UnityCompose;

public class Clip
{
    private readonly VisualElement _element;
    private int _count;
    
    public Clip(VisualElement element)
    {
        _element = element;
    }

    public void Increment()
    {
        if (_count == 0)
            _element.style.overflow = Overflow.Hidden;
        _count++;
    }

    public void Decrement()
    {
        _count--;
        if (_count == 0)
            _element.style.overflow = StyleKeyword.Null;
    }
}

public static partial class VisualElementExtensions
{
    public static Clip Clip(this VisualElement element)
    {
        const string key = "UnityCompose_Clip";
        if (element.UserData().GetOrDefault(key, null) is { } cached)
            return (Clip)cached.NotNull();
        var newInstance = new Clip(element);
        element.UserData()[key] = newInstance;
        return newInstance;
    }
}