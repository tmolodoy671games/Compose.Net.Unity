using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Compose.Net;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using PointerEvent = Compose.Net.PointerEvent;

public sealed class PointerInputKaspressoTests
{
    private static IPointerInputScope Input(VisualElement element, CancellationToken token = default)
    {
        var assembly = typeof(Compose.Net.Unity.Packages.Compose.Net.Unity.Runtime.Adapters.ComposeUnityAdapter).Assembly;
        return (IPointerInputScope)Activator.CreateInstance(assembly.GetType("Compose.Net.Unity.PointerInputScopeImpl", true), element, token);
    }

    private static void Down(VisualElement target, int id = 0)
    {
        using var evt = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = new Vector2(20, 30) });
        typeof(PointerDownEvent).GetProperty("pointerId").SetValue(evt, id);
        evt.target = target;
        target.SendEvent(evt);
    }

    [Test]
    public async Task PassesFollowTreeOrderAndShareConsumption()
    {
        using var driver = new UnityComposeTestDriver();
        var parent = new VisualElement();
        var child = new VisualElement();
        parent.Add(child);
        var seen = new List<string>();
        async Task Observe(IAwaitPointerEventScope scope, string name)
        {
            var initial = await scope.AwaitPointerEvent(PointerEventPass.Initial);
            seen.Add(name + ":initial");
            var main = await scope.AwaitPointerEvent();
            seen.Add(name + ":main");
            Assert.That(main, Is.SameAs(initial));
            if (name == "child") main.Changes[0].Consume();
            else Assert.That(main.Changes[0].IsConsumed, Is.True);
            var final = await scope.AwaitPointerEvent(PointerEventPass.Final);
            seen.Add(name + ":final");
            Assert.That(final.Changes[0].IsConsumed, Is.True);
            Assert.That(scope.CurrentEvent, Is.SameAs(final));
        }
        var first = Input(parent).AwaitPointerEventScope(s => Observe(s, "parent"));
        var second = Input(child).AwaitPointerEventScope(s => Observe(s, "child"));
        driver.Root.Add(parent);
        Down(child);
        await Task.WhenAll(first, second);
        Assert.That(seen, Is.EqualTo(new[] { "parent:initial", "child:initial", "child:main", "parent:main", "parent:final", "child:final" }));
    }

    [Test]
    public async Task EventContainsAllPressedPointersWithoutRepeatingDownTransitions()
    {
        using var driver = new UnityComposeTestDriver();
        var element = new VisualElement(); driver.Root.Add(element);
        using var source = new CancellationTokenSource();
        PointerEvent first = null, second = null;
        var task = Input(element, source.Token).AwaitPointerEventScope(async scope =>
        {
            first = await scope.AwaitPointerEvent();
            second = await scope.AwaitPointerEvent();
        });
        Down(element, 1); Down(element, 2);
        await task;
        Assert.That(first.Changes.Count, Is.EqualTo(1));
        Assert.That(second.Changes.Count, Is.EqualTo(2));
        Assert.That(second.Changes.Single(c => c.Id.Value == 1).ChangedToDownIgnoreConsumed(), Is.False);
        Assert.That(second.Changes.Single(c => c.Id.Value == 2).ChangedToDownIgnoreConsumed(), Is.True);
        Assert.That(second.Changes.All(c => c.ScrollDelta == Offset.Zero), Is.True);
    }

    [Test]
    public async Task CompletedScopeUnregistersAndCancellationEndsPendingWait()
    {
        using var driver = new UnityComposeTestDriver();
        var input = Input(driver.Root);
        Assert.That(await input.AwaitPointerEventScope(_ => Task.FromResult(42)), Is.EqualTo(42));
        var next = input.AwaitPointerEventScope(scope => scope.AwaitPointerEvent());
        Down(driver.Root);
        Assert.That((await next).Type, Is.EqualTo(PointerEventType.Press));
        using var source = new CancellationTokenSource();
        var pending = Input(driver.Root, source.Token).AwaitPointerEventScope(scope => scope.AwaitPointerEvent());
        source.Cancel();
        Assert.CatchAsync<OperationCanceledException>(async () => await pending);
    }
}
