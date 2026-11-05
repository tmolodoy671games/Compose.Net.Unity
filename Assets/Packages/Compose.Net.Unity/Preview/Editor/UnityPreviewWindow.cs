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

public sealed class UnityPreviewWindow : EditorWindow
{
    [SerializeField] private string _selectedId = "";
    private IReadOnlyList<UnityPreviewEntry> _entries = Array.Empty<UnityPreviewEntry>();
    private PopupField<string>? _selector;
    private VisualElement? _viewport;
    private Label? _status;
    private ToolbarButton? _playStop;
    private IComposer? _composer;
    private UnityPreviewAdapter? _adapter;
    public bool IsRunning { get; private set; }

    [MenuItem("Tools/Compose.Net/Unity Previews")]
    public static void Open() => GetWindow<UnityPreviewWindow>("Unity Previews").Show();

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
        DisposePreview();
        rootVisualElement.Clear();
        var toolbar = new Toolbar();
        _selector = new PopupField<string> { name = "preview.selector" };
        _selector.style.flexGrow = 1;
        _selector.RegisterValueChangedCallback(_ =>
        {
            var index = _selector.index;
            if (index >= 0 && index < _entries.Count) _selectedId = _entries[index].Id;
            StopPreview();
        });
        toolbar.Add(_selector);
        _playStop = new ToolbarButton(() =>
        {
            if (IsRunning) StopPreview();
            else StartPreview();
        }) { text = "Play", name = "preview.play-stop" };
        toolbar.Add(_playStop);
        rootVisualElement.Add(toolbar);
        _status = new Label { name = "preview.status" };
        rootVisualElement.Add(_status);
        var scroll = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
        scroll.style.flexGrow = 1;
        _viewport = new VisualElement { name = "preview.viewport" };
        _viewport.style.flexShrink = 0;
        _viewport.style.overflow = Overflow.Hidden;
        _viewport.RegisterCallback<FocusInEvent>(evt =>
        {
            if (IsRunning) return;
            _playStop?.Focus();
            evt.StopImmediatePropagation();
        }, TrickleDown.TrickleDown);
        scroll.Add(_viewport);
        rootVisualElement.Add(scroll);
        Refresh();
    }

    private UnityPreviewEntry? Selected => _entries.FirstOrDefault(entry => entry.Id == _selectedId);

    public void Refresh()
    {
        DisposePreview();
        _entries = UnityPreviewCatalog.Discover();
        if (_selector == null) return;
        _selector.choices = _entries.Select(entry => entry.DisplayName).ToList();
        if (_entries.Count == 0)
        {
            _selectedId = "";
            _selector.SetValueWithoutNotify("");
            SetStatus("No [UnityPreview] methods found.");
            return;
        }
        var selected = Selected ?? _entries[0];
        _selectedId = selected.Id;
        _selector.SetValueWithoutNotify(selected.DisplayName);
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
        if (entry == null || _viewport == null) { SetStatus("Select a preview first."); return; }
        if (EditorApplication.isPlayingOrWillChangePlaymode) { SetStatus("Run previews outside Play Mode."); return; }
        if (entry.Error != null) { SetStatus(entry.Error); return; }
        try
        {
            _selector?.SetValueWithoutNotify(entry.DisplayName);
            _viewport.style.width = entry.Settings.Width;
            _viewport.style.height = entry.Settings.Height;
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
            _playStop!.text = running ? "Stop" : "Play";
            SetStatus((running ? "Running — " : "Stopped — ") + entry.DisplayName);
        }
        catch (Exception exception) { Fail(exception); }
    }

    private void DisposePreview()
    {
        IsRunning = false;
        if (_playStop != null) _playStop.text = "Play";
        var composer = _composer;
        _composer = null;
        try { composer?.Dispose(); }
        finally
        {
            _adapter?.Dispose();
            _adapter = null;
            _viewport?.Clear();
            SetStatus("Stopped.");
        }
    }

    private void Update()
    {
        if (!IsRunning) return;
        try { _adapter!.Tick(); Repaint(); }
        catch (Exception exception) { Fail(exception); }
    }
    private void Fail(Exception exception)
    {
        DisposePreview();
        SetStatus(exception.ToString());
        Debug.LogException(exception);
    }
    private void SetStatus(string message) { if (_status != null) _status.text = message; }
    private sealed class Root : UnityReusableComposeNode, IReusableComposeNode
    {
        public Root(VisualElement element) : base(element) { }
        public new bool IsRoot => true;
    }
}
