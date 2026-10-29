# AnimatedVisibility layout regressions

The six NUnit EditMode cases use real UI Toolkit panels and production size
transitions. They check horizontal and vertical enter/exit transitions before and
after the first layout, natural content size at every frame, padded shell size,
style restoration and balanced clipping after an unmeasured Apply. The first
layout must already have the collapsed size to prevent an initial full-size flash.
Cancelling before the first layout must unregister the deferred transition.

Run from the Unity project root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/AnimatedVisibilityTests/Run-Tests.ps1
```

The runner builds the adapter and stages an isolated project under
`Temp/AnimatedVisibilityProbe`, using the main project's cached NUnit/Test
Framework packages. Results and logs are written to `Build/AnimatedVisibilityTests`.
The main project's scene and package manifest remain untouched.
