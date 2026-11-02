#nullable disable
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
    public abstract partial class ClickableFixture : UITestFixture
    {
        protected readonly bool _shortOverload;
        protected ComposeView _view;
        protected Kaspresso.Kaspresso _ui;
        protected IMutableState<bool> _enabled, _visible, _prefix, _modifierPresent;
        protected IMutableState<int> _clicks;
        protected IMutableState<Action> _callback;
        protected IMutableState<IMutableInteractionSource> _source;
        protected IMutableState<IIndication> _indication;
        protected RecordingIndication _ambient;
        protected CancellationTokenSource _collectCancellation;
        protected Task _collection;
        protected readonly List<IInteraction> _events = new();
        protected bool _sibling, _nested, _useDefaultIndication;
        protected IMutableState<IIndication> _localIndication;
        protected int _otherClicks;

        protected ClickableFixture(bool shortOverload) => _shortOverload = shortOverload;

        [SetUp]
        public void SetUp()
        {
            RecreatePanel();
            panelSize = new Vector2(600, 400);
            _enabled = MutableStateOf(true);
            _visible = MutableStateOf(true);
            _prefix = MutableStateOf(false);
            _modifierPresent = MutableStateOf(true);
            _clicks = MutableStateOf(0);
            _callback = MutableStateOf<Action>(() => _clicks.Value++);
            _source = MutableStateOf<IMutableInteractionSource>(MutableInteractionSource());
            _ambient = new RecordingIndication();
            _indication = MutableStateOf<IIndication>(_ambient);
            _events.Clear();
            _sibling = _nested = _useDefaultIndication = false;
            _localIndication = MutableStateOf<IIndication>(_ambient);
            _otherClicks = 0;
            _view = new ComposeView();
            _view.style.flexGrow = 1;
            rootVisualElement.Add(_view);
            _ui = KaspressoUnity.Create(simulate, () => _view);
            simulate.MouseMove(new Vector2(590, 390));
        }

        [TearDown]
        public void TearDown()
        {
            // Synchronous teardown runs before UITestFixture releases its input context.
            _view?.Dispose();
            rootVisualElement.ReleasePointer(UnityEngine.UIElements.PointerId.mousePointerId);
            simulate.MouseUp(new Vector2(590, 390));
            _collectCancellation?.Cancel();
            _collectCancellation?.Dispose();
            _collectCancellation = null;
            if (_collection?.IsFaulted == true) Assert.Fail(_collection.Exception?.ToString());
            _collection = null;
        }

        [Composable]
        protected void Content()
        {
            CompositionLocalProvider(LocalIndication.Provides(_useDefaultIndication ? LocalIndication.Current : _localIndication.Value), () =>
                Column(modifier: Modifier.FillMaxSize().Padding(20.Dp()), content: () =>
                {
                    if (_visible.Value)
                    {
                        var modifier = Modifier.Width(180.Dp()).Height(80.Dp()).Background(Color.DodgerBlue);
                        if (_prefix.Value) modifier = modifier.Scale(0.9f);
                        modifier = modifier.TestTag("target");
                        if (_modifierPresent.Value)
                            modifier = _shortOverload
                                ? modifier.Clickable(_callback.Value, _enabled.Value)
                                : modifier.Clickable(_callback.Value, _useDefaultIndication ? LocalIndication.Current : _indication.Value, _source.Value, _enabled.Value);
                        Column(modifier: modifier, content: () =>
                        {
                            Text("Target label", modifier: Modifier.TestTag("label"));
                            if (_nested)
                                Text("Nested target", modifier: Modifier.Size(120.Dp(), 32.Dp())
                                    .TestTag("nested").Clickable(OtherClick));
                        });
                    }
                    Text($"Clicks: {_clicks.Value}", modifier: Modifier.TestTag("count"));
                    if (_sibling)
                        Text("Sibling target", modifier: Modifier.Size(180.Dp(), 60.Dp()).TestTag("sibling")
                            .Clickable(OtherClick, _indication.Value, _source.Value));
                }));
        }

        protected void OtherClick() => _otherClicks++;

        protected IEnumerator Mount()
        {
            _view.SetContent(Content);
            var source = _shortOverload ? _ambient.Sources.FirstOrDefault() : _source.Value;
            if (source != null)
            {
                _collectCancellation = new CancellationTokenSource();
                _collection = source.Interactions.Collect(_events.Add, _collectCancellation.Token);
            }
            yield return Wait(() => _ui.OnNodeWithTag("target").AssertIsDisplayed());
            yield return Settle();
        }

        protected VisualElement Target => _view.Q("target") ?? throw new AssertionException("Missing target");
        protected IPressInteraction.Press[] Presses => _events.OfType<IPressInteraction.Press>().ToArray();
        protected IPressInteraction.Release[] Releases => _events.OfType<IPressInteraction.Release>().ToArray();
        protected IPressInteraction.Cancel[] Cancels => _events.OfType<IPressInteraction.Cancel>().ToArray();
        protected IEnumerator Count(int count) => Wait(() => _ui.OnNodeWithTag("count").AssertText($"Clicks: {count}"));

        protected IEnumerator Wait(Action assertion) => _ui.WaitUntil(() =>
        {
            ComposeInvalidatorHolder.ComposeInvalidator.Tick();
            simulate.FrameUpdate();
            try { assertion(); }
            catch (AssertionException e) { throw new KaspressoAssertionException(e.Message); }
        }, TimeSpan.FromSeconds(3));

        protected IEnumerator Settle()
        {
            for (var i = 0; i < 5; i++)
            {
                yield return null;
                ComposeInvalidatorHolder.ComposeInvalidator.Tick();
                simulate.FrameUpdate();
            }
        }

        // Records the real source and actual DrawOnTop calls from the panel, without replacing input.
        protected sealed partial class RecordingIndication : IIndication
        {
            public readonly List<IInteractionSource> Sources = new();
            public int PressedDraws, IdleDraws;

            [Composable]
            public IIndicationInstance RememberUpdatedInstance(IInteractionSource source)
            {
                var pressed = source.CollectIsPressedAsState().Value;
                Sources.Add(source);
                return new Instance(this, pressed);
            }

            private sealed class Instance(RecordingIndication owner, bool pressed) : IIndicationInstance
            {
                public void DrawIndication(IDrawScope scope)
                {
                    if (pressed) owner.PressedDraws++; else owner.IdleDraws++;
                    scope.DrawRect(pressed ? Color.Orange : Color.White, size: scope.Size, alpha: 0.2f);
                }
            }
        }
    }
}
