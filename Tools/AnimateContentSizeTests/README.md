# AnimateContentSize regressions

The 50 NUnit EditMode tests in `Assets/Tests/Editor/AnimateContentSizeTests.cs`
exercise the production modifier and real UI Toolkit layout. They create
offscreen editor panels, validate their layout, and advance the real
`ValueAnimation<float>` clock to deterministic timestamps. No test window,
scene change, sleeps, mock layout engine, or screenshot comparison is needed.

Coverage includes the first settled size, empty content, multiple children in
Box/Row/Column, nested children, visibility, absolute positioning, margins,
padding, borders, fractional dimensions, intermediate animation values,
adding/removing/reordering children, retargeting, delay, zero duration,
specification changes, detach, shared containers, reuse and independent hosts.

Run the fixture in Unity's EditMode Test Runner, or run an isolated batch project
while keeping the main project open:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/AnimateContentSizeTests/Run-Tests.ps1
```

The script requires the main project to have been opened in Unity (generated
`Compose.Net.Unity.csproj`, imported dependency DLLs and a cached Unity Test
Framework). It builds the actual adapter, stages dependencies under
`Temp/AnimateContentSizeProbe`, and uses the same cached Test Framework/NUnit
packages without modifying the main project's manifest or scene.

Use `-UnityEditor <path>` for a different editor location and
`-ResultsDirectory <path>` for a different output folder. By default, NUnit XML
and the Unity log are written to `Build/AnimateContentSizeTests`. The script
fails unless exactly 50 tests pass. Generated projects, DLLs, logs and results
are not committed.
