# Compose.Net.Kaspresso.Unity

Adapter for UI Test Framework 7.0.0. Reference `Compose.Net.Kaspresso.Unity`,
`Unity.UI.TestFramework.Runtime` and the core `Compose.Net.Kaspresso.dll` from your test assembly.
The adapter is compiled only with `UNITY_INCLUDE_TESTS`. Its panel is owned by the UI Test Framework fixture.

```csharp
using Compose.Net.Kaspresso;
using Compose.Net.Kaspresso.Unity;
using NUnit.Framework;
using UnityEngine.UIElements.TestFramework;

[AiGenerated]
public sealed class ScreenTests : UITestFixture
{
    [Test]
    public void Submit()
    {
        // Mount your ComposeView/content into rootVisualElement first.
        var test = KaspressoUnity.Create(simulate, () => rootVisualElement);
        test.OnNodeWithTag("submit").AssertIsDisplayed().Click();
        test.OnNodeWithTag("status").AssertText("Saved");
    }
}
```

Compose's `Modifier.TestTag` maps to `VisualElement.name`. Scope the root provider to your
ComposeView when other UI is present. The provider is evaluated afresh for each search.
Use `yield return test.WaitUntil(() => ...)` in a `[UnityTest]` for asynchronous composition.
The coroutine yields to the real player loop; `Refresh` also calls `PanelSimulator.FrameUpdate`.
Do not call the DSL from worker threads.

Click and text replacement use `PanelSimulator`. ReplaceText supports editable `TextField`
controls and the platform/language limitations of the framework's keyboard simulation.
ScrollTo uses enclosing ScrollViews and requires an already materialized element.
Displayed means attached, visible, nontransparent, nonzero area and at least partly inside
ancestor clipping rectangles and the panel; it is not a guarantee against sibling occlusion.

The local core Release DLL is in `Plugins`. To update it after core changes:

```sh
dotnet build /path/to/Compose.Net/Compose.Net.Kaspresso -c Release
cp /path/to/Compose.Net/Compose.Net.Kaspresso/bin/Release/netstandard2.1/Compose.Net.Kaspresso.dll Plugins/
```

Run `Compose.Net.Kaspresso.Unity.Tests` in the EditMode Test Runner.
