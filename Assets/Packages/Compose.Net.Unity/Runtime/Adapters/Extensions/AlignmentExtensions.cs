using Compose.Net;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Runtime.Adapters.Extensions;

public static class AlignmentExtensions
{
    private readonly record struct AlignAndJustify(
        Align Align,
        Justify Justify = Justify.FlexStart
    );

    public static Align ToAlign(this Alignment alignment) => alignment.ToAlignAndJustify().Align;
    public static Justify ToJustify(this Alignment alignment) => alignment.ToAlignAndJustify().Justify;

    private static AlignAndJustify ToAlignAndJustify(this Alignment alignment)
    {
        if (alignment == Alignment.Left)
            return new AlignAndJustify(Align.FlexStart);
        if (alignment == Alignment.CenterHorizontally)
            return new AlignAndJustify(Align.Center);
        if (alignment == Alignment.Right)
            return new AlignAndJustify(Align.FlexEnd);

        if (alignment == Alignment.Top)
            return new AlignAndJustify(Align.FlexStart);
        if (alignment == Alignment.CenterVertically)
            return new AlignAndJustify(Align.Center);
        if (alignment == Alignment.Bottom)
            return new AlignAndJustify(Align.FlexEnd);

        if (alignment == Alignment.TopLeft)
            return new AlignAndJustify(Align.FlexStart);
        if (alignment == Alignment.TopCenter)
            return new AlignAndJustify(Align.Center);
        if (alignment == Alignment.TopRight)
            return new AlignAndJustify(Align.FlexEnd);
        
        if (alignment == Alignment.CenterLeft)
            return new AlignAndJustify(Align.FlexStart, Justify.Center);
        if (alignment == Alignment.Center)
            return new AlignAndJustify(Align.Center, Justify.Center);
        if (alignment == Alignment.CenterRight)
            return new AlignAndJustify(Align.FlexEnd, Justify.Center);
        
        if (alignment == Alignment.BottomLeft)
            return new AlignAndJustify(Align.FlexStart, Justify.FlexEnd);
        if (alignment == Alignment.BottomCenter)
            return new AlignAndJustify(Align.Center, Justify.FlexEnd);
        if (alignment == Alignment.BottomRight)
            return new AlignAndJustify(Align.FlexEnd, Justify.FlexEnd);
        
        return new AlignAndJustify(Align.FlexStart, Justify.FlexStart);
    }
}