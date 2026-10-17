using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Compose.Net;
using SharpExtensions;
using UnityRandom = UnityEngine.Random;

namespace Compose.Net.Unity.Samples.Behaviors.Stability;

public static class PerformanceUtils
{
    private static readonly Color[] Colors =
    {
        Color.RedColor,
        Color.GreenColor,
        Color.BlueColor,
        Color.Yellow,
        Color.Cyan,
        Color.Magenta,
        Color.White,
        Color.Gray,
    };

    public static Color GetColor(int index) => Colors[index % Colors.Length];

    public static IEnumerator MoveRandomlyCoroutine(Func<Size> parentSize, Action<Offset> onValueChanged)
    {
        var parentSizeValue = parentSize();
        while (float.IsNaN(parentSizeValue.Width) || float.IsNaN(parentSizeValue.Height))
        {
            parentSizeValue = parentSize();
            yield return null;
        }

        var current = new Offset(
            UnityRandom.Range(0f, parentSizeValue.Width),
            UnityRandom.Range(0f, parentSizeValue.Height)
        );

        while (true)
        {
            var target = new Offset(
                UnityRandom.Range(0f, parentSizeValue.Width),
                UnityRandom.Range(0f, parentSizeValue.Height)
            );

            var elapsed = 0f;

            var interval = UnityEngine.Random.Range(1f, 2f);
            while (elapsed < interval)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / interval);
                var value = Offset.Lerp(current, target, t);
                onValueChanged?.Invoke(value);

                parentSizeValue = parentSize();
                yield return null;
            }

            onValueChanged?.Invoke(target);
            current = target;
        }
    }
}