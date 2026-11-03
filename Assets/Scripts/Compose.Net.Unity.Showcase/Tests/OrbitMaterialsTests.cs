using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Showcase.Tests;

public sealed partial class OrbitScreenTests
{
    [UnityTest] public IEnumerator MaterialOpacitySliderUpdatesImmediatelyAndSurvivesNavigation()
    {
        yield return Settle(); Click("nav.Settings"); yield return Settle();
        var slider = (Slider)Find("settings.material-opacity");
        Assert.That(slider.value, Is.EqualTo(.15f).Within(.001f));
        var track = slider.Q(className: "unity-base-slider__tracker");
        var thumb = slider.Q(className: "unity-base-slider__dragger");
        var destination = new Vector2(track.worldBound.xMin + track.worldBound.width * .7f, thumb.worldBound.center.y);
        simulate.MouseDown(thumb); simulate.MouseMove(destination); yield return Settle();
        Assert.That(_state.MaterialOpacity.Value, Is.EqualTo(.7f).Within(.03f), "Dragging must update opacity before pointer release");
        simulate.MouseUp(destination); yield return Settle();
        slider.value = .42f; yield return Settle();
        Assert.That(_state.MaterialOpacity.Value, Is.EqualTo(.42f).Within(.001f));
        Assert.That(((TextElement)Find("settings.material-value")).text, Is.EqualTo("42%"));
        slider.Focus(); simulate.KeyPress(KeyCode.RightArrow); yield return Settle();
        Assert.That(_state.MaterialOpacity.Value, Is.GreaterThan(.42f), "The native slider must support keyboard adjustment");
        slider.value = 0; yield return Settle();
        Assert.That(_state.MaterialOpacity.Value, Is.Zero);
        CapturePreview("materials-off");
        slider.value = 1; yield return Settle();
        Assert.That(_state.MaterialOpacity.Value, Is.EqualTo(1));
        CapturePreview("materials-full");
        slider.value = .15f;
        Click("nav.Overview"); yield return Settle();
        CapturePreview("materials-overview-dark");
        Click("nav.Settings"); yield return Settle();
        Assert.That(((Slider)Find("settings.material-opacity")).value, Is.EqualTo(.15f).Within(.001f));
        CapturePreview("materials-settings-dark");
        Click("theme.toggle"); yield return Settle();
        Assert.That(_state.MaterialOpacity.Value, Is.EqualTo(.15f).Within(.001f));
        CapturePreview("materials-settings-light");
        Click("nav.Overview"); yield return Settle();
        CapturePreview("materials-overview-light");
    }
}
