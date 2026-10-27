// ReSharper disable CheckNamespace

using System;
using System.Collections;
using Compose.Net;
using UnityEngine;

namespace Compose.Net.Unity;

public static partial class ComposeUnityFunctions
{
    [Composable, NonRestartableComposable]
    public static void LaunchedEffect<TKey>(
        TKey key,
        Func<IEnumerator> coroutine
    )
    {
        var stableCoroutine = Remember(key, () => coroutine);
        DisposableEffect(
            key: key,
            effect: () =>
            {
                var coroutineInstance = ComposeInvalidatorHolder.StartCoroutineAsDisposable(stableCoroutine());
                return OnDispose(coroutineInstance.Dispose);
            }
        );
    }
}