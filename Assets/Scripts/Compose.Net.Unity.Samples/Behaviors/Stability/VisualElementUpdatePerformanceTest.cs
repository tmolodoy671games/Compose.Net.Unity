// ReSharper disable ArrangeNamespaceBody

using System.Collections;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Samples.Behaviors.Stability
{
    internal class VisualElementUpdatePerformanceTest : MonoBehaviour
    {
        private void Awake()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
        }

        private void OnEnable()
        {
            GetComponent<PanelRenderer>().RegisterUIReloadCallback(OnUIReload);
        }

        private void OnDisable()
        {
            GetComponent<PanelRenderer>().UnregisterUIReloadCallback(OnUIReload);
        }

        private void OnUIReload(PanelRenderer renderer, VisualElement root, int version)
        {
            // Every reload brings a new tree, so the old children and their movers are gone.
            StopAllCoroutines();
            StartCoroutine(AddChildrenWhenReady(root.Q<ComposeView>()));
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
                            childElement.style.translate = it.ToVector2();
                        }
                    )
                );
            }
        }
    }
}