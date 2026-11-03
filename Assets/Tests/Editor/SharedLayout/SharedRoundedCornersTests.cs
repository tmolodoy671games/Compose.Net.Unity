using System.Reflection;
using Compose.Net;
using NUnit.Framework;
using static Compose.Net.ComposeFunctions;

namespace Compose.Net.Unity.Samples.Tests;

public sealed partial class SharedLayoutDemoTests
{
    [Test] public void RoundedCorners_OverlayClipUsesCurrentAnimatedBounds()
    {
        Select("clip", 1); Flush(500);
        var state = _state["clip"].States[1];
        var content = (SharedContent)typeof(SharedContentState).GetField("Content", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(state)!;
        var host = (ISharedContentHost)typeof(SharedContent).GetField("Host", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(content)!;
        var original = (SharedContentFrame)host.GetType().GetField("Frame", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(host)!;
        try
        {
            var frame = original with { Bounds = new Rect(new Offset(0, 0), new Size(200, 80)), Clip = new OverlayClip(RoundedCornerShape(1000.Dp())) };
            host.Render(frame);
            Near(Layer(Shared("clip", 1))!.style.borderTopLeftRadius.value.value, 40);
            host.Render(frame with { Bounds = new Rect(new Offset(0, 0), new Size(200, 20)) });
            Near(Layer(Shared("clip", 1))!.style.borderTopLeftRadius.value.value, 10);
        }
        finally { host.Render(original); }
    }
}
