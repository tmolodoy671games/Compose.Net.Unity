using Compose.Net;

namespace Compose.Net.Unity.Runtime.Adapters.Modifiers;

internal class InsetsModifiersFactoryImpl : IInsetsModifiersFactory
{
    public IModifier Padding(Dp top, Dp bottom, Dp left, Dp right)
    {
        var density = UnityDensity.Current;
        if (top.ToPx(density) < 0 || bottom.ToPx(density) < 0 ||
            left.ToPx(density) < 0 || right.ToPx(density) < 0)
            throw new System.ArgumentOutOfRangeException(nameof(top), "Padding must be non-negative");
        return new PaddingModifierImpl(top, bottom, left, right);
    }
}
