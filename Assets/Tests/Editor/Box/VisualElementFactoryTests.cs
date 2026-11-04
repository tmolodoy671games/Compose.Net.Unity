using Compose.Net;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Tests;

public sealed partial class BoxTests
{
    private int _factoryCalls;
    private sealed class CustomBox : Box { }
    private sealed class CustomRow : Row { }
    private sealed class CustomColumn : Column { }

    [Test]
    public void VisualElementFactoriesReuseElementsAndUpdateInitializersAndLayout()
    {
        _factoryCalls = 0;
        _composer = IComposer.Create();
        Bootstrap(_composer, _adapter, false, new ComposeConfig(Logging: false),
            new Root(rootVisualElement), FactoryContent);
        Flush();
        var box = rootVisualElement.Q<CustomBox>("factory-box");
        var label = rootVisualElement.Q<Label>("factory-label");
        Assert.That(box, Is.Not.Null);
        Assert.That(rootVisualElement.Q<CustomRow>(), Is.Not.Null);
        Assert.That(rootVisualElement.Q<CustomColumn>(), Is.Not.Null);
        Assert.That(_factoryCalls, Is.EqualTo(6));
        Assert.That(label.text, Is.EqualTo("0"));
        Assert.That(box.ContentAlignment, Is.EqualTo(Alignment.TopStart));
        Assert.That(box.PropagateMinConstraints, Is.False);

        Update();
        Assert.That(_factoryCalls, Is.EqualTo(6), "Recomposition must reuse every factory-created element.");
        Assert.That(rootVisualElement.Q<CustomBox>("factory-box"), Is.SameAs(box));
        Assert.That(rootVisualElement.Q<Label>("factory-label"), Is.SameAs(label));
        Assert.That(label.text, Is.EqualTo("1"));
        Assert.That(box.ContentAlignment, Is.EqualTo(Alignment.BottomEnd));
        Assert.That(box.PropagateMinConstraints, Is.True);
    }

    [Composable]
    private void FactoryContent()
    {
        var revision = _revision.Value;
        ComposeUnityFunctions.Box(
            visualElementFactory: () => { _factoryCalls++; return new CustomBox { name = "factory-box" }; },
            contentAlignment: revision == 0 ? Alignment.TopStart : Alignment.BottomEnd,
            propagateMinConstraints: revision != 0,
            content: () => ComposeUnityFunctions.Column(
                visualElementFactory: () => { _factoryCalls++; return new CustomColumn(); },
                content: () => ComposeUnityFunctions.Row(
                    visualElementFactory: () => { _factoryCalls++; return new CustomRow(); },
                    content: () =>
                    {
                        ComposeUnityFunctions.ReusableComposeNode(
                            visualElementFactory: () => { _factoryCalls++; return new Label { name = "factory-label" }; },
                            initializer: label => label.text = revision.ToString());
                        ComposeUnityFunctions.Spacer(
                            visualElementFactory: () => { _factoryCalls++; return new VisualElement(); },
                            modifier: Modifier.Size(8.Dp()));
                        ComposeUnityFunctions.Box(
                            visualElementFactory: () => { _factoryCalls++; return new CustomBox(); },
                            modifier: Modifier.Size(12.Dp()));
                    })));
    }
}
