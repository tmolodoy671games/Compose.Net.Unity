using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Compose.Net;
using Compose.Net.Kaspresso;
using Compose.Net.Kaspresso.Unity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;
using static Compose.Net.ComposeFunctions;
using Color = Compose.Net.Color;

namespace Compose.Net.Unity.Tests
{
    [Compose.Net.Kaspresso.AiGenerated, TestFixture(true), TestFixture(false)]
    public sealed class ClickableModifierTests : ClickableFixture
    {
        public ClickableModifierTests(bool shortOverload) : base(shortOverload) { }

        [UnityTest]
        public IEnumerator ClickInvokesOnceOnRelease()
        {
            yield return Mount();
            simulate.MouseDown(Target);
            yield return Wait(() => Assert.That(Presses.Length, Is.EqualTo(1)));
            Assert.That(_clicks.Value, Is.Zero);
            simulate.MouseUp(Target);
            yield return Count(1);
            yield return Wait(() => Assert.That(Releases.Length, Is.EqualTo(1)));
            Assert.That(Releases[0].Press, Is.SameAs(Presses[0]));
            Assert.That(Cancels, Is.Empty);
        }

        [UnityTest]
        public IEnumerator RapidClicksAreNeitherDroppedNorDuplicated()
        {
            yield return Mount();
            for (var i = 0; i < 12; i++) simulate.Click(Target);
            yield return Count(12);
            yield return Wait(() => Assert.That(Releases.Length, Is.EqualTo(12)));
            Assert.That(Presses.Length, Is.EqualTo(12));
            for (var i = 0; i < 12; i++) Assert.That(Releases[i].Press, Is.SameAs(Presses[i]));
            Assert.That(Cancels, Is.Empty);
        }

        [UnityTest]
        public IEnumerator DoubleClickProducesTwoOrdinaryClicks()
        {
            yield return Mount();
            simulate.DoubleClick(Target);
            yield return Count(2);
        }

        [UnityTest]
        public IEnumerator HoldDoesNotRepeat()
        {
            yield return Mount();
            simulate.MouseDown(Target);
            yield return Wait(() => Assert.That(Presses.Length, Is.EqualTo(1)));
            simulate.FrameUpdate(1.0);
            yield return Settle();
            Assert.That(_clicks.Value, Is.Zero);
            Assert.That(Presses.Length, Is.EqualTo(1));
            simulate.MouseUp(Target);
            yield return Count(1);
        }

        [UnityTest]
        public IEnumerator HoverOnlyEmitsPairedEnterAndExit()
        {
            yield return Mount();
            simulate.MouseMove(Target);
            yield return Wait(() => Assert.That(_events.OfType<IHoverInteraction.Enter>().Count(), Is.EqualTo(1)));
            simulate.MouseMove(Target.worldBound.center + new Vector2(3, 3));
            yield return Settle();
            Assert.That(Presses, Is.Empty, "Movement is not a pointer down");
            Assert.That(_clicks.Value, Is.Zero);
            simulate.MouseMove(new Vector2(590, 390));
            yield return Wait(() => Assert.That(_events.OfType<IHoverInteraction.Exit>().Count(), Is.EqualTo(1)));
            Assert.That(_events.OfType<IHoverInteraction.Exit>().Single().Enter,
                Is.SameAs(_events.OfType<IHoverInteraction.Enter>().Single()));
        }

        [UnityTest]
        public IEnumerator UpWithoutDownDoesNothing()
        {
            yield return Mount();
            simulate.MouseUp(Target);
            yield return Settle();
            Assert.That(_clicks.Value, Is.Zero);
            Assert.That(Presses, Is.Empty);
        }

        [UnityTest]
        public IEnumerator ClickOutsideDoesNothing()
        {
            yield return Mount();
            simulate.Click(new Vector2(590, 390));
            yield return Settle();
            Assert.That(_clicks.Value, Is.Zero);
            Assert.That(Presses, Is.Empty);
        }

        [UnityTest]
        public IEnumerator NonClickableChildForwardsToParent()
        {
            yield return Mount();
            _ui.OnNodeWithTag("label").Click();
            yield return Count(1);
        }

        [UnityTest]
        public IEnumerator PressPositionUsesLocalCoordinates()
        {
            yield return Mount();
            var position = Target.LocalToWorld(new Vector2(25, 35));
            simulate.MouseDown(position);
            yield return Wait(() => Assert.That(Presses.Length, Is.EqualTo(1)));
            Assert.That(Presses[0].PressPosition.X, Is.EqualTo(25).Within(0.5));
            Assert.That(Presses[0].PressPosition.Y, Is.EqualTo(35).Within(0.5));
            simulate.MouseUp(position);
            yield return Count(1);
        }

