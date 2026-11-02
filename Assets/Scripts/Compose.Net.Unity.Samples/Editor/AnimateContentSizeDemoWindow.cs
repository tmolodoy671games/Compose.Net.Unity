using Compose.Net;
using static Compose.Net.ComposeFunctions;
using Compose.Net.Unity.Samples.Behaviors.Content;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Samples.Editor;

[AiGenerated]
public sealed partial class AnimateContentSizeDemoWindow : EditorWindow
{
    private ComposeView _view;

    [MenuItem("Tools/Compose.Net/Animate Content Size Demo")]
    public static void Open()
    {
        var window = GetWindow<AnimateContentSizeDemoWindow>();
        window.titleContent = new GUIContent("Animate Content Size");
        window.minSize = new Vector2(1080, 500);
        window.Show();
    }

    private void CreateGUI()
    {
        _view?.Dispose();
        rootVisualElement.Clear();
        _view = new ComposeView();
        _view.style.flexShrink = 0;
        var scroll = new ScrollView(ScrollViewMode.Vertical);
        scroll.style.flexGrow = 1;
        scroll.Add(_view);
        rootVisualElement.Add(scroll);
        _view.SetContent(Content);
    }

    [Composable]
    private static void Content() => AnimateContentSizeDemo.Screen(Remember(() => new AnimateContentSizeDemoState()));

    private void Update() { ComposeInvalidatorHolder.ComposeInvalidator.Tick(); Repaint(); }
    private void OnDisable() => _view?.Dispose();
}
