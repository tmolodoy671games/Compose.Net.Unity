using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Compose.Net;
using NUnit.Framework;
using UnityEngine.UIElements;
using static Compose.Net.ComposeFunctions;

public partial class ComposeUpdatePerformanceKaspressoTests
{
    [Test]
    public void PendingEffectCanReturnToCommittedKeyBeforeApply()
    {
        using var composer = IComposer.Create();
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        var slotType = typeof(IComposer).Assembly.GetType("Compose.Net.Internals.Entities.EffectSlot`1", true).MakeGenericType(typeof(int));
        var slot = Activator.CreateInstance(slotType, flags, null, new object[] { composer }, null);
        var shouldRecord = composer.GetType().GetMethod("ShouldRecordEffect", flags).MakeGenericMethod(typeof(int));
        var record = composer.GetType().GetMethod("RecordEffect", flags);
        var apply = slotType.GetMethod("Apply", flags);
        var prepare = slotType.GetMethod("Prepare", flags);
        var events = new List<string>();
        void Queue(int key)
        {
            if (!(bool)shouldRecord.Invoke(composer, new[] { slot, (object)key })) return;
            Func<Action> setup = () =>
            {
                events.Add($"setup:{key}");
                return () => events.Add($"dispose:{key}");
            };
            Action onApply = () => apply.Invoke(slot, new object[] { key, setup });
            Action onPrepare = () => prepare.Invoke(slot, new object[] { key });
            record.Invoke(composer, new object[] { onApply, onPrepare });
        }
        Queue(0);
        composer.ApplySideEffects();
        events.Clear();
        Queue(0);
        composer.ApplySideEffects();
        Assert.That(events, Is.Empty);
        Queue(1);
        Queue(0);
        composer.ApplySideEffects();
        Assert.That(events, Is.EqualTo(new[] { "dispose:0", "setup:1", "dispose:1", "setup:0" }));
        events.Clear();
        Queue(2);
        composer.AbandonSideEffects();
        Queue(0);
        composer.ApplySideEffects();
        Assert.That(events, Is.Empty);
        Queue(2);
        composer.ApplySideEffects();
        Assert.That(events, Is.EqualTo(new[] { "dispose:0", "setup:2" }));
    }

    [Test]
    public async Task KeyedNodesKeepOrderAndIdentityAcrossStructuralChanges()
    {
        var harness = new KeyedHarness();
        using var driver = new UnityComposeTestDriver();
        driver.SetContent(harness.Content);
        async Task Check()
        {
            await driver.WaitForIdleAsync(CancellationToken.None);
            var expected = harness.Items.Value.SelectMany(id => Enumerable.Range(0, harness.Counts[id].Value)
                .Select(index => $"item-{id}-{index}-{harness.Revision.Value}")).ToArray();
            var actual = driver.Root.Query<VisualElement>().ToList()
                .Where(node => node.name?.StartsWith("item-") == true).Select(node => node.name).ToArray();
            Assert.That(actual, Is.EqualTo(expected));
        }
        await Check();
        for (var cycle = 0; cycle < 8; cycle++)
        {
            harness.Items.Value = Enumerable.Range(cycle * 8, 8).ToArray();
            await Check();
            harness.Revision.Value++;
            await Check();
            var before = driver.Root.Query<VisualElement>().ToList()
                .Where(node => node.name?.StartsWith("item-") == true).ToDictionary(node => node.name);
            harness.Items.Value = harness.Items.Value.AsEnumerable().Reverse().ToArray();
            await Check();
            foreach (var entry in before) Assert.That(driver.Root.Q(entry.Key), Is.SameAs(entry.Value));
            foreach (var id in harness.Items.Value)
            {
                harness.Counts[id].Value = 0;
                await Check();
                harness.Counts[id].Value = 2;
                await Check();
            }
            harness.Revision.Value++;
            await Check();
            foreach (var id in harness.Items.Value) harness.Counts[id].Value = 1;
            await Check();
            while (harness.Items.Value.Length > 0)
            {
                harness.Items.Value = harness.Items.Value.Where((_, index) => index != harness.Items.Value.Length / 2).ToArray();
                await Check();
                harness.Revision.Value++;
                await Check();
            }
        }
    }

    [Test]
    public async Task UnchangedEffectsKeepResourcesAndChangedKeysReplaceThem()
    {
        var harness = new EffectsHarness();
        using (var driver = new UnityComposeTestDriver())
        {
            driver.SetContent(harness.Content);
            Assert.That(harness.Events, Is.EqualTo(new[] { "setup:0" }));
            harness.Events.Clear();
            for (var i = 0; i < 20; i++)
            {
                harness.Revision.Value++;
                await driver.WaitForIdleAsync(CancellationToken.None);
            }
            Assert.That(harness.Events, Is.Empty);
            harness.Key.Value = 1;
            await driver.WaitForIdleAsync(CancellationToken.None);
            Assert.That(harness.Events, Is.EqualTo(new[] { "dispose:0", "setup:1" }));
            harness.Events.Clear();
            harness.Key.Value = 0;
            await driver.WaitForIdleAsync(CancellationToken.None);
            Assert.That(harness.Events, Is.EqualTo(new[] { "dispose:1", "setup:0" }));
            harness.Events.Clear();
        }
        Assert.That(harness.Events, Is.EqualTo(new[] { "dispose:0" }));
    }

