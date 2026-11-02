using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Compose.Net.Kaspresso.Unity.Tests
{
    [AiGenerated]
    public sealed class KaspressoUnityTests : UITestFixture
    {
        private Kaspresso _test;
        [SetUp]
        public void SetUpKaspresso()
        {
            panelSize = new Vector2(600, 400);
            _test = KaspressoUnity.Create(simulate, () => rootVisualElement);
        }
        [Test]
        public void ClickDispatchesInputAndSelectorSurvivesReplacement()
        {
            var clicks = 0;
            var button = new Button(() => clicks++) { name = "button", text = "Before" };
            rootVisualElement.Add(button);
            var selector = _test.OnNodeWithTag("button");
            selector.AssertText("Before").Click();
            Assert.That(clicks, Is.EqualTo(1));
            button.RemoveFromHierarchy();
            rootVisualElement.Add(new Button { name = "button", text = "After" });
            selector.AssertText("After");
        }
        [Test]
        public void HiddenAndDisabledAncestorsPreventInteraction()
        {
            var parent = new VisualElement();
            parent.Add(new Button { name = "button", text = "Click" });
            rootVisualElement.Add(parent);
            parent.style.display = DisplayStyle.None;
            _test.OnNodeWithTag("button").AssertIsNotDisplayed();
            Assert.Throws<KaspressoAssertionException>(() => _test.OnNodeWithTag("button").Click());
            parent.style.display = DisplayStyle.Flex;
            parent.SetEnabled(false);
            _test.OnNodeWithTag("button").AssertIsDisplayed().AssertIsDisabled();
            Assert.Throws<KaspressoAssertionException>(() => _test.OnNodeWithTag("button").Click());
        }
        [Test]
        public void DuplicateTagsRequireScope()
        {
            var parent = new VisualElement { name = "parent" };
            parent.Add(new Label("Inside") { name = "label" });
            rootVisualElement.Add(parent);
            rootVisualElement.Add(new Label("Outside") { name = "label" });
            _test.OnNodeWithTag("label").AssertCount(2);
            Assert.Throws<KaspressoAssertionException>(() => _test.OnNodeWithTag("label").AssertExists());
            _test.OnNodeWithTag("parent").DescendantWithTag("label").AssertText("Inside");
        }
        [Test]
        public void ReplaceTextUsesKeyboardAndEmitsChanges()
        {
            var changes = 0;
            var field = new TextField { name = "input", value = "old" };
            field.RegisterValueChangedCallback(_ => changes++);
            rootVisualElement.Add(field);
            _test.OnNodeWithTag("input").ReplaceText("new").AssertText("new");
            Assert.That(field.value, Is.EqualTo("new"));
            Assert.That(changes, Is.GreaterThan(0));
            _test.OnNodeWithTag("input").ReplaceText("").AssertText("");
        }
        [Test]
        public void ScrollToRevealsClippedElement()
        {
            var scroll = new ScrollView();
            scroll.style.height = 100;
            var spacer = new VisualElement();
            spacer.style.height = 500;
            spacer.style.flexShrink = 0;
            scroll.Add(spacer);
            scroll.Add(new Button { name = "bottom", text = "Bottom" });
            rootVisualElement.Add(scroll);
            _test.OnNodeWithTag("bottom").AssertIsNotDisplayed().ScrollTo().AssertIsDisplayed();
        }
        [UnityTest]
        public IEnumerator WaitUntilObservesLaterFrames()
        {
            var wait = _test.WaitUntil(() => _test.OnNodeWithTag("later").AssertText("Ready"));
            Assert.That(wait.MoveNext(), Is.True);
            yield return null;
            rootVisualElement.Add(new Label("Ready") { name = "later" });
            Assert.That(wait.MoveNext(), Is.False);
        }
        [Test]
        public void ZeroTimeoutKeepsSelectorDiagnostic()
        {
            var error = Assert.Throws<KaspressoAssertionException>(() =>
                _test.WaitUntil(() => _test.OnNodeWithTag("missing").AssertExists(), TimeSpan.Zero).MoveNext());
            StringAssert.Contains("missing", error.Message);
        }
    }
}
