using Compose.Net;
using SharpExtensions;

namespace Compose.Net.Unity.Runtime.Adapters.Modifiers;

internal class InsetsModifiersFactoryImpl : IInsetsModifiersFactory
{
    public IModifier Padding(Optional<Dp> top, Optional<Dp> bottom, Optional<Dp> left, Optional<Dp> right)
    {
        return new PaddingModifierImpl(top, bottom, left, right);
    }
}
