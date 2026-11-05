using Compose.Net;
using static Compose.Net.ComposeFunctions;
using Compose.Net.Unity.Samples.Behaviors.Content;
using UnityEditor;
using UnityEngine;

namespace Compose.Net.Unity.Samples.Editor
{
    public sealed partial class ClickableDemoWindow : EditorWindow
    {
        private ComposeView? _view;

        public static void Open()
        {
            var window = GetWindow<ClickableDemoWindow>();
            window.titleContent = new GUIContent("Clickable Demo");
            window.minSize = new Vector2(1000, 500);
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
        private static void Content() => ClickableDemo.Screen(RememberFunctions.Remember(() => new ClickableDemoState()));

        private void Update()
        {
            ComposeInvalidatorHolder.ComposeInvalidator.Tick();
            Repaint();
        }

        private void OnDisable() => _view?.Dispose();
    }
}
