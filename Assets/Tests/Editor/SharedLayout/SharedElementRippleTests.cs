using System.Collections.Generic;
using System.Linq;
using Compose.Net;
using Compose.Net.Unity.Samples.Behaviors.Content;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using static Compose.Net.ComposeFunctions;

namespace Compose.Net.Unity.Samples.Tests;

public sealed partial class SharedLayoutDemoTests
{
    private RippleProbe _rippleProbe = null!;
    [Composable] private void RippleSampleContent()
    {
        _rippleProbe = Remember(() => new RippleProbe(LocalIndication.Current));
        CompositionLocalProvider(LocalIndication.Provides<IIndication>(_rippleProbe),
            () => SharedElementSample.Screen(_elementExpanded));
    }
    [TestCase(false, false)][TestCase(true, false)][TestCase(false, true)][TestCase(true, true)]
    public void RippleDrawingHostStaysAttachedDuringPressAndSharedTransition(bool expanded, bool preview)
    {
        _composer.Dispose(); Flush(); panelSize = new Vector2(800, 600);
        _elementExpanded = MutableStateOf(expanded); _composer = IComposer.Create();
        Bootstrap(_composer, _adapter, preview, new ComposeConfig(Logging: false),
            new RootNode(rootVisualElement), RippleSampleContent); Flush();
        var card = E("shared-element." + (expanded ? "expanded" : "collapsed"));
        var point = card.worldBound.center + new Vector2(60, 10);
        var draw = card.Children().Single(child => child.GetType().Name == "DrawOn");
        var detaches = 0;
        draw.RegisterCallback<DetachFromPanelEvent>(_ => detaches++);
        simulate.MouseMove(point); Flush(); Flush(350);
        Assert.That(detaches, Is.Zero, "Updating indication must keep its native drawing host attached");
        simulate.MouseDown(point); Flush();
        var state = _rippleProbe.States.Values.Single();
        var scale = state.Value("PressScale");
        for (var i = 0; i < 6; i++)
        {
            Flush(16);
            Assert.That(state.Value("PressScale"), Is.GreaterThan(scale));
            scale = state.Value("PressScale");
            Assert.That(detaches, Is.Zero, "Growing the circle must not detach its drawing host");
            RectNear(draw.worldBound, card.worldBound);
        }
        simulate.MouseUp(point); Flush();
        Assert.That(_elementExpanded.Value, Is.EqualTo(!expanded));
        var overlayDetaches = detaches; // Shared content itself moves into the scope overlay once.
        var alpha = state.Value("PressAlpha");
        var center = state.PressProperty("Center");
        Assert.That(alpha, Is.GreaterThan(0));
        Near(state.Value("PressScale"), scale, .001f);
        for (var i = 0; i < 18; i++)
        {
            Flush(16);
            Assert.That(detaches, Is.EqualTo(overlayDetaches), "Shared transition frames must reuse the ripple drawing host");
            Assert.That(card.Children().Single(child => child.GetType().Name == "DrawOn"), Is.SameAs(draw));
            RectNear(draw.worldBound, card.worldBound);
            Assert.That(state.Value("PressScale"), Is.GreaterThanOrEqualTo(scale));
            Assert.That(state.Value("PressAlpha"), Is.LessThan(alpha));
            Assert.That(state.PressProperty("Center"), Is.EqualTo(center));
            scale = state.Value("PressScale"); alpha = state.Value("PressAlpha");
        }
        Flush(12);
        Assert.That(state.Value("PressAlpha"), Is.Zero, "Released ripple finishes fading before the outgoing content is disposed");
        Flush(200);
        var incoming = E("shared-element." + (expanded ? "collapsed" : "expanded"));
        Assert.That(Layer(incoming), Is.Null);
        _composer.Dispose(); Flush();
        Assert.That(rootVisualElement.Query<VisualElement>().ToList().Any(element => element.GetType().Name == "DrawOn"), Is.False,
            "Disposal still removes the retained drawing hosts");
    }
    private sealed partial class RippleProbe : IIndication
    {
        private readonly IIndication inner;
        internal RippleProbe(IIndication inner) => this.inner = inner;
        internal readonly Dictionary<IInteractionSource, ProbeState> States = new();
        [Composable] public IIndicationInstance RememberUpdatedInstance(IInteractionSource source)
        {
            var state = Remember(source, () => new ProbeState()); States[source] = state;
            state.Ripple = inner.RememberUpdatedInstance(source);
            return state.Ripple;
        }
        internal sealed class ProbeState
        {
            internal IIndicationInstance Ripple = null!;
            internal object? PressProperty(string property)
            {
                var framesProperty = Ripple.GetType().GetProperty("Presses");
                Assert.That(framesProperty, Is.Not.Null, "The default indication must expose its press frames");
                var frames = (System.Array)framesProperty!.GetValue(Ripple)!;
                if (frames.Length == 0) return null;
                var frame = frames.GetValue(frames.Length - 1)!;
                return frame.GetType().GetProperty(property)!.GetValue(frame);
            }
            internal float Value(string property) => (float)(PressProperty(property.Substring("Press".Length)) ?? 0f);
        }
    }
}