        [UnityTest]
        public IEnumerator InitiallyDisabledIgnoresClickAndHover()
        {
            _enabled.Value = false;
            yield return Mount();
            simulate.MouseMove(Target);
            simulate.Click(Target);
            yield return Settle();
            Assert.That(_clicks.Value, Is.Zero);
            Assert.That(_events, Is.Empty);
        }

        [UnityTest]
        public IEnumerator EnabledCanToggleRepeatedlyWithStableCallback()
        {
            yield return Mount();
            for (var i = 0; i < 3; i++)
            {
                _enabled.Value = false;
                yield return Settle();
                simulate.Click(Target);
                yield return Settle();
                Assert.That(_clicks.Value, Is.EqualTo(i));
                _enabled.Value = true;
                yield return Settle();
                simulate.Click(Target);
                yield return Count(i + 1);
            }
        }

        [UnityTest]
        public IEnumerator InitiallyDisabledCanBeEnabled()
        {
            _enabled.Value = false;
            yield return Mount();
            _enabled.Value = true;
            yield return Settle();
            simulate.Click(Target);
            yield return Count(1);
        }

        [UnityTest]
        public IEnumerator DisableWhileHeldEmitsCancelAndSuppressesRelease()
        {
            yield return Mount();
            simulate.MouseDown(Target);
            yield return Wait(() => Assert.That(Presses.Length, Is.EqualTo(1)));
            _enabled.Value = false;
            yield return Wait(() => Assert.That(Cancels.Length, Is.EqualTo(1)));
            Assert.That(Cancels[0].Press, Is.SameAs(Presses[0]));
            simulate.MouseUp(Target);
            yield return Settle();
            Assert.That(_clicks.Value, Is.Zero);
            Assert.That(Releases, Is.Empty);
            _enabled.Value = true;
            yield return Settle();
            simulate.Click(Target);
            yield return Count(1);
        }

        [UnityTest]
        public IEnumerator DisableWhileHoveredEmitsExit()
        {
            yield return Mount();
            simulate.MouseMove(Target);
            yield return Wait(() => Assert.That(_events.OfType<IHoverInteraction.Enter>().Count(), Is.EqualTo(1)));
            _enabled.Value = false;
            yield return Wait(() => Assert.That(_events.OfType<IHoverInteraction.Exit>().Count(), Is.EqualTo(1)));
            Assert.That(Presses, Is.Empty);
        }

        [UnityTest]
        public IEnumerator CallbackReplacementIsUsedByNextClick()
        {
            yield return Mount();
            simulate.Click(Target);
            yield return Count(1);
            _callback.Value = () => _clicks.Value += 10;
            yield return Settle();
            simulate.Click(Target);
            yield return Count(11);
        }

        [UnityTest]
        public IEnumerator CallbackReplacementWhileHeldUsesLatestCallback()
        {
            yield return Mount();
            simulate.MouseDown(Target);
            yield return Wait(() => Assert.That(Presses.Length, Is.EqualTo(1)));
            _callback.Value = () => _clicks.Value += 10;
            yield return Settle();
            simulate.MouseUp(Target);
            yield return Count(10);
            Assert.That(Cancels, Is.Empty);
        }

        [UnityTest]
        public IEnumerator CallbackMayRemoveItsOwnTarget()
        {
            _callback.Value = () => { _clicks.Value++; _visible.Value = false; };
            yield return Mount();
            simulate.Click(Target);
            yield return Count(1);
            yield return Wait(() => _ui.OnNodeWithTag("target").AssertDoesNotExist());
            yield return Wait(() => Assert.That(Releases.Length, Is.EqualTo(1)));
            Assert.That(Cancels, Is.Empty);
        }

        [UnityTest]
        public IEnumerator PrefixInsertionAndRemovalPreserveInputAndSource()
        {
            yield return Mount();
            var original = _ambient.Sources.First();
            for (var i = 1; i <= 4; i++)
            {
                _prefix.Value = !_prefix.Value;
                yield return Settle();
                simulate.Click(Target);
                yield return Count(i);
            }
            Assert.That(_ambient.Sources.All(s => ReferenceEquals(s, original)), Is.True);
            yield return Wait(() => Assert.That(Releases.Length, Is.EqualTo(4)));
        }

        [UnityTest]
        public IEnumerator CounterRecompositionDoesNotRestartActiveGesture()
        {
            yield return Mount();
            simulate.MouseDown(Target);
            yield return Wait(() => Assert.That(Presses.Length, Is.EqualTo(1)));
            _clicks.Value = 10;
            yield return Count(10);
            simulate.MouseUp(Target);
            yield return Count(11);
            Assert.That(Cancels, Is.Empty);
        }

