using System;
using System.Collections.Generic;
using Compose.Net.Unity.Runtime.Adapters;
using Compose.Net.Unity.Runtime.Adapters.Brushes;
using Compose.Net.Unity.Runtime.Adapters.Modifiers;
using Compose.Net.Unity.Runtime.Adapters.Nodes;
using Compose.Net.Unity.Runtime.Adapters.Transitions.Enter;
using Compose.Net.Unity.Runtime.Adapters.Transitions.Exit;
using SharpExtensions;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace Compose.Net.Unity.Packages.Compose.Net.Unity.Runtime.Adapters;

[NoAutoStaticsCleanup]
public class ComposeUnityAdapter : IComposeAdapter
{
    public static readonly ComposeUnityAdapter Instance = new();

    private ComposeUnityAdapter()
    {
    }

    public IDisposable StartTimeCoroutine(IEnumerable<TimeSpan?> coroutine) =>
        ComposeInvalidatorHolder.StartCoroutineAsDisposable(coroutine);

    public int Framerate()
    {
        var frameRate = Application.targetFrameRate;
        if (frameRate < 0)
            frameRate = Screen.currentResolution.refreshRateRatio.value.ToFloat().ToInt();
        return frameRate;
    }

    public ComposeInvalidator? ComposeInvalidatorInstance => ComposeInvalidatorHolder.ComposeInvalidator;
    public IModifiersFactory ModifiersFactory { get; } = new ModifiersFactoryImpl();
    public IReusableNodeFactory NodeFactory { get; } = new ReusableNodeFactoryImpl();
    public IComposeLogger Logger { get; } = new ComposeLoggerImpl();
    public IEnterTransitionsFactory EnterTransitionsFactory { get; } = new EnterTransitionsFactoryImpl();
    public IExitTransitionsFactory ExitTransitionsFactory { get; } = new ExitTransitionsFactoryImpl();
    public IBrushFactory BrushFactory { get; } = new BrushFactoryImpl();
}