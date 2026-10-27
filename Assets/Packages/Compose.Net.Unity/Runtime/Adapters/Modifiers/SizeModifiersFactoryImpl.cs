using Compose.Net;
using SharpExtensions;

namespace Compose.Net.Unity.Runtime.Adapters.Modifiers;

internal class SizeModifiersFactoryImpl : ISizeModifiersFactory
{
    public IModifier FillMaxSize(float widthFraction, float heightFraction)
    {
        return new FillMaxSizeModifierImpl(widthFraction, heightFraction);
    }

    public IModifier SizeIn(Optional<Dp> minWidth, Optional<Dp> maxWidth, Optional<Dp> minHeight, Optional<Dp> maxHeight)
    {
        return new SizeInModifierImpl(minWidth, maxWidth, minHeight, maxHeight);
    }

    public IModifier Size(Optional<Dp> width, Optional<Dp> height)
    {
        return new SizeModifierImpl(width, height);
    }

    public IModifier AnimateContentSize(AnimationSpec animationSpec)
    {
        return new AnimateContentSizeModifierImpl(animationSpec, 0);
    }
}
