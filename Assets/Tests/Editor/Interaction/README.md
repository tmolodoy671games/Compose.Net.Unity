# Interaction UI tests

Run `Compose.Net.Unity.Interaction.Tests` in Unity Test Runner / Edit Mode.
The 163 cases dispatch real UI Toolkit mouse, touch, wheel, focus/key and capture events
through an attached panel. They do not call private modifier methods.

Coverage:
- Draggable and Scrollable on both axes: delta/slop, reverse direction, scaled coordinates,
  disable/re-enable, callback changes, source replacement, cancellation, modifier/element
  removal, panel detach, capture theft, out-of-bounds continuation, multiple touch pointers,
  programmatic mutation priority, right-button rejection, tap versus drag and clickable children.
- Scrollable: wheel versus drag interactions, horizontal wheel fallback, nested partial
  consumption, native ScrollView ancestors, progress, fling interruption and keyboard paging.
- Hoverable: enter/exit pairing, nested boundaries, shared sources, duplicate modifiers,
  disable/re-enable, recomposition, source replacement, removal/detach, touch filtering and bubbling.
- Every one of InteractionDemo's 24 cards: layout, actual input, live feedback and controls;
  the complete screen also verifies the last row remains accessible in the native ScrollView.

Also run `Compose.Net.Unity.Scroll.Tests` and `Compose.Net.Unity.Clickable.Tests` when changing
shared state, capture or hover behavior. Core tests live in `Compose.Net.Modifiers.Tests`.

Optional visual review: set `COMPOSE_INTERACTION_CAPTURE` to a PNG path and run the full-screen
test with a graphics device. Capture uses UI Toolkit's native CaptureToPNG helper.
