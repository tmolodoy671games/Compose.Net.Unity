// ReSharper disable CheckNamespace

using System;
using Compose.Net;
using UnityEngine;

namespace Compose.Net.Unity;

public static partial class ComposeUnityFunctions
{
    [Composable]
    public static IState<UnityEngine.Color> AnimateColorAsState(
        UnityEngine.Color targetValue,
        AnimationSpec animationSpec = default
    )
    {
        return AnimateValueAsState(
            targetValue: targetValue,
            typeConverter: UnityEngine.Color.LerpUnclamped,
            animationSpec: animationSpec
        );
    }
}