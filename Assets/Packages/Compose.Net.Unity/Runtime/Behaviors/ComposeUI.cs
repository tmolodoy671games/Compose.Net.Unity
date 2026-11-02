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
        // PanelRenderer retains its tree on disable and does not reload it on re-enable.
        // A callback registered while that tree is detached is not invoked immediately.
        _composeView?.SetContent(ApplicationUtils.IsPlaying ? Content : Preview);
    }

    private void OnDisable()
    {
        _composeView?.Dispose();
        DisablePanelRenderer();
        // UIDocument destroys its tree; PanelRenderer reuses the same ComposeView.
        if (!GetComponent<PanelRenderer>()) _composeView = null;
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
        SetComposeView(root.Q<ComposeView>());
    }

    private void EnableUiDocument()
    {
        var document = GetComponent<UIDocument>();
        if (!document) return;
        SetComposeView(document.rootVisualElement?.Q<ComposeView>());
    }

    private void SetComposeView(ComposeView? view)
    {
        if (!ReferenceEquals(_composeView, view)) _composeView?.Dispose();
        _composeView = view;
        _composeView?.SetContent(ApplicationUtils.IsPlaying ? Content : Preview);
    }
}