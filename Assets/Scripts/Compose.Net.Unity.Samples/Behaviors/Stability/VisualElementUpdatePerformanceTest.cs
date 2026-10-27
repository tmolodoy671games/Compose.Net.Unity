// ReSharper disable ArrangeNamespaceBody

using System.Collections;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Samples.Behaviors.Stability
{
    internal class VisualElementUpdatePerformanceTest : MonoBehaviour
    {
        private float _fps;
        private float _minFps = float.PositiveInfinity;
        private float _maxFps;
        private long _frameCount;
        private double _frameSeconds;
        private float _nextDisplayTime;
        private string _fpsText = "FPS: --\nMin: --\nMax: --\nAvg: --";
        private GUIStyle? _fpsStyle;

        private void Update()
        {
            if (!Application.isPlaying)
            {
                _frameCount = 0;
                _frameSeconds = 0;
                _minFps = float.PositiveInfinity;
                _maxFps = 0;
                _nextDisplayTime = 0;
                return;
            }

            var delta = Time.unscaledDeltaTime;
            if (delta <= 0 || float.IsNaN(delta) || float.IsInfinity(delta)) return;
            _fps = 1f / delta;
            _minFps = Mathf.Min(_minFps, _fps);
            _maxFps = Mathf.Max(_maxFps, _fps);
            _frameCount++;
            _frameSeconds += delta;
            if (Time.unscaledTime < _nextDisplayTime) return;
            _nextDisplayTime = Time.unscaledTime + 0.25f;
            _fpsText = $"FPS: {_fps:F1}\nMin: {_minFps:F1}\nMax: {_maxFps:F1}\nAvg: {_frameCount / _frameSeconds:F1}";
        }

        private void OnGUI()
        {
            if (!Application.isPlaying || !isActiveAndEnabled) return;
            _fpsStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                alignment = TextAnchor.UpperLeft,
                padding = new RectOffset(10, 10, 8, 8),
                normal = { textColor = UnityEngine.Color.white }
            };
            var bounds = new UnityEngine.Rect(12, 12, 220, 108);
            var previousColor = GUI.color;
            GUI.color = new UnityEngine.Color(0, 0, 0, 0.65f);
            GUI.DrawTexture(bounds, Texture2D.whiteTexture);
            GUI.color = UnityEngine.Color.white;
            GUI.Label(bounds, _fpsText, _fpsStyle);
            GUI.color = previousColor;
        }

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