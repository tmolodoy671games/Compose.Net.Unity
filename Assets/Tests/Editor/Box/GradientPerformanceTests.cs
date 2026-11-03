using System;
using Compose.Net;
using NUnit.Framework;
using StableCollections;
using static Compose.Net.ComposeFunctions;
using Color = Compose.Net.Color;

namespace Compose.Net.Unity.Tests;

public sealed partial class BoxTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void GradientRepaintPerformance(bool radial)
    {
        const int draws = 64;
        var colors = new[] { new Color(1, 0, 0), new Color(0, 0, 1) }.ToPersistentStableList();
        var brush = radial ? Brush.RadialGradient(colors, new Offset(30, 20), 25, TileMode.Clamp)
            : Brush.LinearGradient(colors, new Offset(0, 0), new Offset(60, 40));
        var frames = 0; long bytes = 0, ticks = 0;
        _emptyOverload = true;
        _box = Modifier.Size(160.Dp(), 100.Dp()).Draw(d =>
        {
            var allocated = GC.GetAllocatedBytesForCurrentThread();
            var start = System.Diagnostics.Stopwatch.GetTimestamp();
            for (var i = 0; i < draws; i++)
                d.DrawRoundRect(brush, new Offset(i % 8, i / 8), new Size(60, 40), 8, alpha: .65f);
            var elapsed = System.Diagnostics.Stopwatch.GetTimestamp() - start;
            var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
            if (frames++ >= 10) { bytes += allocatedBytes; ticks += elapsed; }
        });
        Mount();
        for (var i = 0; i < 100; i++) PaintGradientFrame();
        var measuredDraws = (frames - 10) * draws;
        Assert.That(measuredDraws, Is.GreaterThan(0));
        var message = $"radial={radial} draws={measuredDraws} bytesPerDraw={(double)bytes/measuredDraws:F3} nsPerDraw={(double)ticks/System.Diagnostics.Stopwatch.Frequency*1e9/measuredDraws:F1}";
        TestContext.WriteLine(message);
        Assert.That((double)bytes / measuredDraws, Is.LessThan(8), "Unchanged gradient draws should reuse their managed data.");
    }
}
