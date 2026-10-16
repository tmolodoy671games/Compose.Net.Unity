// ReSharper disable ArrangeNamespaceBody

using System.Collections;
using UnityEngine.UIElements;

namespace UnityCompose.Samples.Behaviors.Stability
{
    internal class VisualElementUpdatePerformanceTest : MonoBehaviour
    {
        private void Awake()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            var root = GetComponent<UIDocument>().rootVisualElement.Q<ComposeView>();
            StartCoroutine(AddChildrenWhenReady(root));
        }

        private IEnumerator AddChildrenWhenReady(VisualElement root)
        {
            while (float.IsNaN(root.contentRect.size.x) || root.contentRect.size.y <= 0)
                yield return null;
            for (var i = 0; i < 1_000; i++)
            {
                var childElement = new VisualElement
                {
                    style =
                    {
                        backgroundColor = PerformanceUtils.GetColor(i).ToUnityColor(),
                        width = 50,
                        height = 50,
                        position = Position.Absolute
                    }
                };
                root.Add(childElement);

                StartCoroutine(
                    PerformanceUtils.MoveRandomlyCoroutine(
                        () => root.layout.size.ToSize(),
                        it =>
                        {
                            childElement.style.left = it.X;
                            childElement.style.top = it.Y;
                        }
                    )
                );
            }
        }
    }
}