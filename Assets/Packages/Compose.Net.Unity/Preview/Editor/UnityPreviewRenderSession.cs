using System;
using Compose.Net.Unity.Packages.Compose.Net.Unity.Runtime.Adapters;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Compose.Net.Unity.Preview;

// A runtime panel with a texture target needs neither an EditorWindow nor Play Mode.
[AiGenerated]
internal sealed class UnityPreviewRenderSession : IDisposable
{
    private readonly GameObject _object;
    private readonly PanelSettings _settings;
    private readonly RenderTexture _target;
    private IComposer? _composer;
    private UnityPreviewAdapter? _adapter;
    private readonly UnityPreviewCapture _capture = new();
    public UnityPreviewEntry Entry { get; }
    public VisualElement Root { get; }
    public bool IsRunning { get; private set; }
    public int Width => _target.width;
    public int Height => _target.height;
    private Vector2 _screenLimit;
    public UnityPreviewRenderSession(UnityPreviewEntry entry, int availableWidth = 640, int availableHeight = 480, int screenWidth = 0, int screenHeight = 0)
    {
        if (entry.Error != null) throw new InvalidOperationException(entry.Error);
        _screenLimit = screenWidth > 0 && screenHeight > 0 ? new Vector2(screenWidth, screenHeight) : UnityPreviewSizing.CurrentContentLimit;
        var size = UnityPreviewSizing.Resolve(entry.Settings, new Vector2(availableWidth, availableHeight), _screenLimit);
        var width = Mathf.Max(1, Mathf.RoundToInt(size.Display.x));
        var height = Mathf.Max(1, Mathf.RoundToInt(size.Display.y));
        ValidateSize(width, height);
        Entry = entry;
        _settings = ScriptableObject.CreateInstance<PanelSettings>();
        _settings.hideFlags = HideFlags.HideAndDontSave;
        _settings.scaleMode = PanelScaleMode.ConstantPixelSize;
        _settings.scale = 1;
        _settings.clearColor = true;
        _settings.colorClearValue = UnityEngine.Color.white;
        // Rounded UI Toolkit clips need a stencil buffer, including nested Material surfaces.
        _target = new RenderTexture(width, height, 24)
            { hideFlags = HideFlags.HideAndDontSave };
        _target.Create();
        _settings.targetTexture = _target;
        _object = new GameObject("Compose.Net Rider Preview") { hideFlags = HideFlags.HideAndDontSave };
        var document = _object.AddComponent<UIDocument>();
        document.panelSettings = _settings;
        Root = document.rootVisualElement;
        Root.style.position = Position.Absolute;
        Root.style.left = Root.style.top = 0;
        ApplySize(size);
        Root.style.backgroundColor = UnityEngine.Color.white;
        Root.style.overflow = Overflow.Hidden;
        try { SetRunning(false); }
        catch { Dispose(); throw; }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Compose.Net", "CN_COMPOSABLE_PARAMETER:Non composable argument passed to composable parameter")]
    public void SetRunning(bool running)
    {
        DisposeComposition();
        _adapter = new UnityPreviewAdapter();
        _composer = IComposer.Create();
        try
        {
            ComposeFunctions.Bootstrap(_composer, _adapter, !running, new ComposeConfig(Logging: false),
                new RootNode(Root), Entry.CreateContent(), runLaunchedEffects: running);
            IsRunning = running;
            EditorApplication.QueuePlayerLoopUpdate();
        }
        catch { DisposeComposition(); throw; }
    }

    public void Tick() => _adapter?.Tick();
    public bool Resize(int availableWidth, int availableHeight, int screenWidth = 0, int screenHeight = 0)
    {
        if (screenWidth > 0 && screenHeight > 0) _screenLimit = new Vector2(screenWidth, screenHeight);
        var size = UnityPreviewSizing.Resolve(Entry.Settings, new Vector2(availableWidth, availableHeight), _screenLimit);
        var width = Mathf.Max(1, Mathf.RoundToInt(size.Display.x));
        var height = Mathf.Max(1, Mathf.RoundToInt(size.Display.y));
        if (width == Width && height == Height) return false;
        ValidateSize(width, height);
        _target.Release(); _target.width = width; _target.height = height; _target.Create();
        ApplySize(size);
        PrepareLayout();
        return true;
    }
    private void ApplySize(UnityPreviewSizing.Size size)
    {
        Root.style.width = size.Logical.x; Root.style.height = size.Logical.y;
        Root.style.scale = new Scale(new Vector3(size.Scale, size.Scale, 1));
        Root.style.transformOrigin = new UnityEngine.UIElements.TransformOrigin(0, 0);
    }
    private static void ValidateSize(int width, int height)
    {
        if ((long)width * height > 8_000_000) throw new InvalidOperationException("The preview exceeds 8 million pixels.");
    }
    private void PrepareLayout()
    {
        // Edit Mode has no Game View to size a runtime panel's outer visual tree.
        var panelRoot = Root.panel?.visualTree;
        if (panelRoot != null)
        {
            panelRoot.style.width = Width;
            panelRoot.style.height = Height;
        }
    }
    public byte[] Capture()
    {
        PrepareLayout();
        return _capture.Read(Root, _target);
    }
    private void DisposeComposition()
    {
        IsRunning = false;
        try { _composer?.Dispose(); }
        finally { _composer = null; _adapter?.Dispose(); _adapter = null; Root?.Clear(); }
    }
    public void Dispose()
    {
        try { DisposeComposition(); }
        finally
        {
            _capture.Dispose();
            Object.DestroyImmediate(_object);
            Object.DestroyImmediate(_settings);
            _target.Release(); Object.DestroyImmediate(_target);
        }
    }
    [AiGenerated]
    private sealed class RootNode : UnityReusableComposeNode, IReusableComposeNode
    {
        public RootNode(VisualElement root) : base(root) { }
        public new bool IsRoot => true;
    }
}

[AiGenerated]
internal sealed class UnityPreviewCapture : IDisposable
{
    private Texture2D? _pixels;
    private RenderTexture? _scratch;
    public void Render(VisualElement root, RenderTexture target)
    {
        if (_scratch == null || _scratch.width != target.width || _scratch.height != target.height)
        {
            if (_scratch != null) { _scratch.Release(); Object.DestroyImmediate(_scratch); }
            _scratch = new RenderTexture(target.width, target.height, 24) { hideFlags = HideFlags.HideAndDontSave };
            _scratch.Create();
        }
        var previous = RenderTexture.active;
        try { RenderTexture.active = target; GL.Clear(true, true, UnityEngine.Color.white); }
        finally { RenderTexture.active = previous; }
        root.CaptureIntoRenderTexture(_scratch);
    }
    private void ReadPixels(RenderTexture target)
    {
        if (_pixels == null || _pixels.width != target.width || _pixels.height != target.height)
        {
            if (_pixels != null) Object.DestroyImmediate(_pixels);
            _pixels = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false)
                { hideFlags = HideFlags.HideAndDontSave };
        }
        var previous = RenderTexture.active;
        try
        {
            RenderTexture.active = target;
            _pixels.ReadPixels(new UnityEngine.Rect(0, 0, target.width, target.height), 0, 0, false);
        }
        finally { RenderTexture.active = previous; }
    }
    public byte[] Read(VisualElement root, RenderTexture target)
    {
        Render(root, target);
        ReadPixels(target);
        return _pixels!.EncodeToPNG();
    }
    public void Dispose()
    {
        if (_pixels != null) Object.DestroyImmediate(_pixels); _pixels = null;
        if (_scratch != null) { _scratch.Release(); Object.DestroyImmediate(_scratch); _scratch = null; }
    }
}
