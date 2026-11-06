using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Compose.Net.Unity.Packages.Compose.Net.Unity.Runtime.Adapters;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Preview;

[AiGenerated]
public sealed class UnityPreviewWindow : EditorWindow
{
    [SerializeField] private string _selectedId = "";
    private bool _startWhenReady;
    private bool _centerWhenReady;
    private Vector2 _initialPreviewSize;
    private IReadOnlyList<UnityPreviewEntry> _entries = Array.Empty<UnityPreviewEntry>();
    private VisualElement? _viewport;
    private VisualElement? _host;
    private ScrollView? _scroll;
    private VisualElement? _canvas;
    private UnityEngine.Rect _screen;
    private UnityEngine.Rect _appliedScreen;
    private bool _guiReady;
    private double _nextScreenCheck;
    private HelpBox? _error;
    private VisualElement? _playStopIcon;
    private ToolbarButton? _playStop;
    private IComposer? _composer;
    private UnityPreviewAdapter? _adapter;
    public bool IsRunning { get; private set; }

    public static void Open(string previewId, bool running = false, int width = 0, int height = 0)
    {
        var window = Resources.FindObjectsOfTypeAll<UnityPreviewWindow>()
            .FirstOrDefault(candidate => candidate._selectedId == previewId);
        if (window == null)
        {
            window = CreateInstance<UnityPreviewWindow>();
            window._selectedId = previewId;
            window._startWhenReady = running;
            window._centerWhenReady = true;
            var screen = UnityPreviewSizing.ScreenAt(EditorGUIUtility.GetMainWindowPosition().center);
            var initial = window.position;
            initial.center = screen.center;
            window.position = initial;
            if (width > 0 && height > 0) window._initialPreviewSize = new Vector2(width, height);
            window.Show();
        }
        else
        {
            if (width > 0 && height > 0) { window._initialPreviewSize = new Vector2(width, height); window.FitInitialPreviewSize(); }
            window.SelectPreview(previewId);
            if (running) window.StartPreview();
        }
        window.Focus();
    }

    public void SelectPreview(string previewId)
    {
        _selectedId = previewId;
        Refresh();
    }

