using Compose.Net;
using static Compose.Net.ComposeFunctions;
using Compose.Net.Unity.Samples.Behaviors.Content;
using UnityEditor;
using UnityEngine;

namespace Compose.Net.Unity.Samples.Editor
{
    public sealed partial class ScrollDemoWindow : EditorWindow
    {
        private ComposeView? _view;

        public static void Open()
        {
            var window = GetWindow<ScrollDemoWindow>();
            window.titleContent = new GUIContent("Scroll Demo");
            window.minSize = new Vector2(1140, 600);
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
        private static void Content() => ScrollDemo.Screen(RememberFunctions.Remember(() => new ScrollDemoState()));

        private void Update()
        {
            ComposeInvalidatorHolder.ComposeInvalidator.Tick();
            Repaint();
        }

        private void OnDisable() => _view?.Dispose();
    }
}
