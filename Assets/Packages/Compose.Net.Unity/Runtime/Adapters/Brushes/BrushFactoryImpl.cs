using Compose.Net;
using SharpExtensions;
using StableCollections;

namespace Compose.Net.Unity.Runtime.Adapters.Brushes;

internal class BrushFactoryImpl : IBrushFactory
{
    public IBrush SolidColor(Color color) => new SolidColorBrushImpl(color);

    public IBrush LinearGradient(
        IStableList<Color> colors,
        IStableList<float>? stops,
        Offset start,
        Offset end,
        TileMode tileMode
    )
    {
        return new LinearGradientBrushImpl(colors, stops, start, end, tileMode);
    }

    public IBrush RadialGradient(
        IStableList<Color> colors,
        IStableList<float>? stops,
        Optional<Offset> center,
        float radius,
        TileMode tileMode
    )
    {
        return new RadialGradientBrushImpl(colors, stops, center, radius, tileMode);
    }
}