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
        panelSize = new Vector2(1040, 980);
        simulate.needsRendering = true;
    }
    private IEnumerator Settle()
    {
        var deadline = EditorApplication.timeSinceStartup + .75;
        while (EditorApplication.timeSinceStartup < deadline) { yield return null; simulate.FrameUpdate(); }
    }
    private string Title(string prefix) => rootVisualElement.Q<Label>(prefix + ".title").text;
    private void Click(string tag) => simulate.Click(rootVisualElement.Q(tag));

    [UnityTest] public IEnumerator TwoLevelsRetainSelectionAndRemainIndependent()
    {
        yield return Settle();
        foreach (var theme in new[] { "light", "dark" })
        foreach (var material in new[] { "ceramic", "satin", "metal" })
        {
            var p = "tabs." + theme + "." + material;
            Assert.That(Title(p), Is.EqualTo("Workspace / Overview"));
            Click(p + ".secondary.tab.1"); yield return Settle();
            Assert.That(Title(p), Is.EqualTo("Workspace / Activity"));
            Click(p + ".primary.tab.1"); yield return Settle();
            Assert.That(Title(p), Is.EqualTo("Discover / For you"));
            Click(p + ".secondary.tab.2"); yield return Settle();
            Assert.That(Title(p), Is.EqualTo("Discover / Following"));
            Click(p + ".primary.tab.0"); yield return Settle();
            Assert.That(Title(p), Is.EqualTo("Workspace / Activity"));
            Click(p + ".primary.tab.2"); yield return Settle();
            Click(p + ".secondary.tab.2"); yield return Settle();
            Assert.That(Title(p), Is.EqualTo("Library / Saved"), "Disabled archive rejects input");
            Click(p + ".motion"); yield return Settle();
            Click(p + ".secondary.tab.1"); yield return Settle();
            Assert.That(Title(p), Is.EqualTo("Library / Recent"));
            Click(p + ".primary.tab.0"); yield return Settle();
            Click(p + ".secondary.tab.0"); yield return Settle();
            Click(p + ".secondary.tab.0"); yield return Settle();
            Assert.That(Title(p), Is.EqualTo("Workspace / Overview"));
            var panel = rootVisualElement.Q(p + ".panel");
            Assert.That(panel.worldBound.yMax, Is.LessThanOrEqualTo(rootVisualElement.worldBound.yMax));
            foreach (var otherTheme in new[] { "light", "dark" })
            foreach (var otherMaterial in new[] { "ceramic", "satin", "metal" })
                if (theme != otherTheme || material != otherMaterial)
                    Assert.That(Title("tabs." + otherTheme + "." + otherMaterial), Is.EqualTo("Workspace / Overview"));
        }
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "compose-tabs");
        System.IO.Directory.CreateDirectory(directory);
        rootVisualElement.CaptureToPNG(System.IO.Path.Combine(directory, "gallery.png"));
    }

    [UnityTest] public IEnumerator RapidRetargetingAndResizeKeepLatestDestination()
    {
        yield return Settle();
        var p = "tabs.light.ceramic";
        foreach (var index in new[] { 2, 0, 1, 2, 1 })
        {
            Click(p + ".primary.tab." + index);
            yield return null; simulate.FrameUpdate();
        }
        yield return Settle();
        Assert.That(Title(p), Is.EqualTo("Discover / For you"));
        panelSize = new Vector2(840, 760);
        yield return Settle();
        var row = rootVisualElement.Q(p + ".primary");
        var first = rootVisualElement.Q(p + ".primary.tab.0");
        var last = rootVisualElement.Q(p + ".primary.tab.2");
        Assert.That(first.worldBound.xMin, Is.GreaterThanOrEqualTo(row.worldBound.xMin - 1));
        Assert.That(last.worldBound.xMax, Is.LessThanOrEqualTo(row.worldBound.xMax + 1));
        Click(p + ".primary.tab.0"); yield return Settle();
        Assert.That(Title(p), Is.EqualTo("Workspace / Overview"));
    }
}
