# AnimatedVisibility layout regressions

The four NUnit EditMode cases use real UI Toolkit panels and production size
transitions. They check horizontal and vertical enter/exit transitions before and
after the first layout, natural content size at every frame, padded shell size,
style restoration and balanced clipping after an unmeasured Apply.

Run from the Unity project root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/AnimatedVisibilityTests/Run-Tests.ps1
```

The runner builds the adapter and stages an isolated project under
`Temp/AnimatedVisibilityProbe`, using the main project's cached NUnit/Test
Framework packages. Results and logs are written to `Build/AnimatedVisibilityTests`.
The main project's scene and package manifest remain untouched.
