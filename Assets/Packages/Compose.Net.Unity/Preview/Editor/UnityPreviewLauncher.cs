using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using IOPath = System.IO.Path;

namespace Compose.Net.Unity.Preview;

[Serializable]
[AiGenerated]
internal sealed class UnityPreviewRequest
{
    public string clientId = "", requestId = "", op = "", sourceFile = "", previewId = "";
    public int line = 1, width, height, screenWidth, screenHeight;
    public bool running, refreshed;
}

[Serializable]
[AiGenerated]
internal sealed class UnityPreviewFrameState
{
    public int protocol = 1, width, height;
    public string requestId = "", previewId = "", name = "", error = "";
    public bool running, autoWidth, autoHeight;
    public long heartbeat, revision;
}

// Per-project local mailboxes; no listening port, catalog dropdown or renderer window.
[InitializeOnLoad]
[global::Unity.Scripting.LifecycleManagement.NoAutoStaticsCleanup]
[AiGenerated]
internal static class UnityPreviewLauncher
{
    private static readonly string BaseDirectory = IOPath.GetFullPath("Library/ComposeNetPreview");
    private static readonly string Inbox = IOPath.Combine(BaseDirectory, "requests");
    private static readonly Dictionary<string, Client> Clients = new();
    private static double _nextPoll;
    static UnityPreviewLauncher()
    {
        EditorApplication.update += Tick;
        AssemblyReloadEvents.beforeAssemblyReload += Dispose;
        EditorApplication.quitting += Dispose;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.ExitingPlayMode)
                CloseClients();
        };
    }

    private static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        var now = EditorApplication.timeSinceStartup;
        if (now >= _nextPoll)
        {
            _nextPoll = now + .008;
            if (Directory.Exists(Inbox))
                foreach (var file in Directory.GetFiles(Inbox, "*.json").OrderBy(path => path, StringComparer.Ordinal).Take(128))
                    Process(file, now);
        }
        foreach (var pair in Clients.ToArray())
        {
            if (now - pair.Value.LastRequest > 10)
            {
                pair.Value.Dispose(); Clients.Remove(pair.Key); continue;
            }
            pair.Value.Tick(now);
        }
    }

    private static void Process(string file, double now)
    {
        UnityPreviewRequest? request = null;
        try
        {
            request = JsonUtility.FromJson<UnityPreviewRequest>(File.ReadAllText(file));
            if (request != null && request.op == "open" && !request.refreshed)
            {
                request.refreshed = true;
                AtomicWrite(file, System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(request)));
                AssetDatabase.Refresh();
                return;
            }
            File.Delete(file);
            if (request == null || !IsClientId(request.clientId)) return;
            if (!Clients.TryGetValue(request.clientId, out var client))
            {
                if (request.op != "open") return;
                client = new Client(IOPath.Combine(BaseDirectory, "clients", request.clientId));
                Clients.Add(request.clientId, client);
            }
            client.LastRequest = now;
            if (request.op == "open")
            {
                if (request.requestId == client.State.requestId && client.Session != null) return;
                client.State.requestId = request.requestId;
                var entries = UnityPreviewSource.Resolve(request.sourceFile, request.line);
                // An attribute-line click selects that variant; a method-line action selects the first.
                var selected = entries[0];
                if (entries.Count > 1)
                {
                    var lines = File.ReadAllLines(request.sourceFile);
                    var attributeLines = lines.Select((text, index) => (text, line: index + 1))
                        .Where(item => item.line <= request.line && item.text.Contains("UnityPreview"))
                        .Select(item => item.line).ToArray();
                    if (attributeLines.Length > 0 && attributeLines[^1] == request.line)
                    {
                        var before = lines.Skip(request.line).TakeWhile(text => !text.Contains(" void "))
                            .Count(text => text.Contains("UnityPreview"));
                        selected = entries[Math.Max(0, entries.Count - before - 1)];
                    }
                }
                client.Open(selected, now, request.width, request.height, request.screenWidth, request.screenHeight);
            }
            else if (request.op == "release") { client.Dispose(); Clients.Remove(request.clientId); }
            else if ((request.op == "playInUnity" || request.op == "setRunning" && request.running)
                && request.previewId == client.State.previewId)
                client.PlayInUnity();
            else if (request.op == "resize" && request.width > 0 && request.height > 0)
                client.Resize(request.width, request.height, now, request.screenWidth, request.screenHeight);

        }
        catch (Exception exception)
        {
            if (File.Exists(file)) File.Delete(file);
            if (request != null && Clients.TryGetValue(request.clientId, out var client)) client.Fail(exception.Message);
            else Debug.LogError("Compose.Net preview: " + exception.Message);
        }
    }

    private static bool IsClientId(string id) => id.Length == 32 && id.All(character => character >= '0' && character <= '9' || character >= 'a' && character <= 'f');
    private static void CloseClients() { foreach (var client in Clients.Values) client.Dispose(); Clients.Clear(); }
    private static void Dispose() { EditorApplication.update -= Tick; CloseClients(); }

    internal static void AtomicWrite(string path, byte[] bytes)
    {
        var temporary = path + ".tmp";
        File.WriteAllBytes(temporary, bytes);
        if (File.Exists(path)) File.Replace(temporary, path, null);
        else File.Move(temporary, path);
    }

    [AiGenerated]
    private sealed class Client : IDisposable
    {
        private readonly string _directory;
        private double _nextCapture, _nextPublish;
        public double LastRequest;
        public UnityPreviewRenderSession? Session;
        public readonly UnityPreviewFrameState State = new();
        public Client(string directory) { _directory = directory; Directory.CreateDirectory(directory); }
        public void Open(UnityPreviewEntry entry, double now, int width, int height, int screenWidth, int screenHeight)
        {
            Session?.Dispose(); Session = null;
            State.error = ""; State.running = false; State.revision = 0;
            State.previewId = entry.Id; State.name = string.IsNullOrWhiteSpace(entry.Settings.Name) ? entry.Method.Name : entry.Settings.Name;
            Session = new UnityPreviewRenderSession(entry, width > 0 ? width : 640, height > 0 ? height : 480, screenWidth, screenHeight);
            State.autoWidth = entry.Settings.Width <= 0; State.autoHeight = entry.Settings.Height <= 0;
            ResetFrameBuffer();
            _nextCapture = now + .2;
            Publish();
        }
        private void ResetFrameBuffer()
        {
            State.width = Session!.Width; State.height = Session.Height;
        }
        public void Resize(int width, int height, double now, int screenWidth, int screenHeight)
        {
            if (Session == null || !Session.Resize(width, height, screenWidth, screenHeight)) return;
            ResetFrameBuffer(); _nextCapture = now; Publish();
        }
        public void PlayInUnity()
        {
            if (Session != null) UnityPreviewWindow.Open(Session.Entry.Id, running: true, width: State.width, height: State.height);
        }
        public void Tick(double now)
        {
            try
            {
                if (Session != null && now >= _nextCapture && Session.Root.panel != null)
                {
                    AtomicWrite(IOPath.Combine(_directory, "frame.png"), Session.Capture());
                    State.revision++;
                    _nextCapture = now + 1;
                    Publish();
                }
                if (now >= _nextPublish) { Publish(); _nextPublish = now + .25; }
            }
            catch (Exception exception) { Fail(exception.Message); }
        }
        public void Fail(string message)
        {
            Session?.Dispose(); Session = null;
            State.error = message; State.running = false; State.revision = 0; Publish();
        }
        private void Publish()
        {
            State.heartbeat = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            AtomicWrite(IOPath.Combine(_directory, "state.json"), System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(State)));
        }
        public void Dispose()
        {
            Session?.Dispose(); Session = null;
            if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
        }
    }
}
