using Compose.Net;

namespace Compose.Net.Unity.Runtime.Adapters.Modifiers;

internal class InsetsModifiersFactoryImpl : IInsetsModifiersFactory
{
    public IModifier Padding(Dp top, Dp bottom, Dp left, Dp right)
    {
        return new PaddingModifierImpl(top, bottom, left, right);
    }
}