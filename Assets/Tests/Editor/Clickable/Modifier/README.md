# Modifier.Clickable UI tests

Run the `Compose.Net.Unity.Clickable.Tests` assembly in Unity Test Runner → EditMode.
The tests mount a minimal `ComposeView` on a UI Test Framework panel. They do not
reference the Samples assembly or `ClickableDemo`. Kaspresso locates live nodes
and waits for recomposition; `PanelSimulator` dispatches mouse input. Touch and
cancel cases dispatch real UI Toolkit pointer events through the panel.

The 78 cases cover:

- Both overloads: release-only activation, repeated/double clicks, holds, hover,
  local press coordinates, child hit testing, padding, hidden/disabled ancestors.
- `enabled`: initially disabled, enable/disable cycles, disabling a held or hovered target.
- Lifecycle: removing nodes/modifiers, disposal, restoring targets, pointer capture loss,
  explicit cancellation, leaving/reentering bounds and input after cancellation.
- Recomposition: new callbacks, callback replacement during a press, self-removal,
  modifier insertion/removal, remembered interaction-source identity.
- Mouse/touch, secondary-finger isolation, consumed input and nested clickables.
- Default ripple and custom indications, actual panel draw calls, every combination
  of nullable indication/source, replacement and sharing of explicit sources.

A successful click has one `Press` and its matching `Release`; cancelled gestures
have the matching `Cancel` and no click. The matching interaction is checked by
object identity. Each test recreates its panel and clears input during synchronous
teardown, before the framework releases its input context. No wall-clock sleeps,
private method invocation or direct `onClick` invocation are used.

For unattended execution, close the Editor for this project (or use a disposable
copy) and run:

```sh
"$UNITY_EDITOR" -batchmode -projectPath "$UNITY_PROJECT" \
  -runTests -testPlatform EditMode \
  -assemblyNames Compose.Net.Unity.Clickable.Tests \
  -testResults /tmp/clickable-results.xml -logFile /tmp/clickable-unity.log
```

Keep graphics enabled: the tests exercise real panel rendering. Unity exits when
the run finishes; do not add `-quit` to the test command.

The standalone demo is available at **Tools → Compose.Net → Clickable Demo**.
Its `ClickableDemo` component can also be attached beside a `UIDocument` whose
UXML contains a `ComposeView`. It presents 12 examples with counters, state and
interaction diagnostics. Keyboard activation, long-click and double-click
recognition are not exposed by this pointer-based API; a double click produces
two ordinary clicks.
