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

Modifier chains share one native element whenever their operations can use the
same bounds. The adapter creates a named `ComposeModifierWrapper` only when an
outer operation must retain different bounds: for example,
`Background(...).Offset(...)` or `Background(...).Padding(...).Background(...)`.
Adjacent padding, offset and size operations are combined, and zero padding or
offset does not create a boundary. An exact size already imposed by an earlier
modifier takes precedence over later size requests.

Chain collection, normalization, target selection and node reconciliation live
entirely in the Unity adapter. `UnityModifierElement` uses the existing
`IModifier.Flatten` / `Fill` / `Apply` / `Revert` contracts; the composer has no
native layout planner or wrapper-specific callbacks. The adapter owns its modifier
nodes through the platform-independent `IModifier.Node.Attach` / `Detach` lifecycle.

Wrappers are reused when only values change and removed when their boundary is
no longer needed. Parent layout data (weight, alignment and positioning) applies
to the outer element. `UnityReusableComposeNode.VisualElement` remains the
original content view; `LayoutElement` is the element to insert, track or remove
in a parent layout. Native factories continue to initialize the original view.

`ModifierLayerKaspressoTests` checks the native geometry, wrapper count, reuse,
removal and offset accumulation in an editor panel. The adapter still uses UI
Toolkit layout: this does not add RTL/start-end APIs, intrinsic measurement or
the complete Compose constraints model for unbounded `FillMaxSize` layouts.
