// ReSharper disable CheckNamespace

using System;
using Compose.Net;
using SharpExtensions;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

public class Clip
{
    private readonly VisualElement _element;
    private int _count;
    private ModifierValueBinding<StyleEnum<Overflow>>? _binding;
    private readonly ClipOwner _owner = new();
    private StyleEnum<Overflow> _originalOverflow;
    private sealed class ClipOwner : ModifierNode
    {
        public override bool CanUpdate(IModifier modifier) => false;
        public override void Update(IModifier modifier) => throw new InvalidOperationException();
    }

    
    public Clip(VisualElement element)
    {
        _element = element;
    }

    public void Increment()
    {
        if (_count == 0)
        {
            if (_element.GetReusableComposeNode() is UnityReusableComposeNode node)
                _binding = node.Styles.Overflow.Add(_owner, Overflow.Hidden);
            else
            {
                _originalOverflow = _element.style.overflow;
                _element.style.overflow = Overflow.Hidden;
            }
        }
        _count++;
    }

    public void Decrement()
    {
        if (_count <= 0) throw new InvalidOperationException("Unbalanced clip release");
        _count--;
        if (_count == 0)
        {
            if (_binding != null) { _binding.Dispose(); _binding = null; }
            else _element.style.overflow = _originalOverflow;
        }
    }
}

public static partial class VisualElementExtensions
{
    public static Clip Clip(this VisualElement element)
    {
        const string key = "Compose.Net.Unity_Clip";
        if (element.UserData().GetOrDefault(key, null) is { } cached)
            return (Clip)cached.NotNull();
        var newInstance = new Clip(element);
        element.UserData()[key] = newInstance;
        return newInstance;
    }
}