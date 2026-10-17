// ReSharper disable CheckNamespace

using System.Diagnostics.CodeAnalysis;
using Compose.Net;
using Compose.Net.Unity.Runtime.Adapters.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

[ExecuteAlways]
[DefaultExecutionOrder(-1)]
public abstract partial class ComposeUI : MonoBehaviour
{
    [Composable]
    protected abstract void Content();

    [Composable]
    protected virtual void Preview()
    {
    }

    private ComposeView? _composeView;

    [SuppressMessage("Compose.Net", "CN_COMPOSABLE_PARAMETER:Non composable argument passed to composable parameter")]
    private void OnEnable()
    {
        EnablePanelRenderer();
        EnableUiDocument();
    }

    private void OnDisable()
    {
        _composeView?.Dispose();
        _composeView = null;
        DisablePanelRenderer();
    }

    private void EnablePanelRenderer()
    {
        var renderer = GetComponent<PanelRenderer>();
        if (!renderer) return;
        renderer.RegisterUIReloadCallback(OnUIReload);
    }

    private void DisablePanelRenderer()
    {
        var renderer = GetComponent<PanelRenderer>();
        if (!renderer) return;
        renderer.UnregisterUIReloadCallback(OnUIReload);
    }

    private void OnUIReload(
        PanelRenderer renderer,
        VisualElement root,
        int version
    )
    {
        _composeView = root.Q<ComposeView>();
        _composeView?.SetContent(ApplicationUtils.IsPlaying ? Content : Preview);
    }

    private void EnableUiDocument()
    {
        var document = GetComponent<UIDocument>();
        if (!document) return;
        _composeView = document.rootVisualElement?.Q<ComposeView>();
        _composeView?.SetContent(ApplicationUtils.IsPlaying ? Content : Preview);
    }
}