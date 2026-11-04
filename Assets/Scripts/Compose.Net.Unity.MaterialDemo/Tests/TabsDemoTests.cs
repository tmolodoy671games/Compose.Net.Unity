using System.Collections;
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

public sealed class TabsDemoTests : EditorWindowUITestFixture<TabsDemoWindow>
{
    [SetUp] public void Reset()
    {
        RecreatePanel();
        panelSize = new Vector2(840, 760);
        simulate.needsRendering = true;
    }

    private IEnumerator Settle()
    {
        var deadline = EditorApplication.timeSinceStartup + 0.35;
        while (EditorApplication.timeSinceStartup < deadline) { yield return null; simulate.FrameUpdate(); }
    }

    private string Title(string theme, string style) =>
        rootVisualElement.Q<Label>("tabs." + theme + "." + style + ".title").text;

    [UnityTest] public IEnumerator TabsSwitchContentIndependentlyInBothThemes()
    {
        yield return Settle();
        foreach (var theme in new[] { "light", "dark" })
        foreach (var style in new[] { "underline", "segmented", "pills" })
        {
            Assert.That(Title(theme, style), Is.EqualTo("Overview"));
            var prefix = "tabs." + theme + "." + style;
            var panel = rootVisualElement.Q(prefix + ".panel");
            Assert.That(panel.layout.width, Is.GreaterThan(0));
            Assert.That(panel.worldBound.yMax, Is.LessThanOrEqualTo(rootVisualElement.worldBound.yMax));
            foreach (var index in new[] { 1, 2, 2, 0 })
            {
                simulate.Click(rootVisualElement.Q(prefix + ".tab." + index));
                yield return Settle();
                Assert.That(Title(theme, style), Is.EqualTo(new[] { "Overview", "Activity", "Settings" }[index]));
                foreach (var otherTheme in new[] { "light", "dark" })
                foreach (var otherStyle in new[] { "underline", "segmented", "pills" })
                    if (otherTheme != theme || otherStyle != style)
                        Assert.That(Title(otherTheme, otherStyle), Is.EqualTo("Overview"));
            }
        }
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "compose-tabs");
        System.IO.Directory.CreateDirectory(directory);
        rootVisualElement.CaptureToPNG(System.IO.Path.Combine(directory, "gallery.png"));
    }
}
