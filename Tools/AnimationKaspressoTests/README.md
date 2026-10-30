# Animation scenarios through Kaspresso

18 NUnit EditMode cases exercise real Compose content, state invalidation,
recomposition, production Unity adapters, and UI Toolkit layout. Kaspresso resolves
live nodes by `Modifier.TestTag`, checks geometry, presence and opacity, and records
named scenario steps with a hierarchy dump on failure. Classes have no namespace.

`UnityComposeTestDriver` owns the editor panel and composition. Its manual frame
clock advances Compose transitions; its UI Toolkit animation clock advances
`AnimateContentSize`. Assertions flush recomposition/layout without advancing time.
Reflection is confined to the panel and animation APIs inside this driver; the
scenarios do not create modifier nodes or apply transitions directly.

- Content size: Column/Row/Box resizing, growth/shrink, keyed children added and
  removed through state, retargeting, delay and zero duration (10 cases).
- Visibility: initially visible/hidden, repeated enter/exit, fade combined with
  expansion, collapsed first layout, natural content size, interrupted exit and
  resized content (8 cases).

The existing adapter tests retain checks of internal cleanup and style restoration.

Run from the Unity project root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/AnimationKaspressoTests/Run-Tests.ps1
```

Override `-UnityEditor` and `-ComposeNetDirectory` for another checkout. The script
builds the adapter and Kaspresso, compiles/patches the composable scenarios, stages
an isolated project under `Temp/AnimationKaspressoProbe`, and runs both suites.
The isolated runner uses derived fixtures to register the precompiled scenarios
with Unity's test discovery. The main project compiles the same scenario sources
using its test asmdef and normal Compose IL postprocessor.

Results: `Build/AnimationKaspressoTests/results.xml` and `editor.log`.
The script fails if any of the 18 cases fails, is skipped, or is not discovered.
