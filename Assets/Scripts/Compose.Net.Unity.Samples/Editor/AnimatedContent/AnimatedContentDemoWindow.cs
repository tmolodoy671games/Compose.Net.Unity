using Compose.Net;
using static Compose.Net.ComposeFunctions;
using Compose.Net.Unity.Samples.Behaviors.Content;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Samples.Editor
{

public sealed partial class AnimatedContentDemoWindow : EditorWindow
{
    private ComposeView _view;

    public static void Open()
    {
        var window = GetWindow<AnimatedContentDemoWindow>();
        window.titleContent = new GUIContent("Animated Content");
        window.minSize = new Vector2(1080, 500);
        window.Show();
    }

    private void CreateGUI()
    {
        _view?.Dispose();
        rootVisualElement.Clear();
        _view = new ComposeView();
        _view.style.flexGrow = 1;
        rootVisualElement.Add(_view);
        _view.SetContent(Content);
    }

    [Composable]
    private static void Content() => AnimatedContentDemo.Screen(Remember(() => new AnimatedContentDemoState()));

    private void Update() { ComposeInvalidatorHolder.ComposeInvalidator.Tick(); Repaint(); }
    private void OnDisable() => _view?.Dispose();
}

}
