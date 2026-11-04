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
    [SetUp] public void Reset() { RecreatePanel(); panelSize = new Vector2(900, 1400); simulate.needsRendering = true; }
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

    [UnityTest] public IEnumerator ControlsUpdateAndDepthDoesNotChangeLayout()
    {
        yield return Settle();
        var before = Find("preview").layout;
        for (var i = 0; i < 5; i++)
        {
            simulate.Click(Find("Material." + i)); yield return Settle();
            Assert.That(window.State.Material.Value, Is.EqualTo(i));
        }
        simulate.Click(Find("Material.3")); yield return Settle();
        for (var i = 0; i < 4; i++)
        {
            simulate.Click(Find("Weave." + i)); yield return Settle();
            Assert.That(window.State.Weave.Value, Is.EqualTo(i));
            simulate.Click(Find("Depth." + i)); yield return Settle();
            Assert.That(window.State.DepthKind.Value, Is.EqualTo(i));
            Assert.That(Find("preview").layout, Is.EqualTo(before));
        }
        simulate.Click(Find("press")); yield return Settle();
        Assert.That(window.State.Clicks.Value, Is.EqualTo(1));
        window.State.DepthKind.Value = 1; window.State.Weave.Value = 0;
        yield return Settle(); Capture("light");
        simulate.Click(Find("theme")); yield return Settle(); Capture("dark");
        Assert.That(window.State.Dark.Value, Is.True);
        window.State.Opacity.Value = 0; yield return Settle(); Capture("transparent");
        simulate.Click(Find("inherit")); yield return Settle(); Capture("override");
        Assert.That(window.State.Inherit.Value, Is.False);
    }

    [UnityTest] public IEnumerator BitmapReliefChangesPixelsAndClothWeavesAreDistinct()
    {
        foreach (var name in new[] { "paper", "metal", "linen", "canvas", "felt", "twill" })
        {
            var texture = Resources.Load<Texture2D>("ComposeMaterials/" + name);
            Assert.That(texture, Is.Not.Null, name);
            Assert.That(texture.isReadable, Is.True, name);
        }
        window.State.Strength.Value = 0;
        yield return Settle();
        var flat = Pixels(Find("preview"));
        window.State.Strength.Value = 1;
        yield return Settle();
        var textured = Pixels(Find("preview"));
        Assert.That(Changed(flat, textured), Is.GreaterThan(200), "Bitmap relief must change actual rendered pixels");
        for (var i = 1; i < 4; i++)
        {
            window.State.Weave.Value = i; yield return Settle();
            var next = Pixels(Find("preview"));
            Assert.That(Changed(textured, next), Is.GreaterThan(200), "Each weave must have its own bitmap");
            textured = next;
        }
        window.State.Scale.Value = 128; yield return Settle();
        Assert.That(Changed(textured, Pixels(Find("preview"))), Is.GreaterThan(200), "Tile size must affect UV scale");
        Capture("textures");
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
