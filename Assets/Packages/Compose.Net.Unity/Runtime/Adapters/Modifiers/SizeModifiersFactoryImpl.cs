using Compose.Net;

namespace Compose.Net.Unity.Runtime.Adapters.Modifiers;

internal class SizeModifiersFactoryImpl : ISizeModifiersFactory
{
    public IModifier FillMaxSize(float widthFraction, float heightFraction)
    {
        return new FillMaxSizeModifierImpl(widthFraction, heightFraction);
    }

    public IModifier SizeIn(Dp minWidth, Dp maxWidth, Dp minHeight, Dp maxHeight)
    {
        return new SizeInModifierImpl(minWidth, maxWidth, minHeight, maxHeight);
    }

    public IModifier Size(Dp width, Dp height)
    {
        return new SizeModifierImpl(width, height);
    }

    public IModifier AnimateContentSize(AnimationSpec animationSpec)
    {
        return new AnimateContentSizeModifierImpl(animationSpec);
    }
}