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

## PointerInputDemo

Open **Assets/Scenes/PointerInputDemo.unity**, enable **Samples/PointerInputDemo** in SampleScene,
or use **Tools > Compose.Net > Pointer Input Demo**. **Add Pointer Input Demo to Scene** adds
or selects the component in the active scene. The gallery has 18 independent examples:
raw mouse/touch/pen/wheel events, three passes, nested consumption, tap/double/long press,
press release/cancel, free/axis/long-press drag, pan/zoom/rotation and panZoomLock, keyed restart,
latest state without restart, enable/removal, independent siblings and scoped timeouts.
Drag and transform cards include a moving/scaling/rotating marker and numeric readouts.

The adapter uses UI Toolkit picking, capture, coordinate conversion and event callbacks.
A PointerManipulator bridge adds Compose's Initial/Main/Final passes and shared consumption.
Native trickle-down guards are honored before Compose dispatch. Snapshots own their data;
no pooled native event is retained, and events are not queued when nobody is awaiting them.
Callbacks resume synchronously to allow consumption before the next handler/pass.

Run **Compose.Net.Unity.PointerInput.Tests** in the Edit Mode Test Runner: 45 event/gesture/
gallery tests plus 3 native scene-host lifecycle tests. The suite uses real UI Toolkit panels,
mouse/wheel/touch/pen events, capture, layout, nested elements and the actual demo screen.
Also run **Compose.Net.Unity.Clickable.Tests** when changing input dispatch.
See the Compose.Net repository README for the public API and migration details.


## Lazy list demo and tests

Open `Assets/Scenes/LazyListDemo.unity`, **Tools > Compose.Net > Lazy List Demo**, or
`SampleScene → Demos → LazyListDemo`. **Add Lazy List Demo to Scene** is idempotent and includes
inactive demo groups. The 18 cards cover both axes, reverse layout, variable sizes, padding and
spacing, stable-key insertion, sticky headers, initial positions, disabled input, 100,000 items,
nested rows, parent sizing and empty data. All cards provide jump/animation, prepend, resize,
state replacement, mount/unmount, input/reverse and native physics controls, with live counters.

Columns use native UI Toolkit `ListView` with dynamic heights. Toolkit has no horizontal
collection view, so rows use a sparse size index and bounded Compose window over `ScrollView`.
Native Toolkit handles wheel, touch, capture, keyboard navigation, nesting, inertia and overscroll.
Item trees remain in the parent composition, preserving keys, Remember and composition locals.

Run **Compose.Net.Unity.Lazy.Tests** in the Edit Mode Test Runner. The panel suite covers both axes
and reverse layouts, large/variable data, offscreen navigation, empty asynchronous data, animations
and cancellation, stable keys/lifetimes, padding/overlap/alignment, parent sizing, native input and
interactions, every demo control and the complete screen. Core API details and platform adaptations
are documented in the Compose.Net README.
