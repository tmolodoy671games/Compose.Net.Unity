using System;
using System.Collections;
using System.Collections.Generic;
using Compose.Net;
using SharpExtensions;
using UnityEngine;

// ReSharper disable ArrangeNamespaceBody
// ReSharper disable CheckNamespace

namespace UnityCompose
{
    [ExecuteAlways]
    [DefaultExecutionOrder(-1_000)]
    public class ComposeInvalidatorHolder : MonoBehaviour
    {
        private static ComposeInvalidatorHolder? _instance;
        private readonly ComposeInvalidator _invalidator = new();

        private static ComposeInvalidatorHolder Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = new GameObject(nameof(ComposeInvalidatorHolder))
                            { hideFlags = HideFlags.HideAndDontSave }
                        .AddComponent<ComposeInvalidatorHolder>();
                }

                return _instance!;
            }
        }

        public static ComposeInvalidator ComposeInvalidator => Instance._invalidator;

        private void OnEnable() => _instance = this;

        private void Update() => _invalidator.Tick();

        public static IDisposable StartCoroutineAsDisposable(IEnumerable<TimeSpan?>? coroutine) =>
            StartCoroutineAsDisposable(coroutine == null ? null : CoroutineAdapter(coroutine));

        public static IDisposable StartCoroutineAsDisposable(IEnumerator? coroutine)
        {
            if (coroutine == null)
                return new CustomDisposable(() => { });
            var holder = Instance;
            var handle = holder.StartCoroutine(coroutine);
            return new CustomDisposable(() =>
            {
                if (holder) holder.StopCoroutine(handle);
            });
        }

        private static IEnumerator CoroutineAdapter(IEnumerable<TimeSpan?> coroutine)
        {
            foreach (var step in coroutine)
                yield return step == null ? null : new WaitForSeconds(step.Value.TotalSeconds.ToFloat());
        }
    }
}