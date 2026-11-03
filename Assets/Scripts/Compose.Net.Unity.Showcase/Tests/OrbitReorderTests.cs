using System.Collections;
using System.Linq;
using Compose.Net.Showcase;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Showcase.Tests;

public sealed partial class OrbitScreenTests
{
    [UnityTest] public IEnumerator ProjectOrderingPreservesStarsProgressAndClickableCards()
    {
        panelSize = new Vector2(2650, 900); yield return Settle(); Click("nav.Projects"); yield return Settle();
        var first = _state.Projects[0]; var original = _state.Projects.ToArray();
        var card = Find("project.card." + first.Id); var progress = first.Completed;
        Click("project.star." + first.Id); yield return Settle(); var starred = first.Starred.Value;
        Click("projects.rotate"); yield return Settle();
        Assert.That(_state.Projects[0], Is.SameAs(original[1])); Assert.That(_state.Projects[5], Is.SameAs(first));
        Assert.That(Find("project.card." + first.Id), Is.SameAs(card), "Moving a visible keyed card must retain its composition");
        for (var round = 0; round < 5; round++)
        {
            Click("projects.shuffle"); yield return Settle();
            Assert.That(_state.Projects.OrderBy(p => p.Id), Is.EqualTo(original.OrderBy(p => p.Id)));
            Assert.That(first.Starred.Value, Is.EqualTo(starred)); Assert.That(first.Completed, Is.EqualTo(progress));
            var rendered = _state.Projects.Select(p => Find("project.card." + p.Id)).ToArray();
            for (var i = 1; i < rendered.Length; i++)
                Assert.That(rendered[i].worldBound.xMin, Is.GreaterThan(rendered[i - 1].worldBound.xMax), "Card positions must follow the new order");
        }
        Click("projects.reset"); yield return Settle(); Assert.That(_state.Projects.Select(p => p.Id), Is.EqualTo(original.Select(p => p.Id)));
        Click("project.card." + first.Id); yield return Settle(); Assert.That(_state.Route.ProjectId, Is.EqualTo(first.Id));
        Click("project.back"); yield return Settle(); Assert.That(first.Starred.Value, Is.EqualTo(starred));
        Click("filter.Engineering"); yield return Settle(); var filtered = _state.VisibleProjects();
        Click("projects.rotate"); yield return Settle();
        Assert.That(_state.VisibleProjects()[0], Is.SameAs(filtered[1]), "Reordering must act on the filtered cards the user sees");
        Assert.That(Find("project.card." + filtered[1].Id).worldBound.xMin, Is.LessThan(Find("project.card." + filtered[0].Id).worldBound.xMin));
    }

    [UnityTest] public IEnumerator ActivityOrderingRebindsRowsWithoutDuplicatesOrOverlap()
    {
        yield return Settle(); Click("nav.Activity"); yield return Settle();
        var original = _state.Events.ToArray(); Click("activity.rotate"); yield return Settle();
        Assert.That(_state.Events[0], Is.SameAs(original[1]));
        foreach (var action in new[] { "shuffle", "shuffle", "reset" })
        {
            Click("activity." + action); yield return Settle();
            Assert.That(_state.Events.OrderBy(e => e.Id), Is.EqualTo(original.OrderBy(e => e.Id)));
            var list = Find("activity.list");
            var first = list.Q("event." + _state.Events[0].Id); var second = list.Q("event." + _state.Events[1].Id);
            Assert.That(first, Is.Not.Null); Assert.That(second, Is.Not.Null);
            Assert.That(second.worldBound.yMin, Is.GreaterThanOrEqualTo(first.worldBound.yMax - 1));
            foreach (var item in _state.Events)
                Assert.That(list.Query<VisualElement>("event." + item.Id).ToList().Count, Is.LessThanOrEqualTo(1), "Duplicate keyed row " + item.Id);
        }
        Assert.That(_state.Events.Select(e => e.Id), Is.EqualTo(original.Select(e => e.Id)));
    }

    [UnityTest] public IEnumerator ChaosCanStartStopAndCancelsWhenLeavingEitherCollection()
    {
        yield return Settle();
        foreach (var page in new[] { OrbitPage.Projects, OrbitPage.Activity })
        {
            Click("nav." + page); yield return Settle(); var tag = page == OrbitPage.Projects ? "projects" : "activity";
            int[] Order() => page == OrbitPage.Projects ? _state.Projects.Select(p => p.Id).ToArray() : _state.Events.Select(e => e.Id).ToArray();
            var original = Order(); Click(tag + ".chaos");
            var until = EditorApplication.timeSinceStartup + 1.2;
            while (EditorApplication.timeSinceStartup < until) { yield return null; Frame(); }
            Assert.That(Order(), Is.Not.EqualTo(original), "Chaos must reorder " + page);
            Click(tag + ".chaos"); yield return Settle(); var stopped = Order();
            until = EditorApplication.timeSinceStartup + 1.1;
            while (EditorApplication.timeSinceStartup < until) { yield return null; Frame(); }
            Assert.That(Order(), Is.EqualTo(stopped), "Stop must cancel pending reorder work");
            Click(tag + ".chaos"); Click("nav.Settings"); yield return Settle();
            Assert.That(_state.ProjectChaos.Value || _state.ActivityChaos.Value, Is.False);
            stopped = Order(); until = EditorApplication.timeSinceStartup + 1.1;
            while (EditorApplication.timeSinceStartup < until) { yield return null; Frame(); }
            Assert.That(Order(), Is.EqualTo(stopped), "An outgoing screen must stop sorting");
        }
    }
}
