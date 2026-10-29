# AnimatedVisibility layout regressions

The 50 Kaspresso scenarios run against real UI Toolkit panels and production
transitions. NUnit only discovers the parameterized EditMode cases; actions and
checks use Kaspresso `KScreen`, `KView`, `ActAsync`, `AssertAsync` and named steps.
Selectors re-resolve the live hierarchy on every operation, and failures include
the Kaspresso step path and hierarchy dump. Coverage includes:

- 6 initial-layout and cancellation regressions, including the first painted size.
- 9 alignments for simultaneous horizontal and vertical enter/exit transitions.
- 6 natural content sizes, including empty, zero-width, zero-height and fractional sizes.
- 6 combinations of padding and borders.
- 6 timing cases covering delay, partial initial size, completion and zero duration.
- 2 clipping modes and 4 cases restoring pre-existing styles.
- 7 lifecycle cases covering repeated cycles, reversal, resizing, retargeting,
  cancellation, reparenting and independent hosts.
- 4 combinations of fade and size transitions, including restored opacity.

The first layout must already have the collapsed size to prevent an initial
full-size flash. Cancelling before layout must unregister the deferred transition.
The deterministic driver holds layout only while arranging these pre-measurement
scenarios. Ordinary actions and checks validate actual Yoga layout on idle.
The scenarios use custom layout actions rather than pointer clicks or scene playback.

Run from the Unity project root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/AnimatedVisibilityTests/Run-Tests.ps1
```

The runner builds the adapter and stages an isolated project under
`Temp/AnimatedVisibilityProbe`, using the main project's cached NUnit/Test
Framework packages. Results and logs are written to `Build/AnimatedVisibilityTests`.
The main project's scene and package manifest remain untouched.
The runner fails unless exactly 50 tests pass.

Tests have their own assembly definition under `Assets/Tests/Editor/AnimatedVisibility`.
The Editor-only `Compose.Net.Kaspresso.dll` comes from the sibling Compose.Net
repository's `Compose.Net.Kaspresso` project. To refresh it, build that project in
Release and copy `bin/Release/netstandard2.1/Compose.Net.Kaspresso.dll` to
`Assets/Packages/Compose.Net.Unity/Assemblies/Editor/`, preserving its `.meta` file.
