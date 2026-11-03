using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Compose.Net.Unity.Samples.Behaviors.Content;
using Compose.Net.Unity.Samples.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Tests;

public sealed class BoxSceneTests
{
    [Test] public void AddToSceneIsIdempotentAndKeepsUnrelatedObjects()
    {
        var original=SceneManager.GetActiveScene();
        var selection=Selection.activeObject;
        var scene=EditorSceneManager.NewPreviewScene();
        try
        {
            var samples=new GameObject("Samples");SceneManager.MoveGameObjectToScene(samples,scene);
            var other=new GameObject("Keep me");SceneManager.MoveGameObjectToScene(other,scene);
            BoxDemoScene.EnsureInScene(scene);
            var demo=samples.GetComponentInChildren<BoxDemo>();Assert.That(demo,Is.Not.Null);
            var renderer=demo.GetComponent<PanelRenderer>();Assert.That(renderer.panelSettings,Is.Not.Null);
            Assert.That(renderer.visualTreeAsset,Is.Not.Null);Assert.That(renderer.sortingOrder,Is.EqualTo(40));
            demo.name="Renamed Box Demo";demo.gameObject.SetActive(false);
            BoxDemoScene.EnsureInScene(scene);
            Assert.That(samples.GetComponentsInChildren<BoxDemo>(true).Length,Is.EqualTo(1));
            Assert.That(demo.gameObject.activeSelf,Is.True);Assert.That(other.scene,Is.EqualTo(scene));
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
            if(original.IsValid())SceneManager.SetActiveScene(original);
            Selection.activeObject=selection;
        }
    }
    [Test] public void SampleSceneContainsActiveBoxDemoWithLinkedRendererAndParent()
    {
        var scene=File.ReadAllText("Assets/Scenes/SampleScene.unity");
        Assert.That(scene,Does.Contain("m_Name: BoxDemo"));
        Assert.That(scene,Does.Contain("Compose.Net.Unity.Samples.Behaviors.Content.BoxDemo"));
        var blocks=Regex.Split(scene,@"(?=--- !u!)");
        var go=blocks.Single(block=>block.Contains("m_Name: BoxDemo\n"));
        Assert.That(go,Does.Contain("m_IsActive: 1"));
        var id=Regex.Match(go,@"&([0-9]+)").Groups[1].Value;
        Assert.That(blocks.Any(block=>block.Contains("PanelRenderer:")&&block.Contains("m_GameObject: {fileID: "+id+"}")&&block.Contains("m_SortingOrder: 40")),Is.True);
        var transform=blocks.Single(block=>block.Contains("Transform:")&&block.Contains("m_GameObject: {fileID: "+id+"}"));
        var parent=Regex.Match(transform,@"m_Father: \{fileID: ([0-9]+)\}").Groups[1].Value;
        var child=Regex.Match(transform,@"&([0-9]+)").Groups[1].Value;
        Assert.That(parent,Is.Not.EqualTo("0"));
        Assert.That(blocks.Single(block=>block.StartsWith("--- !u!4 &"+parent+"\n")),Does.Contain("- {fileID: "+child+"}"));
    }
}
