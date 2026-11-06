using System.Collections;
using System.Linq;
using Compose.Net.Material3;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Preview.Tests;

public sealed class SwitchRipplePreviewTests
{
    [UnityTest]
    public IEnumerator HoverIsClippedToTheThumbCircleInBothPositions()
    {
        foreach (var selected in new[] { false, true })
        {
            SwitchRipplePreviewContent.Selected = selected;
            SwitchRipplePreviewContent.Source = ComposeFunctions.MutableInteractionSource();
            var entry = UnityPreviewCatalog.Discover().Single(e => e.Method.DeclaringType == typeof(SwitchRipplePreviewContent));
            using var session = new UnityPreviewRenderSession(entry, 240, 160);
            session.SetRunning(true);
            for (var frame = 0; frame < 10; frame++) { session.Tick(); yield return null; }
            var layer = session.Root.Q("material.switch.stateLayer");
            Assert.That(layer.layout.width, Is.EqualTo(40).Within(.1));
            Assert.That(layer.layout.height, Is.EqualTo(40).Within(.1));
            Assert.That(layer.resolvedStyle.overflow, Is.EqualTo(Overflow.Hidden));
            Assert.That(layer.resolvedStyle.borderTopLeftRadius, Is.EqualTo(20).Within(.1));
            var center = layer.worldBound.center - session.Root.worldBound.position;
            var before = new Texture2D(2, 2);
            var after = new Texture2D(2, 2);
            try
            {
                before.LoadImage(session.Capture());
                SwitchRipplePreviewContent.Source.TryEmit(new IHoverInteraction.Enter { Initial = true });
                for (var frame = 0; frame < 10; frame++) { session.Tick(); yield return null; }
                after.LoadImage(session.Capture());
                var a = before.GetPixels32(); var b = after.GetPixels32();
                var changed = 0;
                for (var y = 0; y < after.height; y++)
                for (var x = 0; x < after.width; x++)
                {
                    var index = y * after.width + x;
                    if (a[index].Equals(b[index])) continue;
                    changed++;
                    var distance = Vector2.Distance(new Vector2(x + .5f, after.height - y - .5f), center);
                    Assert.That(distance, Is.LessThanOrEqualTo(22), "Hover leaked outside the thumb's circular state layer.");
                }
                Assert.That(changed, Is.GreaterThan(30), "Hover must render visible feedback.");
            }
            finally { Object.DestroyImmediate(before); Object.DestroyImmediate(after); }
        }
    }
}

public static partial class SwitchRipplePreviewContent
{
    public static bool Selected;
    public static IMutableInteractionSource Source = ComposeFunctions.MutableInteractionSource();

    [UnityPreview("Switch ripple regression", 240, 160)]
    [Composable]
    public static void Content()
    {
        MaterialTheme.Provide(content: () => SurfaceFunctions.Surface(modifier: Modifier.FillMaxSize(), content: () =>
            BoxFunctions.Box(modifier: Modifier.FillMaxSize(), contentAlignment: Alignment.Center, content: () =>
                SwitchFunctions.Switch(Selected, _ => { }, interactionSource: Source))));
    }
}
