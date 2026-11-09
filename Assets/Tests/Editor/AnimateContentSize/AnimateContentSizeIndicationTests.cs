using System;
using System.Collections;
using System.Collections.Generic;
using Compose.Net;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Samples.Tests;

public sealed partial class AnimateContentSizeDemoTests
{
    private RecordingIndication? _recordingIndication;

    [UnityTest]
    public IEnumerator ControlsAndAnimatedContentDrawTheDefaultPressIndication()
    {
        _recordingIndication = new RecordingIndication();
        _view.Dispose(); _view.SetContent(Content); Flush();
        try
        {
            yield return PressAndRelease("animate.both.large");
            Flush(1000);
            Assert.That(_state["both"].Target.Value, Is.EqualTo(2));
            yield return PressAndRelease("animate.both.target");
            Flush(1000);
            Assert.That(_state["both"].Target.Value, Is.Zero);
            Assert.That(_state["both"].CompletedA.Value, Is.EqualTo(2));
        }
        finally { _recordingIndication = null; }
    }

    private IEnumerator PressAndRelease(string tag)
    {
        var element = Element(tag);
        simulate.MouseDown(element);
        try
        {
            var deadline = EditorApplication.timeSinceStartup + 1;
            while (!_recordingIndication!.VisiblePresses.Contains(tag) && EditorApplication.timeSinceStartup < deadline)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                yield return null;
                // UITestFixture drives panel frames without running MonoBehaviour.Update.
                typeof(Compose.Net.Unity.Packages.Compose.Net.Unity.Runtime.Adapters.ComposeUnityAdapter)
                    .GetMethod("TickFrame", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                    .Invoke(Compose.Net.Unity.Packages.Compose.Net.Unity.Runtime.Adapters.ComposeUnityAdapter.Instance, null);
                Flush();
            }
            Assert.That(_recordingIndication!.VisiblePresses, Does.Contain(tag),
                "A successful click must also render the library's default press indication");
        }
        finally { simulate.MouseUp(element); }
        for (var i = 0; i < 3; i++) { yield return null; Flush(); }
    }

    // Wrap the ambient implementation so the real panel still renders the default ripple.
    // Counting click callbacks alone did not detect the demo bypassing Modifier.Clickable.
    private sealed partial class RecordingIndication : IIndication
    {
        internal IIndication Native = null!;
        internal readonly HashSet<string> VisiblePresses = new();
        [Composable] public IIndicationInstance RememberUpdatedInstance(IInteractionSource source) =>
            new Instance(this, Native.RememberUpdatedInstance(source));

        private sealed record Instance(RecordingIndication Owner, IIndicationInstance Inner) : IIndicationInstance
        {
            public void DrawIndication(IDrawScope scope)
            {
                var (_, alpha, scale) = RecordedRipple.LatestPress(Inner);
                if (alpha > 0.05f && scale > 0.05f && scope.Size.Width > 0 && scope.Size.Height > 0)
                {
                    var element = scope.Context().visualElement;
                    while (element != null && string.IsNullOrEmpty(element.name)) element = element.parent;
                    if (element != null) Owner.VisiblePresses.Add(element.name);
                }
                Inner.DrawIndication(scope);
            }
        }
    }
}

// Ripple instances contain a frame per active press; an idle ripple has no frames.
internal static class RecordedRipple
{
    internal static (Offset Center, float Alpha, float Scale) LatestPress(IIndicationInstance indication)
    {
        var property = indication.GetType().GetProperty("Presses");
        Assert.That(property, Is.Not.Null, "The default indication must expose its press frames");
        var presses = (Array)property!.GetValue(indication)!;
        if (presses.Length == 0) return default;
        var press = presses.GetValue(presses.Length - 1)!;
        var type = press.GetType();
        return ((Offset)type.GetProperty("Center")!.GetValue(press)!,
            (float)type.GetProperty("Alpha")!.GetValue(press)!,
            (float)type.GetProperty("Scale")!.GetValue(press)!);
    }
}