        [UnityTest]
        public IEnumerator RemovingHeldNodeCancelsAndReleasesCapture()
        {
            yield return Mount();
            var target = Target;
            var position = target.worldBound.center;
            simulate.MouseDown(position);
            yield return Wait(() => Assert.That(Presses.Length, Is.EqualTo(1)));
            _visible.Value = false;
            yield return Wait(() => _ui.OnNodeWithTag("target").AssertDoesNotExist());
            yield return Wait(() => Assert.That(Cancels.Length, Is.EqualTo(1)));
            Assert.That(Cancels[0].Press, Is.SameAs(Presses[0]));
            Assert.That(target.HasPointerCapture(UnityEngine.UIElements.PointerId.mousePointerId), Is.False);
            simulate.MouseUp(position);
            yield return Settle();
            Assert.That(_clicks.Value, Is.Zero);
            Assert.That(Releases, Is.Empty);
            _visible.Value = true;
            yield return Wait(() => _ui.OnNodeWithTag("target").AssertIsDisplayed());
            simulate.Click(Target);
            yield return Count(1);
        }

        [UnityTest]
        public IEnumerator RemovingModifierCancelsAndReaddingDoesNotDuplicateHandlers()
        {
            yield return Mount();
            simulate.MouseDown(Target);
            yield return Wait(() => Assert.That(Presses.Length, Is.EqualTo(1)));
            _modifierPresent.Value = false;
            yield return Wait(() => Assert.That(Cancels.Length, Is.EqualTo(1)));
            simulate.MouseUp(Target);
            simulate.Click(Target);
            yield return Settle();
            Assert.That(_clicks.Value, Is.Zero);
            _modifierPresent.Value = true;
            yield return Settle();
            simulate.Click(Target);
            yield return Count(1);
        }

        [UnityTest]
        public IEnumerator DisposeWhileHeldCancelsWithoutClick()
        {
            yield return Mount();
            var position = Target.worldBound.center;
            simulate.MouseDown(position);
            yield return Wait(() => Assert.That(Presses.Length, Is.EqualTo(1)));
            _view.Dispose();
            yield return Wait(() => Assert.That(Cancels.Length, Is.EqualTo(1)));
            simulate.MouseUp(position);
            yield return Settle();
            Assert.That(_clicks.Value, Is.Zero);
            Assert.That(Releases, Is.Empty);
        }

        [UnityTest]
        public IEnumerator CustomIndicationReceivesSourceAndDrawsOnActualPanel()
        {
            yield return Mount();
            simulate.MouseDown(Target);
            yield return Wait(() => Assert.That(_ambient.PressedDraws, Is.GreaterThan(0)));
            if (!_shortOverload) Assert.That(_ambient.Sources.First(), Is.SameAs(_source.Value));
            simulate.MouseUp(Target);
            yield return Count(1);
            yield return Wait(() => Assert.That(_ambient.IdleDraws, Is.GreaterThan(0)));
        }

        [UnityTest]
        public IEnumerator DisabledAncestorPreventsInput()
        {
            yield return Mount();
            _view.SetEnabled(false);
            simulate.Click(Target);
            yield return Settle();
            Assert.That(_clicks.Value, Is.Zero);
            _view.SetEnabled(true);
            simulate.Click(Target);
            yield return Count(1);
        }

        [UnityTest]
        public IEnumerator HiddenTargetDoesNotReceiveInput()
        {
            yield return Mount();
            var position = Target.worldBound.center;
            Target.style.display = DisplayStyle.None;
            simulate.Click(position);
            yield return Settle();
            Assert.That(_clicks.Value, Is.Zero);
        }

        [UnityTest]
        public IEnumerator ReleaseOutsideCancelsClick()
        {
            yield return Mount();
            simulate.MouseDown(Target);
            yield return Wait(() => Assert.That(Presses.Length, Is.EqualTo(1)));
            simulate.MouseMove(new Vector2(590, 390));
            simulate.MouseUp(new Vector2(590, 390));
            yield return Wait(() => Assert.That(Cancels.Length, Is.EqualTo(1)));
            Assert.That(_clicks.Value, Is.Zero);
            Assert.That(Releases, Is.Empty);
        }

        [UnityTest]
        public IEnumerator SmallMovementInsideStillClicks()
        {
            yield return Mount();
            var position = Target.worldBound.center;
            simulate.MouseDown(position);
            simulate.MouseMove(position + new Vector2(2, 2));
            simulate.MouseUp(position + new Vector2(2, 2));
            yield return Count(1);
            Assert.That(Cancels, Is.Empty);
        }

        [UnityTest]
        public IEnumerator PointerCancelCancelsAndNextGestureStillWorks()
        {
            yield return Mount();
            simulate.MouseDown(Target);
            yield return Wait(() => Assert.That(Presses.Length, Is.EqualTo(1)));
            using (var evt = PointerCancelEvent.GetPooled()) { evt.target = Target; Target.SendEvent(evt); }
            yield return Wait(() => Assert.That(Cancels.Length, Is.EqualTo(1)));
            simulate.MouseUp(Target);
            yield return Settle();
            Assert.That(_clicks.Value, Is.Zero);
            simulate.Click(Target);
            yield return Count(1);
        }