    // Measures actual Unity modifier nodes and UI Toolkit layout, without a hardware-specific FPS threshold.
    [TestCase(500)]
    [TestCase(1000)]
    [TestCase(2000)]
    [Category("Performance")]
    public async Task MovingNodesUpdateWithoutRestartingEffects(int count)
    {
        var harness = new MovingHarness(count);
        using var driver = new UnityComposeTestDriver();
        driver.SetContent(harness.Content);
        async Task Frame(int value)
        {
            foreach (var position in harness.Positions) position.Value = value;
            await driver.WaitForIdleAsync(CancellationToken.None);
        }
        for (var frame = 1; frame <= 30; frame++) await Frame(frame);
        var samples = new List<double>();
        var allocations = new List<long>();
        for (var sample = 0; sample < 5; sample++)
        {
            var allocated = GC.GetAllocatedBytesForCurrentThread();
            var timer = Stopwatch.StartNew();
            for (var frame = 1; frame <= 100; frame++) await Frame(frame);
            timer.Stop();
            allocations.Add((GC.GetAllocatedBytesForCurrentThread() - allocated) / 100);
            samples.Add(timer.Elapsed.TotalMilliseconds / 100);
        }
        samples.Sort();
        allocations.Sort();
        Assert.That(harness.Setups, Is.EqualTo(count));
        var nodes = driver.Root.Query<VisualElement>().ToList().Where(node => node.name?.StartsWith("moving-") == true).ToArray();
        Assert.That(nodes.Length, Is.EqualTo(count));
        foreach (var node in nodes) Assert.That(node.style.translate.value.x.value, Is.EqualTo(100).Within(.01f));
        TestContext.WriteLine($"Unity update {count}: {samples[2]:F4} ms/frame, {allocations[2]} bytes/frame, setups={harness.Setups}");
    }

    private sealed partial class KeyedHarness
    {
        internal readonly IMutableState<int[]> Items = MutableStateOf(Enumerable.Range(0, 8).ToArray());
        internal readonly IMutableState<int> Revision = MutableStateOf(0);
        internal readonly IMutableState<int>[] Counts = Enumerable.Range(0, 64).Select(_ => MutableStateOf(1)).ToArray();
        [Composable]
        internal void Content() => Box(content: Children);
        [Composable, BoxScope]
        private void Children()
        {
            var revision = Revision.Value;
            foreach (var id in Items.Value) Key(id, () => Child(id, revision));
        }
        [Composable]
        private void Child(int id, int revision)
        {
            for (var index = 0; index < Counts[id].Value; index++)
                Spacer(Modifier.Size(10.Dp()).TestTag($"item-{id}-{index}-{revision}"));
        }
    }

    private sealed partial class EffectsHarness
    {
        internal readonly IMutableState<int> Key = MutableStateOf(0);
        internal readonly IMutableState<int> Revision = MutableStateOf(0);
        internal readonly List<string> Events = new();
        [Composable]
        internal void Content()
        {
            var revision = Revision.Value;
            var key = Key.Value;
            DisposableEffect(key, () =>
            {
                Events.Add($"setup:{key}");
                return OnDispose(() => Events.Add($"dispose:{key}"));
            });
            Spacer(Modifier.Size((10 + revision).Dp()));
        }
    }

    private sealed partial class MovingHarness
    {
        internal readonly IMutableState<int>[] Positions;
        internal int Setups;
        internal MovingHarness(int count) => Positions = Enumerable.Range(0, count).Select(_ => MutableStateOf(0)).ToArray();
        [Composable]
        internal void Content() => Box(content: Children, modifier: Modifier.FillMaxSize());
        [Composable, BoxScope]
        private void Children()
        {
            for (var i = 0; i < Positions.Length; i++)
            {
                var index = i;
                Key(index, () => Item(index));
            }
        }
        [Composable]
        private void Item(int index)
        {
            DisposableEffect(1, () =>
            {
                Setups++;
                return OnDispose(() => { });
            });
            var prefix = Remember(index, () => Modifier.Size(50.Dp()).Background(Color.RedColor).Float().TestTag($"moving-{index}"));
            Spacer(prefix.Offset(Positions[index].Value.Dp(), 0.Dp()));
        }
    }
}
