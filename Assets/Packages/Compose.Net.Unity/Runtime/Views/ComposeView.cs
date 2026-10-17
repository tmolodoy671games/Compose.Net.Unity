using System;
using Compose.Net;
using Compose.Net.Unity;
using Compose.Net.Unity.Packages.Compose.Net.Unity.Runtime.Adapters;
using Compose.Net.Unity.Runtime.Adapters.Utils;
using UnityEngine.UIElements;
using PickingMode = UnityEngine.UIElements.PickingMode;

// ReSharper disable CheckNamespace

[UxmlElement]
public partial class ComposeView : VisualElement, IDisposable
{
    [Composable] private Action? _content;
    private readonly IReusableComposeNode _rootNode;
    private readonly IComposer _composer = IComposer.Create();
    private bool _isDisposed;

    public ComposeView()
    {
        pickingMode = PickingMode.Ignore;
        _rootNode = new UnityReusableComposeNode(this, true);
    }

    public void SetContent([Composable] Action content)
    {
        if (_content == content && !_isDisposed)
            return;
        Dispose();
        _isDisposed = false;
        _content = content;
        Bootstrap(
            adapter: ComposeUnityAdapter.Instance,
            composer: _composer,
            preview: !ApplicationUtils.IsPlaying,
            rootNode: _rootNode,
            config: new ComposeConfig(
                Logging: false
            ),
            content: content
        );
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;
        _isDisposed = true;
        _composer.Dispose();
        Clear();
    }
}