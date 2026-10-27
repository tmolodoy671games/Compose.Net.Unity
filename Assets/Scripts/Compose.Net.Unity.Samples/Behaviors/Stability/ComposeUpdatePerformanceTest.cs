// ReSharper disable ArrangeNamespaceBody

using Compose.Net;

namespace Compose.Net.Unity.Samples.Behaviors.Stability
{
    internal partial class ComposeUpdatePerformanceTest : ComposeUI
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

        [Composable]
        protected override void Content()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            var parentSize = Remember(() => MutableStateOf(new Size()));
            Box(
                modifier: Modifier
                    .FillMaxSize()
                    .OnGloballyPositioned(it => parentSize.Value = it.Size),
                content: () =>
                {
                    if (float.IsNaN(parentSize.Value.Width) || parentSize.Value.Width <= 0)
                        return;
                    for (var i = 0; i < 1_000; i++)
                    {
                        var currentI = i;
                        Key(
                            key: currentI,
                            content: () => Item(currentI, parentSize.Value)
                        );
                    }
                }
            );
        }

        [BoxScope]
        [Composable]
        private static void Item(int currentI, Size parentSize)
        {
            var position = Remember(static () => MutableStateOf(new Offset()));

            LaunchedEffect(
                key: 1,
                coroutine: () => PerformanceUtils.MoveRandomlyCoroutine(
                    parentSize: () => parentSize,
                    it => position.Value = it
                )
            );

            var baseModifier = Remember(currentI, () => Modifier
                .Size(50.Dp())
                .Background(PerformanceUtils.GetColor(currentI))
                .Float()
            );
            Spacer(
                modifier: baseModifier.Offset(
                    x: position.Value.X.Dp(),
                    y: position.Value.Y.Dp()
                )
            );
        }
    }
}
