using System.Collections;
using System.IO;
using System.Linq;
using Compose.Net.Unity.MaterialDemo.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.UIElements.TestFramework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Compose.Net.Unity.MaterialDemo.Tests;

public sealed class MaterialDemoTests : EditorWindowUITestFixture<MaterialDemoWindow>
{
    [SetUp] public void Reset() { RecreatePanel(); panelSize = new Vector2(680, 620); simulate.needsRendering = true; }
    private IEnumerator Settle()
    {
        var deadline = EditorApplication.timeSinceStartup + 0.35;
        while (EditorApplication.timeSinceStartup < deadline) { yield return null; simulate.FrameUpdate(); }
    }
    private VisualElement Find(string tag) => rootVisualElement.Q("materials." + tag);
    private void Capture(string name)
    {
        var dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "compose-materials"); Directory.CreateDirectory(dir);
        rootVisualElement.CaptureToPNG(System.IO.Path.Combine(dir, name + ".png"));
    }

    [UnityTest] public IEnumerator BothThemesShowFiveStaticMaterials()
    {
        yield return Settle();
        foreach (var theme in new[] { "light", "dark" })
        {
            Assert.That(Find(theme), Is.Not.Null);
            for (var i = 0; i < 5; i++)
            {
                var swatch = Find(theme + "." + i);
                Assert.That(swatch, Is.Not.Null);
                Assert.That(swatch.layout.width, Is.GreaterThan(0));
                Assert.That(swatch.worldBound.yMax, Is.LessThanOrEqualTo(Find(theme).worldBound.yMax));
                var before = Pixels(swatch);
                simulate.Click(swatch);
                yield return Settle();
                Assert.That(Changed(before, Pixels(swatch)), Is.Zero, "Swatches must remain static after clicks");
            }
        }
        Assert.That(Changed(Pixels(Find("light.0")), Pixels(Find("dark.0"))), Is.GreaterThan(200),
            "Light and dark themes must render distinct palettes");
        Capture("gallery");
    }

    private static Color32[] Pixels(VisualElement element)
    {
        var render = element.CaptureToRenderTexture(); var previous = RenderTexture.active;
        var texture = new Texture2D(render.width, render.height, TextureFormat.RGBA32, false);
        try
        {
            RenderTexture.active = render;
            texture.ReadPixels(new UnityEngine.Rect(0, 0, render.width, render.height), 0, 0); texture.Apply();
            return texture.GetPixels32();
        }
        finally { RenderTexture.active = previous; UnityEngine.Object.DestroyImmediate(texture); render.Release(); UnityEngine.Object.DestroyImmediate(render); }
    }

    private static int Changed(Color32[] a, Color32[] b) => a.Zip(b, (x, y) =>
        System.Math.Abs(x.r - y.r) + System.Math.Abs(x.g - y.g) + System.Math.Abs(x.b - y.b)).Count(delta => delta > 8);
}
