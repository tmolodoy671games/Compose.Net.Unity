using System;
using Compose.Net;

namespace Compose.Net.Unity.Runtime.Adapters.Modifiers;

internal class SizeModifiersFactoryImpl : ISizeModifiersFactory
{
    public IModifier FillMaxSize(float widthFraction, float heightFraction)
    {
        return new ChainDependentModifier(new FillMaxSizeModifierImpl(widthFraction, heightFraction));
    }

    public IModifier SizeIn(Dp minWidth, Dp maxWidth, Dp minHeight, Dp maxHeight)
    {
        return new ChainDependentModifier(new SizeInModifierImpl(minWidth, maxWidth, minHeight, maxHeight));
    }

    public IModifier Size(Dp width, Dp height)
    {
        return new ChainDependentModifier(new SizeModifierImpl(width, height));
    }

    public IModifier WrapContent(Orientation? orientation, Alignment align, bool unbounded) =>
        new ChainDependentModifier(new WrapContentModifierImpl(orientation, align, unbounded));

    public IModifier Intrinsic(Orientation orientation, IntrinsicSize size, bool enforceIncoming)
    {
        IModifier intrinsic = new ChainDependentModifier(new IntrinsicSizeModifierImpl(orientation, size));
        if (enforceIncoming) return intrinsic;
        // Measure at the required extent, then center the child in the parent's allocation.
        IModifier outer = new ChainDependentModifier(new WrapContentModifierImpl(null, Alignment.Center, true, orientation));
        return outer.Then(intrinsic);
    }

    public IModifier AnimateContentSize(AnimationSpec animationSpec, Alignment alignment, Action<Size, Size>? finishedListener)
    {
        return new AnimateContentSizeModifierImpl(animationSpec, alignment, finishedListener);
    }
}
