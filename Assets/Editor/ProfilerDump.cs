using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;

static class ProfilerDump
{
    const string Capture = "ProfilerCaptures/Compose.Net.Unity_2026-10-18_01-43-30.data";
    const string Output = "Logs/profiler-dump-worst.txt";

    [InitializeOnLoadMethod]
    static void Run()
    {
        if (File.Exists(Output) || !File.Exists(Capture))
            return;
        EditorApplication.delayCall += Dump;
    }

    static void Dump()
    {
        ProfilerDriver.LoadProfile(Capture, false);
        var times = new List<(int f, float ms)>();
        for (var f = ProfilerDriver.firstFrameIndex; f <= ProfilerDriver.lastFrameIndex; f++)
        {
            using var raw = ProfilerDriver.GetRawFrameDataView(f, 0);
            if (raw.valid)
                times.Add((f, raw.frameTimeMs));
        }
        var sorted = times.Select(t => t.ms).OrderBy(t => t).ToList();
        var lines = new List<string> { $"frames {times.Count} median {sorted[sorted.Count / 2]:F2}ms p90 {sorted[sorted.Count * 9 / 10]:F2}ms over100ms {sorted.Count(t => t > 100)}" };
        foreach (var (f, ms) in times.OrderByDescending(t => t.ms).Take(6))
        {
            using var view = ProfilerDriver.GetHierarchyFrameDataView(f, 0, HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName, HierarchyFrameDataView.columnTotalTime, false);
            var self = new Dictionary<string, double>();
            var total = new Dictionary<string, double>();
            var calls = new Dictionary<string, double>();
            Walk(view, view.GetRootItemID(), "", 0, self, total, calls);
            lines.Add($"== frame {f} {ms:F1}ms");
            lines.AddRange(total.Where(p => p.Value > ms * 0.03).OrderByDescending(p => p.Value).Take(60).Select(p => $"{p.Value:F1}\t{calls[p.Key]:F0}\t{Short(p.Key)}"));
            lines.Add("-- self");
            lines.AddRange(self.OrderByDescending(p => p.Value).Take(25).Select(p => $"{p.Value:F1}\t{p.Key}"));
        }
        File.WriteAllLines(Output, lines);
    }

    static string Short(string path) => string.Join("/", path.Split('/').Select(s => s.Contains("!") ? s.Substring(s.IndexOf('!') + 1) : s).Select(s => s.Length > 70 ? s.Substring(s.Length - 70) : s).TakeLast(5));

    static void Walk(HierarchyFrameDataView view, int id, string path, int depth, Dictionary<string, double> self, Dictionary<string, double> total, Dictionary<string, double> calls)
    {
        if (depth > 60)
            return;
        var children = new List<int>();
        view.GetItemChildren(id, children);
        foreach (var child in children)
        {
            var name = view.GetItemName(child);
            var p = path + "/" + name;
            self[name] = self.GetValueOrDefault(name) + view.GetItemColumnDataAsFloat(child, HierarchyFrameDataView.columnSelfTime);
            if (depth < 60)
            {
                total[p] = total.GetValueOrDefault(p) + view.GetItemColumnDataAsFloat(child, HierarchyFrameDataView.columnTotalTime);
                calls[p] = calls.GetValueOrDefault(p) + view.GetItemColumnDataAsFloat(child, HierarchyFrameDataView.columnCalls);
            }
            Walk(view, child, depth < 60 ? p : path, depth + 1, self, total, calls);
        }
    }
}
