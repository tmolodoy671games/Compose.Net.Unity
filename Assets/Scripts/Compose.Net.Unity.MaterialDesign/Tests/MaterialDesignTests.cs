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

namespace Compose.Net.Unity.MaterialDesign.Tests;

public sealed class MaterialDesignTests : EditorWindowUITestFixture<MaterialDesignWindow>
{
    [SetUp] public void Reset() { RecreatePanel(); panelSize = new Vector2(900, 800); simulate.needsRendering = true; }
    private IEnumerator Settle()
    {
        var deadline = EditorApplication.timeSinceStartup + .3;
        while (EditorApplication.timeSinceStartup < deadline) { yield return null; simulate.FrameUpdate(); }
    }
    private VisualElement Find(string name) => rootVisualElement.Q("md." + name);
    private string Count => ((TextElement)Find("count")).text;

    [UnityTest] public IEnumerator ThemeSwitchRetainsStateAndDisabledButtonRejectsInput()
    {
        yield return Settle();
        Assert.That(Find("button").layout.height, Is.GreaterThanOrEqualTo(48));
        var light = Find("count").resolvedStyle.color;
        simulate.Click(Find("button"));
        yield return Settle();
        Assert.That(Count, Is.EqualTo("Actions: 1"));
        simulate.Click(Find("theme"));
        yield return Settle();
        Assert.That(Find("count").resolvedStyle.color, Is.Not.EqualTo(light));
        Assert.That(Count, Is.EqualTo("Actions: 1"));
        simulate.Click(Find("enabled"));
        yield return Settle();
        simulate.Click(Find("button"));
        yield return Settle();
        Assert.That(Count, Is.EqualTo("Actions: 1"));
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "compose-material3");
        System.IO.Directory.CreateDirectory(directory);
        rootVisualElement.CaptureToPNG(System.IO.Path.Combine(directory, "gallery-dark.png"));
    }
}
