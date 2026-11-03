using StableCollections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

// Keep the brush description managed and allocation-free until a path needs baking.
// Entry snapshots own the keys: mutable stable lists must still invalidate the cache.
internal readonly record struct GradientDefinition(
    IStableList<Color> Colors, IStableList<float>? Stops, float Alpha, bool Radial,
    Vector2 Start, Vector2 End, float Radius, AddressMode AddressMode)
{
    private float Stop(int index) => Stops != null ? Stops[Radial ? 0 : index]
        : (float)index / (Radial ? Colors.Size : Colors.Size - 1);

    internal FillGradient Create(out GradientColorKey[] colors, out GradientAlphaKey[] alphas)
    {
        colors = new GradientColorKey[Colors.Size];
        alphas = new GradientAlphaKey[Colors.Size];
        for (var i = 0; i < Colors.Size; i++)
        {
            var color = Colors[i].ToUnityColor();
            var time = Stop(i);
            colors[i] = new GradientColorKey(color, time);
            alphas[i] = new GradientAlphaKey(color.a * Alpha, time);
        }
        var gradient = new Gradient { colorKeys = colors, alphaKeys = alphas };
        return Radial
            ? FillGradient.MakeRadialGradient(gradient, Start, Radius, End, AddressMode)
            : FillGradient.MakeLinearGradient(gradient, Start, End, AddressMode);
    }

    internal bool Matches(GradientDefinition previous, GradientColorKey[] colors, GradientAlphaKey[] alphas)
    {
        if (Radial != previous.Radial || Start != previous.Start || End != previous.End ||
            Radius != previous.Radius || AddressMode != previous.AddressMode || Colors.Size != colors.Length)
            return false;
        for (var i = 0; i < Colors.Size; i++)
        {
            var color = Colors[i].ToUnityColor();
            if (!colors[i].color.Equals(color) || !colors[i].time.Equals(Stop(i)) ||
                !alphas[i].alpha.Equals(color.a * Alpha)) return false;
        }
        return true;
    }
}