    private void OnEnable() => EditorApplication.playModeStateChanged += OnPlayModeChanged;
    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        DisposePreview();
    }
    private void OnPlayModeChanged(PlayModeStateChange state)
    {
        DisposePreview();
        if (state == PlayModeStateChange.EnteredEditMode) StopPreview();
    }

    private void CreateGUI()
    {
        _guiReady = false;
        DisposePreview();
        rootVisualElement.Clear();
        var toolbar = new Toolbar();
        toolbar.style.flexShrink = 0;
        _playStop = new ToolbarButton(() =>
        {
            if (IsRunning) StopPreview();
            else StartPreview();
        }) { name = "preview.play-stop", tooltip = "Play" };
        _playStopIcon = new VisualElement { pickingMode = UnityEngine.UIElements.PickingMode.Ignore };
        _playStopIcon.style.width = _playStopIcon.style.height = 14;
        _playStopIcon.generateVisualContent += DrawPlayStopIcon;
        _playStop.Add(_playStopIcon);
        toolbar.Add(_playStop);
        rootVisualElement.Add(toolbar);
        _error = new HelpBox("", HelpBoxMessageType.Error) { name = "preview.error" };
        _error.style.display = DisplayStyle.None;
        rootVisualElement.Add(_error);
        _host = new VisualElement { name = "preview.host" };
        _host.style.flexGrow = 1;
        _host.style.flexBasis = 0;
        _host.style.minWidth = _host.style.minHeight = 0;
        _host.style.overflow = Overflow.Hidden;
        _host.RegisterCallback<GeometryChangedEvent>(_ =>
        {
            ApplyWindowSizeConstraints();
            ApplyViewportSize();
            FitInitialPreviewSize();
            CenterInitialWindow();
        });
        _scroll = new ScrollView(ScrollViewMode.VerticalAndHorizontal)
        {
            horizontalScrollerVisibility = ScrollerVisibility.Hidden,
            verticalScrollerVisibility = ScrollerVisibility.Hidden
        };
        _scroll.style.position = Position.Absolute;
        _scroll.style.left = _scroll.style.top = _scroll.style.right = _scroll.style.bottom = 0;
        _scroll.contentViewport.RegisterCallback<GeometryChangedEvent>(_ => ApplyViewportSize());
        _canvas = new VisualElement { name = "preview.canvas" };
        _canvas.style.flexShrink = 0;
        _viewport = new VisualElement { name = "preview.viewport" };
        _viewport.style.position = Position.Absolute;
        _viewport.style.left = _viewport.style.top = 0;
        _viewport.style.flexShrink = 0;
        _viewport.style.overflow = Overflow.Hidden;
        _viewport.style.backgroundColor = UnityEngine.Color.white;
        _viewport.RegisterCallback<FocusInEvent>(evt =>
        {
            if (IsRunning) return;
            _playStop?.Focus();
            evt.StopImmediatePropagation();
        }, TrickleDown.TrickleDown);
        _canvas.Add(_viewport);
        _scroll.Add(_canvas);
        _host.Add(_scroll);
        rootVisualElement.Add(_host);
        _screen = UnityPreviewSizing.ScreenAt(position.center);
        Refresh();
        if (_startWhenReady) { _startWhenReady = false; StartPreview(); }
        EditorApplication.delayCall += () =>
        {
            if (!this) return;
            _guiReady = true;
            UpdateScreenBounds();
            FitInitialPreviewSize();
            CenterInitialWindow();
        };
    }

    private UnityPreviewEntry? Selected => _entries.FirstOrDefault(entry => entry.Id == _selectedId);

    public void Refresh()
    {
        DisposePreview();
        _entries = UnityPreviewCatalog.Discover();
        var selected = Selected;
        if (selected == null)
        {
            if (!string.IsNullOrEmpty(_selectedId)) SetError("This preview is no longer available. Reopen it from Rider.");
            return;
        }
        ApplyWindowSizeConstraints();
        titleContent = new GUIContent(string.IsNullOrWhiteSpace(selected.Settings.Name) ? selected.Method.Name : selected.Settings.Name);
        StopPreview();
    }

    public void StartPreview(string? id = null)
    {
        if (id != null) _selectedId = id;
        RenderPreview(true);
    }

    public void StopPreview() => RenderPreview(false);

    [SuppressMessage("Compose.Net", "CN_COMPOSABLE_PARAMETER:Non composable argument passed to composable parameter")]
    private void RenderPreview(bool running)
    {
        DisposePreview();
        var entry = Selected;
        if (entry == null || _viewport == null) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode) { SetError("Run previews outside Play Mode."); return; }
        if (entry.Error != null) { SetError(entry.Error); return; }
        try
        {
            ApplyViewportSize();
            _adapter = new UnityPreviewAdapter();
            _composer = IComposer.Create();
            ComposeFunctions.Bootstrap(_composer, _adapter, !running, new ComposeConfig(Logging: false),
                new Root(_viewport), entry.CreateContent(), runLaunchedEffects: running);
            IsRunning = running;
            if (!running)
            {
                // Block input without applying Unity's disabled styles to the preview.
                foreach (var element in _viewport.Query<VisualElement>().ToList()) element.focusable = false;
                var shield = new VisualElement { name = "preview.input-shield" };
                shield.style.position = Position.Absolute;
                shield.style.left = shield.style.right = shield.style.top = shield.style.bottom = 0;
                _viewport.Add(shield);
            }
            UpdatePlayStopIcon();
        }
        catch (Exception exception) { Fail(exception); }
    }

    private void DisposePreview()
    {
        IsRunning = false;
        UpdatePlayStopIcon();
        var composer = _composer;
        _composer = null;
        try { composer?.Dispose(); }
        finally
        {
            _adapter?.Dispose();
            _adapter = null;
            _viewport?.Clear();
            SetError("");
        }
    }

    private void Update()
    {
        if (!_guiReady) return;
        if (EditorApplication.timeSinceStartup >= _nextScreenCheck)
        {
            _nextScreenCheck = EditorApplication.timeSinceStartup + .25;
            UpdateScreenBounds();
        }
        if (!IsRunning) return;
        try { _adapter!.Tick(); Repaint(); }
        catch (Exception exception) { Fail(exception); }
    }
    private void ApplyViewportSize()
    {
        var entry = Selected;
        if (entry == null || _viewport == null) return;
        var available = _host?.contentRect.size ?? position.size;
        if (!(available.x > 0) || !(available.y > 0)) return;
        var sizing = UnityPreviewSizing.Resolve(entry.Settings, available, UnityPreviewSizing.ContentLimit(_screen));
        _viewport.style.width = sizing.Logical.x; _viewport.style.height = sizing.Logical.y;
        _viewport.style.scale = new Scale(new Vector3(sizing.Scale, sizing.Scale, 1));
        _viewport.style.transformOrigin = new UnityEngine.UIElements.TransformOrigin(0, 0);
        if (_canvas != null) { _canvas.style.width = sizing.Display.x; _canvas.style.height = sizing.Display.y; }
        if (_scroll != null)
        {
            // Decide against the host before scrollbars reserve space. Otherwise one
            // scrollbar can make the other appear even when the canvas fits exactly.
            _scroll.horizontalScrollerVisibility = sizing.Display.x > available.x + .5f ? ScrollerVisibility.Auto : ScrollerVisibility.Hidden;
            _scroll.verticalScrollerVisibility = sizing.Display.y > available.y + .5f ? ScrollerVisibility.Auto : ScrollerVisibility.Hidden;
        }
    }
    private void UpdateScreenBounds()
    {
        var screen = UnityPreviewSizing.ScreenAt(position.center);
        _screen = screen;
        if (_appliedScreen != screen)
        {
            _appliedScreen = screen;
            ApplyWindowSizeConstraints();
            ApplyViewportSize();
        }
        if (!docked)
        {
            var bounded = UnityPreviewSizing.Constrain(position, screen);
            if (position != bounded) position = bounded;
        }
    }
    private void ApplyWindowSizeConstraints()
    {
        var entry = Selected;
        if (entry == null || _host == null || !(_host.layout.width > 0) || !(_host.layout.height > 0)
            || !(rootVisualElement.layout.width > 0) || !(rootVisualElement.layout.height > 0)) return;
        var size = UnityPreviewSizing.Resolve(entry.Settings, _host.contentRect.size, UnityPreviewSizing.ContentLimit(_screen));
        // EditorWindow sizes include the toolbar, but attribute dimensions describe the canvas.
        var chrome = rootVisualElement.layout.size - _host.layout.size;
        var maximum = new Vector2(_screen.width, Mathf.Max(1, _screen.height - 32));
        var minimum = Vector2.Min(new Vector2(100, 100), maximum);
        if (entry.Settings.Width > 0) minimum.x = maximum.x = Mathf.Min(maximum.x, Mathf.Ceil(size.Display.x + chrome.x));
        if (entry.Settings.Height > 0) minimum.y = maximum.y = Mathf.Min(maximum.y, Mathf.Ceil(size.Display.y + chrome.y));
        if (minSize != minimum || maxSize != maximum)
        {
            minSize = Vector2.Min(minSize, maximum);
            maxSize = maximum;
            minSize = minimum;
        }
        if (docked) return;
        var rect = position;
        rect.size = new Vector2(Mathf.Clamp(rect.width, minimum.x, maximum.x), Mathf.Clamp(rect.height, minimum.y, maximum.y));
        rect = UnityPreviewSizing.Constrain(rect, _screen);
        if (position != rect) position = rect;
    }
    private void FitInitialPreviewSize()
    {
        if (!_guiReady || _initialPreviewSize.x <= 0 || _host == null) return;
        if (docked) { _initialPreviewSize = Vector2.zero; return; }
        var current = _host.contentRect.size;
        if (current.x <= 0 || current.y <= 0) return;
        var delta = _initialPreviewSize - current;
        _initialPreviewSize = Vector2.zero;
        var rect = position;
        rect.size = new Vector2(Mathf.Max(minSize.x, rect.width + delta.x), Mathf.Max(minSize.y, rect.height + delta.y));
        position = UnityPreviewSizing.Constrain(rect, _screen);
        ApplyWindowSizeConstraints();
    }
    private void CenterInitialWindow()
    {
        if (!_centerWhenReady || !_guiReady || _initialPreviewSize.x > 0 || _host == null
            || !(_host.layout.width > 0) || !(_host.layout.height > 0)) return;
        _centerWhenReady = false;
        if (docked) return;
        ApplyWindowSizeConstraints();
        var rect = position;
        rect.center = _screen.center;
        position = UnityPreviewSizing.Constrain(rect, _screen);
    }
    private void Fail(Exception exception)
    {
        DisposePreview();
        SetError(exception.ToString());
        Debug.LogException(exception);
    }
    private void SetError(string message)
    {
        if (_error == null) return;
        _error.text = message;
        _error.style.display = string.IsNullOrEmpty(message) ? DisplayStyle.None : DisplayStyle.Flex;
    }
    private void UpdatePlayStopIcon()
    {
        if (_playStop != null) _playStop.tooltip = IsRunning ? "Stop" : "Play";
        _playStopIcon?.MarkDirtyRepaint();
    }
    private void DrawPlayStopIcon(MeshGenerationContext context)
    {
        var painter = context.painter2D;
        painter.fillColor = EditorGUIUtility.isProSkin ? new UnityEngine.Color(.85f, .85f, .85f) : new UnityEngine.Color(.2f, .2f, .2f);
        painter.BeginPath();
        if (IsRunning)
        {
            painter.MoveTo(new Vector2(3, 3)); painter.LineTo(new Vector2(11, 3));
            painter.LineTo(new Vector2(11, 11)); painter.LineTo(new Vector2(3, 11));
        }
        else
        {
            painter.MoveTo(new Vector2(4, 2)); painter.LineTo(new Vector2(12, 7));
            painter.LineTo(new Vector2(4, 12));
        }
        painter.ClosePath(); painter.Fill();
    }
    [AiGenerated]
    private sealed class Root : UnityReusableComposeNode, IReusableComposeNode
    {
        public Root(VisualElement element) : base(element) { }
        public new bool IsRoot => true;
    }
}
