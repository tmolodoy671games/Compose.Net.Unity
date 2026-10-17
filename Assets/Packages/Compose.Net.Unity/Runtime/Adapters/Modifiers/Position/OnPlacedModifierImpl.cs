// ReSharper disable CheckNamespace

using System;
using Compose.Net;
using SharpExtensions;
using StableCollections;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record OnPlacedModifierImpl(
    Action<ILayoutCoordinates> OnPlaced
) : UnityModifier
{
    private readonly EventCallback<GeometryChangedEvent> _onGeometryChanged = OnGeometryChanged(OnPlaced);

    protected override void Apply(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    )
    {
        element.RegisterCallback(_onGeometryChanged);
    }

    protected override void Revert(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    )
    {
        element.UnregisterCallback(_onGeometryChanged);
    }

    private static EventCallback<GeometryChangedEvent> OnGeometryChanged(Action<ILayoutCoordinates> onPlaced)
    {
        var previousBounds = Optional.Empty<RelativeLayoutBounds>();
        return it =>
        {
            var coordinates = it.VisualElement().LayoutCoordinates();
            var newBounds = coordinates.ToRelativeLayoutBounds();
            if (previousBounds.Equals(newBounds))
                return;
            previousBounds = newBounds;
            onPlaced(coordinates);
        };
    }
}