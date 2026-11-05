using Compose.Net.MaterialDemo;
using Compose.Net.Unity.Packages.Compose.Net.Unity.Runtime.Adapters;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static Compose.Net.ComposeFunctions;

namespace Compose.Net.Unity.MaterialDemo.Editor;

public sealed partial class TabsDemoWindow : EditorWindow
{
    private IComposer? _composer;

    public static void Open()
    {
        var window = GetWindow<TabsDemoWindow>();
        window.titleContent = new GUIContent("Tabs Lab");
        window.minSize = new Vector2(840, 760);
        window.Show();
    }

    private void CreateGUI()
    {
        _composer?.Dispose();
        rootVisualElement.Clear();
        _composer = IComposer.Create();
        Bootstrap(_composer, ComposeUnityAdapter.Instance, false, new ComposeConfig(Logging: false),
            new Root(rootVisualElement), Content);
    }

    [Composable] private void Content() => TabsLab.Content();
    private void Update() { ComposeInvalidatorHolder.ComposeInvalidator.Tick(); Repaint(); }
    private void OnDisable() { _composer?.Dispose(); _composer = null; }
    private sealed class Root(VisualElement element) : UnityReusableComposeNode(element), IReusableComposeNode
    { public new bool IsRoot => true; }
}
