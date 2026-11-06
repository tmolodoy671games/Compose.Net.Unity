using System.Collections;
using System.Linq;
using Compose.Net.Material3;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Compose.Net.Unity.Preview.Tests;

public sealed class MaterialSurfacePreviewTests
{
    [UnityTest]
    public IEnumerator RoundedMaterialSurfaceRetainsItsFillInCapture()
    {
        var entry = UnityPreviewCatalog.Discover().Single(e => e.Method.DeclaringType == typeof(MaterialSurfacePreviewContent));
        using var session = new UnityPreviewRenderSession(entry, 240, 160);
        for (var frame = 0; frame < 10; frame++) yield return null;
        var texture = new Texture2D(2, 2);
        try
        {
            Assert.That(texture.LoadImage(session.Capture()), Is.True);
            var primaryPixels = texture.GetPixels32().Count(p => p.r > 90 && p.r < 120 && p.g > 65 && p.g < 95 && p.b > 145 && p.b < 180);
            Assert.That(primaryPixels, Is.GreaterThan(500), "Rounded clips must preserve the purple surface fill, rather than render a white stencil mask.");
        }
        finally { Object.DestroyImmediate(texture); }
    }
}

public static partial class MaterialSurfacePreviewContent
{
    [UnityPreview("Material surface regression", 240, 160)]
    [Composable]
    public static void Content()
    {
        MaterialTheme.Provide(content: () =>
            SurfaceFunctions.Surface(modifier: Modifier.FillMaxSize(), content: () =>
                ButtonFunctions.Button(() => { }, () => TextFunctions.Text("Material"))));
    }
}
