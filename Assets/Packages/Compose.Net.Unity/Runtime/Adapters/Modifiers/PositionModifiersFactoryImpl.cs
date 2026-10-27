using System;
using Compose.Net;

namespace Compose.Net.Unity.Runtime.Adapters.Modifiers;

internal class PositionModifiersFactoryImpl : IPositionModifiersFactory
{
    public IModifier OnGloballyPositioned(Action<ILayoutCoordinates> onGloballyPositioned)
    {
        return new OnGloballyPositionedModifierImpl(onGloballyPositioned);
    }

    public IModifier OnPlaced(Action<ILayoutCoordinates> onPlaced)
    {
        return new OnPlacedModifierImpl(onPlaced);
    }

    public IModifier OnSizeChanged(Action<Size> onSizeChanged)
    {
        return new OnSizeChangedImpl(onSizeChanged);
    }

    public IModifier OnLayoutRectChanged(Action<RelativeLayoutBounds> callback)
    {
        return new OnLayoutRectChangedModifierImpl(callback);
    }
}
