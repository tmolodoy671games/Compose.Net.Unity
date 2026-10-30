using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Compose.Net;
using NUnit.Framework;
using UnityEngine.UIElements;
using static Compose.Net.ComposeFunctions;

public partial class UnityModifierChainKaspressoTests
{
    private readonly IMutableState<bool> _native = MutableStateOf(false);
    private readonly IMutableState<int> _value = MutableStateOf(0);
    private readonly List<string> _events = new();

    [Test]
    public async Task CoreModifierSurvivesNativeValueUpdatesAndSwitchesLifecycleCorrectly()
    {
        _native.Value = false;
        _value.Value = 0;
        _events.Clear();
        using (var driver = new UnityComposeTestDriver())
        {
            driver.SetContent(Content);
            _native.Value = true;
            await driver.WaitForIdleAsync(CancellationToken.None);
            _value.Value = 1;
            await driver.WaitForIdleAsync(CancellationToken.None);
            Assert.That(_events, Is.EqualTo(new[] { "attach", "detach", "attach", "update" }));
            _native.Value = false;
            await driver.WaitForIdleAsync(CancellationToken.None);
            Assert.That(_events, Is.EqualTo(new[] { "attach", "detach", "attach", "update", "detach", "attach" }));
            Assert.That(driver.Root.Query().Where(e => e.GetType().Name == "Wrapper").ToList(), Is.Empty);
        }
        Assert.That(_events[_events.Count - 1], Is.EqualTo("detach"));
    }

    [Composable]
    private void Content()
    {
        var prefix = Remember(() => Modifier.Size(80.Dp()).Background(Color.RedColor).Offset(10.Dp()));
        var probe = new CoreProbe(_value.Value, _events);
        Spacer(_native.Value ? prefix.Then(probe).TestTag("item") : probe);
    }

    private record CoreProbe : ModifierNodeElement<CoreProbe.ProbeNode>
    {
        private readonly int _value;
        private readonly List<string> _events;
        public CoreProbe(int value, List<string> events) { _value = value; _events = events; }
        public override ProbeNode Create() => new(_events);
        public override void Update(ProbeNode node) => node.Updated();
        public sealed class ProbeNode(List<string> events) : IModifier.Node
        {
            public override void OnAttach() => events.Add("attach");
            public void Updated() => events.Add("update");
            public override void OnDetach() => events.Add("detach");
        }
    }
}
