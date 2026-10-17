// ReSharper disable CheckNamespace

using System;
using System.Collections;
using Compose.Net;
using UnityEngine;

namespace Compose.Net.Unity;

public static partial class ComposeUnityFunctions
{
    [Composable]
    public static void LaunchedEffect<TKey>(
        TKey key,
        Func<IEnumerator> coroutine
    )
    {
        DisposableEffect(
            key: key,
            effect: () =>
            {
                var coroutineInstance = ComposeInvalidatorHolder.StartCoroutineAsDisposable(coroutine());
                return OnDispose(coroutineInstance.Dispose);
            }
        );
    }
}