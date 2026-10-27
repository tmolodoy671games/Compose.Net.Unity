// ReSharper disable CheckNamespace

using System;
using Compose.Net;
using SharpExtensions;
using StableCollections;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record OnSizeChangedImpl(
    Action<Size> OnSizeChanged
) : UnityModifier
{
    private readonly EventCallback<GeometryChangedEvent> _onGeometryChanged = OnGeometryChanged(OnSizeChanged);

    protected override void OnAttach(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    )
    {
        element.RegisterCallback(_onGeometryChanged);
    }

    protected override void OnDetach(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    )
    {
        element.UnregisterCallback(_onGeometryChanged);
    }

    private static EventCallback<GeometryChangedEvent> OnGeometryChanged(Action<Size> onSizeChanged)
    {
        var previousSize = Optional.Empty<Size>();
        return it =>
        {
            var coordinates = it.VisualElement().LayoutCoordinates();
            var newSize = coordinates.Size;
            if (previousSize.Equals(newSize))
                return;
            previousSize = newSize;
            onSizeChanged(newSize);
        };
    }
}
