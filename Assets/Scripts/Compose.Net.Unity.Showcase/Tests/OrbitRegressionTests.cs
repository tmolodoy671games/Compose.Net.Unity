using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Showcase.Tests;

public sealed partial class OrbitScreenTests
{
    private void Frame(int milliseconds = 20)
    {
        _adapter.Clock.Advance(milliseconds); _context.Drain(); _adapter.Invalidator.Tick(); simulate.FrameUpdateMs(milliseconds);
    }
    private TextElement VisibleText(string text)
    {
        var matches = rootVisualElement.Query<TextElement>().ToList().Where(t => t.text == text).ToArray();
        var visible = matches.FirstOrDefault(t =>
        {
            if (t.worldBound.width <= 0 || t.worldBound.height <= 0) return false;
            var opacity = t.resolvedStyle.color.a;
            for (VisualElement e = t; e != null; e = e.parent)
            {
                opacity *= e.resolvedStyle.opacity;
                if (e.resolvedStyle.visibility == Visibility.Hidden || e.resolvedStyle.display == DisplayStyle.None) return false;
            }
            return opacity > .8f && t.worldBound.Overlaps(rootVisualElement.worldBound);
        });
        Assert.That(visible, Is.Not.Null, "Text must be visible: " + text + "; matches=" + matches.Length);
        return visible!;
    }
    private void Type(string tag, string text)
    {
        var field = Find(tag).Q<TextField>();
        simulate.Click(field); simulate.KeyPress(KeyCode.A, EventModifiers.Control | EventModifiers.Command);
        simulate.TypingText(text); Flush();
        Assert.That(field.value, Is.EqualTo(text), "Keyboard input: " + tag);
    }
    [UnityTest] public IEnumerator RapidNavigationKeepsHeadingsAndControlsVisible()
    {
        yield return Settle();
        var pages = new[] { "Projects", "Activity", "Focus", "Motion", "Settings", "Overview" };
        var titles = new[] { "Your projects", "Good things are happening.", "Find a little breathing room.", "A little beautifully unnecessary.", "Make yourself at home.", "A little focus. Infinite possibility." };
        for (var pass = 0; pass < 3; pass++)
            for (var i = 0; i < pages.Length; i++)
            {
                simulate.Click(Find("nav." + pages[i])); Frame(40);
                simulate.Click(Find("nav." + pages[(i + 1) % pages.Length])); Frame(40);
                simulate.Click(Find("nav." + pages[i]));
                // Include both the staggered entrance and the subsequent shared
                // bounds transition before checking disposal of outgoing screens.
                for (var frame = 0; frame < 90; frame++) { yield return null; Frame(); }
                VisibleText(titles[i]);
                Assert.That(rootVisualElement.Query<VisualElement>().ToList().Count(e => e.name?.StartsWith("screen.") == true), Is.EqualTo(1), "Outgoing screens must be disposed: " + string.Join(", ", rootVisualElement.Query<VisualElement>().ToList().Where(e => e.name?.StartsWith("screen.") == true).Select(e => e.name)));
            }
    }
    [UnityTest] public IEnumerator ResizingDoesNotLeaveStaleScreenOrContentWidths()
    {
        yield return Settle();
        foreach (var size in new[] { new Vector2(3840,2160), new Vector2(720,560), new Vector2(1280,720), new Vector2(760,900) })
        {
            panelSize = size; yield return Settle();
            foreach (var page in new[] { "Overview", "Projects", "Focus", "Settings", "Motion" })
            {
                Click("nav." + page); yield return Settle();
                var screen = Find("screen." + page);
                Assert.That(screen.worldBound.xMax, Is.EqualTo(size.x).Within(2), page + " right edge; state=" + _state.Width.Value + ", root=" + Find("orbit.root").worldBound);
                Assert.That(screen.worldBound.yMax, Is.EqualTo(size.y).Within(2), page + " bottom edge");
                var content = screen.Q<ScrollView>()?.contentContainer ?? screen.Children().First();
                Assert.That(content.worldBound.width, Is.GreaterThan(screen.worldBound.width - 100), page + " content must fill the screen");
                Assert.That(Find("nav.Settings").worldBound.yMax, Is.LessThan(size.y), "Settings navigation must remain on screen");
                CapturePreview(page + "-" + size.x + "x" + size.y);
            }
        }
    }
    [UnityTest] public IEnumerator ModalAcceptsKeyboardAndCategoryClicksWithoutClosing()
    {
        yield return Settle(); Click("project.new"); yield return Settle();
        Type("project.name", "Keyboard project");
        Assert.That(_state.CreateOpen.Value, Is.True);
        Click("category.1"); yield return Settle();
        Assert.That(_state.CreateOpen.Value, Is.True); Assert.That(_state.DraftCategory.Value, Is.EqualTo(1));
        Click("project.create"); yield return Settle();
        VisibleText("Keyboard project");
        Assert.That(_state.Projects[_state.Projects.Size - 1].Category, Is.EqualTo("Engineering"));
    }
    [UnityTest] public IEnumerator ScrimClosesDialogAndDoesNotClickThroughToNavigation()
    {
        yield return Settle(); Click("project.new"); yield return Settle();
        simulate.Click(Find("nav.Focus").worldBound.center); yield return Settle();
        Assert.That(_state.CreateOpen.Value, Is.False); Assert.That(_state.Route.Page.ToString(), Is.EqualTo("Overview"));
        Click("nav.Focus"); yield return Settle(); VisibleText("Find a little breathing room.");
    }
    [UnityTest] public IEnumerator CardStarDoesNotOpenDetailAndFilteringUsesKeyboard()
    {
        yield return Settle(); Click("nav.Projects"); yield return Settle();
        Click("project.star.2"); yield return Settle();
        Assert.That(_state.Route.Page.ToString(), Is.EqualTo("Projects")); Assert.That(_state.Projects[1].Starred.Value, Is.True);
        Click("filter.Starred"); yield return Settle(); Type("projects.search", "parallel"); yield return Settle();
        Assert.That(_state.VisibleProjects().Select(p => p.Id), Is.EqualTo(new[] { 2 }));
        VisibleText("Parallel worlds");
        Click("project.star.2"); yield return Settle(); VisibleText("A little quiet here.");
        Click("projects.clear"); yield return Settle(); Assert.That(_state.VisibleProjects().Length, Is.EqualTo(6));
    }
    [UnityTest] public IEnumerator DoubleClickingProjectDoesNotDuplicateBackStack()
    {
        yield return Settle(); Click("nav.Projects"); yield return Settle();
        simulate.DoubleClick(Find("project.card.1")); yield return Settle();
        Assert.That(_state.Navigation.Size, Is.EqualTo(2));
        Click("project.back"); yield return Settle(); Assert.That(_state.Route.Page.ToString(), Is.EqualTo("Projects"));
    }
    [UnityTest] public IEnumerator OpeningDifferentProjectStartsAtTop()
    {
        yield return Settle(); Click("nav.Projects"); yield return Settle(); Click("project.card.1"); yield return Settle();
        for (var i = 0; i < 10; i++) { simulate.ScrollWheel(new Vector2(0,10), Find("screen.Project").worldBound.center); Flush(); }
        Assert.That(_state.DetailsScroll.Value, Is.GreaterThan(100));
        Click("nav.Projects"); yield return Settle(); Click("project.card.2"); yield return Settle();
        Assert.That(_state.DetailsScroll.Value, Is.EqualTo(0)); VisibleText("Parallel worlds");
    }
    [UnityTest] public IEnumerator InvalidNamesCannotSubmitAndCompletionPreferenceHidesDoneTasks()
    {
        yield return Settle(); Click("project.new"); yield return Settle();
        Type("project.name", "   "); simulate.Click(Find("project.create")); yield return Settle();
        Assert.That(_state.CreateOpen.Value, Is.True); Assert.That(_state.Projects.Size, Is.EqualTo(6));
        Type("project.name", new string('x',49)); simulate.Click(Find("project.create")); yield return Settle();
        Assert.That(_state.CreateOpen.Value, Is.True); Assert.That(_state.Projects.Size, Is.EqualTo(6));
        Click("project.cancel"); yield return Settle(); Click("nav.Settings"); yield return Settle();
        Click("settings.completed"); yield return Settle(); Click("nav.Projects"); yield return Settle(); Click("project.card.1"); yield return Settle();
        Assert.That(rootVisualElement.Q("task.0"), Is.Null); Click("task.3"); yield return Settle();
        Assert.That(rootVisualElement.Q("task.3"), Is.Null); Assert.That(rootVisualElement.Q("task.4"), Is.Not.Null);
    }
    [UnityTest] public IEnumerator CarouselDotsAndActionsRemainReachable()
    {
        yield return Settle();
        Click("hero.page.1"); yield return Settle();
        VisibleText("Good ideas deserve\na little breathing room.");
        Click("hero.action.1"); yield return Settle(); Assert.That(_state.Route.Page.ToString(), Is.EqualTo("Focus"));
        Click("nav.Overview"); yield return Settle(); Click("hero.page.2"); yield return Settle();
        Click("hero.action.2"); yield return Settle(); Assert.That(_state.Route.Page.ToString(), Is.EqualTo("Motion"));
    }
    [UnityTest] public IEnumerator WheelScrollReachesOverviewFooterAndReturns()
    {
        yield return Settle();
        var screen = Find("screen.Overview");
        for (var i = 0; i < 18; i++) { simulate.ScrollWheel(new Vector2(0, 10), screen.worldBound.center); Flush(); }
        CapturePreview("scroll-footer");
        Assert.That(_state.OverviewScroll.Value, Is.GreaterThan(100), "Wheel should scroll: " + _state.OverviewScroll.Value);
        VisibleText("Made of small ideas, shared energy, and a lot of motion.");
        Assert.That(_state.OverviewScroll.Value, Is.GreaterThan(0));
        for (var i = 0; i < 18; i++) { simulate.ScrollWheel(new Vector2(0, -10), screen.worldBound.center); Flush(); }
        VisibleText("A little focus. Infinite possibility.");
    }
    [UnityTest] public IEnumerator DragParticlesResetsOnReleaseAndNavigationStillWorks()
    {
        yield return Settle(); Click("nav.Motion"); yield return Settle();
        var center = Find("motion.canvas").worldBound.center;
        simulate.MouseDown(center); simulate.MouseMove(center + new Vector2(80,40)); Flush();
        Assert.That(_state.PlaygroundOffset.Value.X, Is.GreaterThan(0));
        simulate.MouseUp(center + new Vector2(80,40)); yield return Settle();
        Assert.That(_state.PlaygroundOffset.Value.X, Is.EqualTo(0));
        Click("nav.Projects"); yield return Settle(); VisibleText("Your projects");
    }
    [UnityTest] public IEnumerator MinimalWindowKeepsModalButtonsAndSettingsReachable()
    {
        panelSize = new Vector2(720,560); yield return Settle();
        Click("nav.Settings"); yield return Settle(); Click("settings.reduced"); yield return Settle();
        Click("project.new"); yield return Settle();
        Type("project.name", "Small window"); Click("project.create"); yield return Settle();
        Click("task.4"); yield return Settle();
        Assert.That(_state.Projects.Last().Tasks[4].Value, Is.True);
    }
    [UnityTest] public IEnumerator ThemeAnimatesBothDirectionsAndSurvivesNavigationAndModal()
    {
        yield return Settle();
        var dark = Find("orbit.root").resolvedStyle.backgroundColor;
        Click("theme.toggle");
        var middle = dark;
        for (var i = 0; i < 30; i++)
        {
            yield return null; Frame();
            if (i == 9) middle = Find("orbit.root").resolvedStyle.backgroundColor;
            var heading = VisibleText("A little focus. Infinite possibility.");
            float Luminance(UnityEngine.Color color)
            {
                float Linear(float c) => c <= .04045f ? c / 12.92f : (float)Math.Pow((c + .055f) / 1.055f, 2.4);
                return .2126f * Linear(color.r) + .7152f * Linear(color.g) + .0722f * Linear(color.b);
            }
            var a = Luminance(heading.resolvedStyle.color); var b = Luminance(Find("orbit.root").resolvedStyle.backgroundColor);
            Assert.That((Math.Max(a,b) + .05f) / (Math.Min(a,b) + .05f), Is.GreaterThan(3), "Heading must not disappear into the theme transition");
        }
        Assert.That(middle.r, Is.GreaterThan(dark.r + .01f)); Assert.That(middle.r, Is.LessThan(.95f));
        yield return Settle();
        Assert.That(Find("orbit.root").resolvedStyle.backgroundColor.r, Is.GreaterThan(.9f));
        Click("nav.Projects"); yield return Settle(); VisibleText("Your projects");
        var input = Find("projects.search").Q<TextField>().Q<VisualElement>(className: TextField.inputUssClassName);
        Assert.That(input.resolvedStyle.backgroundColor.r, Is.GreaterThan(.8f));
        Assert.That(input.resolvedStyle.color.r, Is.LessThan(.3f));
        Click("project.new"); yield return Settle(); VisibleText("Make something great.");
        CapturePreview("theme-modal");
        var modalInput = Find("project.name").Q<TextField>().Q<VisualElement>(className: TextField.inputUssClassName);
        Assert.That(modalInput.resolvedStyle.backgroundColor.r, Is.GreaterThan(.8f));
        Click("project.cancel"); yield return Settle(); Click("theme.toggle"); yield return Settle();
        Assert.That(Find("orbit.root").resolvedStyle.backgroundColor.r, Is.EqualTo(dark.r).Within(.01));
        VisibleText("Your projects"); CapturePreview("theme-dark");
        Click("theme.toggle"); yield return Settle(); CapturePreview("theme-light");
    }
    [UnityTest] public IEnumerator ThemeCanReverseMidAnimationAndReducedMotionIsImmediate()
    {
        yield return Settle();
        simulate.Click(Find("theme.toggle")); Frame(80); simulate.Click(Find("theme.toggle")); yield return Settle();
        Assert.That(_state.LightTheme.Value, Is.False);
        Assert.That(Find("orbit.root").resolvedStyle.backgroundColor.r, Is.LessThan(.2f));
        Click("nav.Settings"); yield return Settle(); Click("settings.reduced"); yield return Settle();
        simulate.Click(Find("theme.toggle")); Frame(); Frame();
        Assert.That(Find("orbit.root").resolvedStyle.backgroundColor.r, Is.GreaterThan(.9f));
        VisibleText("Make yourself at home.");
    }
}
