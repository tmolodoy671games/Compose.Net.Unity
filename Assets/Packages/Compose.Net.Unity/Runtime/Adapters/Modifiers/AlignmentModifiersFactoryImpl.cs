using Compose.Net;

namespace Compose.Net.Unity.Runtime.Adapters.Modifiers;

internal class AlignmentModifiersFactoryImpl : IAlignmentModifiersFactory
{
    public IModifier HorizontalAlignment(Alignment.Horizontal horizontalAlignment)
    {
        return new HorizontalAlignModifierImpl(horizontalAlignment);
    }

    public IModifier VerticalAlignment(Alignment.Vertical verticalAlignment)
    {
        return new VerticalAlignModifierImpl(verticalAlignment);
    }

    public IModifier Alignment(Alignment alignment)
    {
        return new AlignModifierImpl(alignment);
    }

    public IModifier Position(Dp top, Dp bottom, Dp left, Dp right)
    {
        return new PositionModifierImpl(top, bottom, left, right);
    }

    public IModifier Weight(float weight) => new WeightModifierImpl(weight);
    public IModifier Float(bool enabled) => new FloatModifierImpl(enabled);
}