        [UnityTest]
        public IEnumerator LosingPointerCaptureCancelsAndNextGestureStillWorks()
        {
            yield return Mount();
            simulate.MouseDown(Target);
            yield return Wait(() => Assert.That(Presses.Length, Is.EqualTo(1)));
            rootVisualElement.CapturePointer(UnityEngine.UIElements.PointerId.mousePointerId);
            simulate.MouseMove(new Vector2(590, 390));
            yield return Wait(() => Assert.That(Cancels.Length, Is.EqualTo(1)));
            rootVisualElement.ReleasePointer(UnityEngine.UIElements.PointerId.mousePointerId);
            simulate.MouseUp(Target);
            simulate.Click(Target);
            yield return Count(1);
        }

        [UnityTest]
        public IEnumerator NestedClickableConsumesClickBeforeParent()
        {
            _nested = true;
            yield return Mount();
            _ui.OnNodeWithTag("nested").Click();
            yield return Wait(() => Assert.That(_otherClicks, Is.EqualTo(1)));
            Assert.That(_clicks.Value, Is.Zero);
        }


        [UnityTest]
        public IEnumerator DefaultRippleWorksAcrossRepeatedClicks()
        {
            _useDefaultIndication = true;
            yield return Mount();
            for (var i = 1; i <= 3; i++)
            {
                simulate.Click(Target);
                yield return Count(i);
            }
        }

        [UnityTest]
        public IEnumerator ConsumedDownDoesNotStartGesture()
        {
            yield return Mount();
            Target.RegisterCallback<PointerDownEvent>(e => e.StopImmediatePropagation(), TrickleDown.TrickleDown);
            simulate.Click(Target);
            yield return Settle();
            Assert.That(_clicks.Value, Is.Zero);
            Assert.That(Presses, Is.Empty);
        }

        [UnityTest]
        public IEnumerator TouchTapClicksAndPairsInteractions()
        {
            yield return Mount();
            TouchDown(0);
            yield return Wait(() => Assert.That(Presses.Length, Is.EqualTo(1)));
            Assert.That(_clicks.Value, Is.Zero);
            TouchUp(0);
            yield return Count(1);
            yield return Wait(() => Assert.That(Releases.Length, Is.EqualTo(1)));
            Assert.That(Releases[0].Press, Is.SameAs(Presses[0]));
        }

        [UnityTest]
        public IEnumerator AnotherFingerCannotReleaseTheActivePress()
        {
            yield return Mount();
            TouchDown(0);
            yield return Wait(() => Assert.That(Presses.Length, Is.EqualTo(1)));
            TouchDown(1);
            TouchUp(1);
            yield return Settle();
            Assert.That(_clicks.Value, Is.Zero);
            Assert.That(Releases, Is.Empty);
            TouchUp(0);
            yield return Count(1);
        }

        [UnityTest]
        public IEnumerator LeavingAndReenteringDoesNotReviveCancelledGesture()
        {
            yield return Mount();
            simulate.MouseDown(Target);
            yield return Wait(() => Assert.That(Presses.Length, Is.EqualTo(1)));
            simulate.MouseMove(new Vector2(590, 390));
            yield return Wait(() => Assert.That(Cancels.Length, Is.EqualTo(1)));
            simulate.MouseMove(Target);
            simulate.MouseUp(Target);
            yield return Settle();
            Assert.That(_clicks.Value, Is.Zero);
            simulate.Click(Target);
            yield return Count(1);
        }

        [UnityTest]
        public IEnumerator ClickableIncludesItsPaddingArea()
        {
            yield return Mount();
            Target.style.paddingLeft = 20;
            Target.style.paddingRight = 20;
            simulate.FrameUpdate();
            simulate.Click(Target.LocalToWorld(new Vector2(170, 40)));
            yield return Count(1);
            simulate.Click(Target.LocalToWorld(new Vector2(5, 40)));
            yield return Count(2);
        }

        private void TouchDown(int finger)
        {
            var touch = new Touch { fingerId = finger, position = Target.worldBound.center, phase = TouchPhase.Began, pressure = 1 };
            using var evt = PointerDownEvent.GetPooled(touch, EventModifiers.None);
            evt.target = Target;
            Target.SendEvent(evt);
            simulate.FrameUpdate();
        }

        private void TouchUp(int finger)
        {
            var touch = new Touch { fingerId = finger, position = Target.worldBound.center, phase = TouchPhase.Ended };
            using var evt = PointerUpEvent.GetPooled(touch, EventModifiers.None);
            evt.target = Target;
            Target.SendEvent(evt);
            simulate.FrameUpdate();
        }
    }
}
