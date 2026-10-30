using System;
using Compose.Net;

namespace Compose.Net.Unity.Runtime.Adapters.Modifiers;

internal class TransformModifiersFactoryImpl : ITransformModifiersFactory
{
    public IModifier Offset(Dp x, Dp y) => new OffsetModifierImpl(x, y);
    public IModifier Offset(Func<Action<Offset>, IDisposable> subscribe) => new ObservedOffsetModifierImpl(subscribe);
    public IModifier Rotate(float degrees) => new RotateModifierImpl(degrees);
    public IModifier Scale(float scaleX, float scaleY) => new ScaleModifierImpl(scaleX, scaleY);
    public IModifier TransformOrigin(Dp originX, Dp originY) => new TransformOriginModifierImpl(originX, originY);


    public IModifier VerticalScroll(
        IScrollState state,
        bool enabled,
        IFlingBehavior? flingBehavior,
        bool reverseScrolling,
        IOverscrollEffect? overscrollEffect = null
    )
    {
        return new VerticalScrollModifierImpl(
            Enabled: true,
            State: state,
            ScrollMultiplier: 40,
            ReverseScrolling: reverseScrolling,
            UserScrollEnabled: enabled,
            InteractionSource: state.InteractionSource as IMutableInteractionSource,
            FlingBehavior: flingBehavior,
            OverscrollEffect: overscrollEffect
        );
    }

    public IModifier HorizontalScroll(
        IScrollState state,
        bool enabled,
        IFlingBehavior? flingBehavior,
        bool reverseScrolling,
        IOverscrollEffect? overscrollEffect = null
    )
    {
        return new HorizontalScrollModifierImpl(
            Enabled: true,
            State: state,
            ScrollMultiplier: 40,
            ReverseScrolling: reverseScrolling,
            UserScrollEnabled: enabled,
            InteractionSource: state.InteractionSource as IMutableInteractionSource,
            FlingBehavior: flingBehavior,
            OverscrollEffect: overscrollEffect
        );
    }
}
