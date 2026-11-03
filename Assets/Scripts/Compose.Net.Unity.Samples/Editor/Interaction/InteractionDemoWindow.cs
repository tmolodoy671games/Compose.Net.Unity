using Compose.Net;
using static Compose.Net.ComposeFunctions;
using Compose.Net.Unity.Samples.Behaviors.Content;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Samples.Editor
{

public sealed partial class InteractionDemoWindow : EditorWindow
{
    private ComposeView _view;

    [MenuItem("Tools/Compose.Net/Interaction Demo")]
    public static void Open()
    {
        var window = GetWindow<InteractionDemoWindow>();
        window.titleContent = new GUIContent("Interaction");
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
    private static void Content() => InteractionDemo.Screen(Remember(() => new InteractionDemoState()));

    private void Update() { ComposeInvalidatorHolder.ComposeInvalidator.Tick(); Repaint(); }
    private void OnDisable() => _view?.Dispose();
}

}
