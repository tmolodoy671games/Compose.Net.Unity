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
    [Compose.Net.Kaspresso.AiGenerated]
    public sealed class ClickableExplicitModifierTests : ClickableFixture
    {
        public ClickableExplicitModifierTests() : base(false) { }

        [UnityTest]
        public IEnumerator NullIndicationDoesNotFallBackToAmbient()
        {
            _indication.Value = null;
            yield return Mount();
            simulate.Click(Target);
            yield return Count(1);
            yield return Wait(() => Assert.That(Releases.Length, Is.EqualTo(1)));
            Assert.That(_ambient.Sources, Is.Empty);
            Assert.That(_ambient.PressedDraws + _ambient.IdleDraws, Is.Zero);
        }

        [UnityTest]
        public IEnumerator NullSourceIsRememberedAcrossRecomposition()
        {
            _source.Value = null;
            yield return Mount();
            var source = _ambient.Sources.First();
            for (var i = 1; i <= 3; i++)
            {
                simulate.Click(Target);
                yield return Count(i);
            }
            Assert.That(_ambient.Sources.All(s => ReferenceEquals(s, source)), Is.True);
        }

        [UnityTest]
        public IEnumerator BothNullParametersStillAllowRepeatedClicks()
        {
            _source.Value = null;
            _indication.Value = null;
            yield return Mount();
            simulate.Click(Target);
            yield return Count(1);
            simulate.Click(Target);
            yield return Count(2);
            Assert.That(_ambient.Sources, Is.Empty);
        }

        [UnityTest]
        public IEnumerator ReplacingSourceCancelsOldGestureAndUsesNewSource()
        {
            yield return Mount();
            simulate.MouseDown(Target);
            yield return Wait(() => Assert.That(Presses.Length, Is.EqualTo(1)));
            var next = MutableInteractionSource();
            var nextEvents = new List<IInteraction>();
            using var cancellation = new CancellationTokenSource();
            var collecting = next.Interactions.Collect(nextEvents.Add, cancellation.Token);
            try
            {
                _source.Value = next;
                yield return Wait(() => Assert.That(Cancels.Length, Is.EqualTo(1)));
                simulate.MouseUp(Target);
                yield return Settle();
                Assert.That(_clicks.Value, Is.Zero);
                simulate.Click(Target);
                yield return Count(1);
                yield return Wait(() => Assert.That(nextEvents.OfType<IPressInteraction.Release>().Count(), Is.EqualTo(1)));
                Assert.That(Releases, Is.Empty);
            }
            finally { cancellation.Cancel(); }
            while (!collecting.IsCompleted) yield return null;
        }

        [UnityTest]
        public IEnumerator SharedSourceKeepsSiblingCallbacksIndependent()
        {
            _sibling = true;
            yield return Mount();
            simulate.Click(Target);
            yield return Count(1);
            Assert.That(_otherClicks, Is.Zero);
            _ui.OnNodeWithTag("sibling").Click();
            yield return Wait(() => Assert.That(_otherClicks, Is.EqualTo(1)));
            Assert.That(_clicks.Value, Is.EqualTo(1));
            yield return Wait(() => Assert.That(Releases.Length, Is.EqualTo(2)));
        }

        [UnityTest]
        public IEnumerator ReplacingIndicationPreservesInputSource()
        {
            yield return Mount();
            var replacement = new RecordingIndication();
            _indication.Value = replacement;
            yield return Settle();
            simulate.MouseDown(Target);
            yield return Wait(() => Assert.That(replacement.PressedDraws, Is.GreaterThan(0)));
            simulate.MouseUp(Target);
            yield return Count(1);
            Assert.That(replacement.Sources.First(), Is.SameAs(_source.Value));
        }

    }
}
