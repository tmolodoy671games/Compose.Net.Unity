# Compose.Net.Unity

Compose implementation for Unity.

## Installation

In Unity, open **Window > Package Manager**, click **+**, select **Add package from git URL**, and paste one of the URLs below.

HTTPS:

```text
https://github.com/tmolodoy671games/Unity-Compose.git?path=/Assets/Packages/Compose.Net.Unity
```

SSH (requires SSH access to GitHub):

```text
git@github.com:tmolodoy671games/Unity-Compose.git?path=/Assets/Packages/Compose.Net.Unity
```

The package appears as **Compose.Net.Unity** in Package Manager.

## File-scoped namespaces

For MonoBehaviour and ScriptableObject imports with `namespace Example;`, see
[Unity namespace support](Tools/UnityNamespaceSupport/README.md). The repository
includes a reversible patch for Unity’s MonoScript source generator and regression
tests that check actual script imports.


## Scroll demo and tests

Open `Assets/Scenes/ScrollDemo.unity`, or use **Tools > Compose.Net > Scroll Demo**.
**Tools > Compose.Net > Add Scroll Demo to Scene** adds it to the current scene.
The 18 interactive cards cover both axes, reverse origins, disabled input, initial offsets,
resizing, content/state changes, empty and short content, native physics and nested scrolling.
Each card has jump/relative/animation controls and enable, reverse, resize, replace and remove toggles.

Scroll modifiers use native UI Toolkit `ScrollView`; no custom wheel easing or drag physics.
`enabled: false` preserves measurement, clipping, state control and clickable children.
The public API is documented in the Compose.Net repository. Wheel sensitivity follows UI Toolkit.

Run **Compose.Net.Unity.Scroll.Tests** in the Edit Mode Test Runner. The tests use real panels,
wheel, keyboard and touch events, and a deterministic clock for programmatic animations.
They cover all axis/reverse combinations, bounds, layout, gesture interruption, subscriptions,
native capture, nested propagation, all demo controls, and the complete demo screen.

## Shared layout demo and tests

`SharedLayoutDemo` is included in `Assets/Scenes/SampleScene.unity` with its own PanelRenderer.
Open **Tools > Compose.Net > Shared Layout Demo** for the Editor window, or
**Tools > Compose.Net > Add Shared Layout Demo to Scene** to add it to another scene.
The 27 cards cover SharedElement/SharedBounds, all scale modes, remeasurement, placeholders,
overlay clipping and ordering, nesting, matching, dynamic enable/disable, reversal, retargeting,
caller-managed visibility, delayed and instantaneous transitions, springs, zero-size targets,
remembered state and independent AnimatedVisibility containers. A/B/C and clickable shared cards
change targets; Resize changes a destination in flight, and Enable toggles matching.

Shared transitions use live UI Toolkit elements, native layout, transforms, rounded clipping and
picking. There is no screenshot renderer or synthetic pointer pipeline. The core README describes
parameter semantics and the native measurement limits.

Run **Compose.Net.Unity.SharedLayout.Tests** in the Edit Mode Test Runner. Tests check every demo
in both directions, numeric geometry and timing, clipping, scale policies, parent opacity isolation,
placeholder displacement, interrupted transitions, retention/disposal, configuration, scope isolation
and actual pointer input to buttons and moving shared content.
