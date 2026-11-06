using Compose.Net.MaterialDemo;

namespace Compose.Net.Unity.MaterialDemo.Editor;

/// <summary>Run either preview from the Unity Project browser or the Rider gutter.</summary>
public static partial class MaterialDesignPreviews
{
    [UnityPreview("Material surfaces — Light & Dark", 1100)]
    [Composable]
    public static void Surfaces()
    {
        MaterialDesignLab.SurfacesContent();
    }

    [UnityPreview("Material 3 — Light", 760, 900)]
    [Composable]
    public static void Light()
    {
        MaterialDesignLab.Content(initialDark: false);
    }

    [UnityPreview("Material 3 — Dark", 760, 900)]
    [Composable]
    public static void Dark()
    {
        MaterialDesignLab.Content(initialDark: true);
    }

    [UnityPreview("Material colors — Light", 760, 900)]
    [Composable]
    public static void ColorsLight()
    {
        MaterialDesignLab.ColorsContent(dark: false);
    }

    [UnityPreview("Material colors — Dark", 760, 900)]
    [Composable]
    public static void ColorsDark()
    {
        MaterialDesignLab.ColorsContent(dark: true);
    }
}
