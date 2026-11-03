using System;
using System.Threading.Tasks;
using Compose.Net;

namespace Compose.Net.Unity.Runtime.Adapters.Modifiers;

internal class InputModifiersFactoryImpl : IInputModifiersFactory
{
    public IModifier Draggable(IDraggableState state, Orientation orientation, bool enabled,
        IMutableInteractionSource? interactionSource, bool startDragImmediately,
        Action<Offset>? onDragStarted, Action<float>? onDragStopped, bool reverseDirection) =>
        new DraggableModifierImpl(state, orientation, enabled, interactionSource, startDragImmediately,
            onDragStarted, onDragStopped, reverseDirection);

    public IModifier Scrollable(IScrollableState state, Orientation orientation, bool enabled,
        bool reverseDirection, FlingBehavior? flingBehavior, IMutableInteractionSource? interactionSource) =>
        new ScrollableModifierImpl(state, orientation, enabled, reverseDirection, flingBehavior, interactionSource);

    public IModifier Hoverable(IMutableInteractionSource interactionSource, bool enabled)
    {
        return new HoverableModiferImpl(interactionSource, enabled);
    }
    
    public IModifier PointerInput<T>(T key, Func<IPointerInputScope, Task> body)
    {
        return new PointerInputModifierImpl<T>(key, body);
    }
}