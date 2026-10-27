using System;
using System.Threading.Tasks;
using Compose.Net;

namespace Compose.Net.Unity.Runtime.Adapters.Modifiers;

internal class InputModifiersFactoryImpl : IInputModifiersFactory
{
    public IModifier Hoverable(IMutableInteractionSource interactionSource, bool enabled)
    {
        return new HoverableModiferImpl(interactionSource, enabled);
    }
    
    public IModifier PointerInput<T>(T key, Func<IPointerInputScope, Task> body)
    {
        return new PointerInputModifierImpl<T>(key, body);
    }
}
