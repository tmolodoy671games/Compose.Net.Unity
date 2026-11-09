# Compose.Net

.Net implementation of Compose.

Unity-specific preview annotations and rendering live in the companion
`Compose.Net.Unity` integration. The core library has no Unity preview attributes.

## Material Design and themes

Shared light/dark themes, nested overrides, typography, shapes and a foundational
Material 3 component set are available in `Compose.Net.Theming` and
`Compose.Net.Material3`. See the [API and Unity gallery](docs/material-design.md).

## Tabs component gallery

[Tabs Lab](Compose.Net.MaterialDemo/README.md) demonstrates primary and secondary
decorative tabs in light and dark themes, ceramic/satin/metal finishes, smoothly animated indicators,
retained nested selections, disabled destinations and reduced motion.
Open **Tools → Compose.Net → Tabs Lab** in Unity.

## ORBIT application demo

[ORBIT](Compose.Net.Showcase/README.md) is a complete seven-screen creative workspace:
projects, search, favorites, task milestones, activity, focus timer, settings, and a
motion playground with up to 144 particles. It combines shared-element navigation,
lazy grids/lists, a pager, animated controls, custom drawing and gesture input.
Open **Tools → Compose.Net → ORBIT — Full App Demo** in the companion Unity project
after following the [build/install instructions](Compose.Net.Showcase/README.md#run-in-unity).


`By` delegates a C# local to `IMutableState<T>`, following the
[Compose delegated-state pattern](https://developer.android.com/develop/ui/compose/state):

```csharp
public partial class CounterScreen
{
    [Composable]
    public void Content()
    {
        var count = By(Remember(() => MutableStateOf(1)));
        Text($"Count: {count}");
        Box(modifier: Modifier.Clickable(() => count++),
            content: () => Text("Increment"));
    }
}
```

The local has the value's type in source code. Compose.Net.CodeGen rewrites its
reads and writes (including `++`, compound assignments, local functions and
captured callbacks) to the state's `Value`. State reads subscribe the current
composition; equal writes use the existing `MutableStateOf` equality behavior.
`Remember` retains the holder; `By` itself does not remember or copy it.
`var value = By(existingState)` also delegates to an externally owned holder.

Use one `var` local per declaration in a method or property accessor in a
`partial` type; enclosing types must also be partial. `By` needs both the Compose
generator and assembly patcher, including for ordinary methods outside composition.
`ref`, `out` and explicit `in` arguments are rejected because the delegated value
is a property. For struct values, assign a replacement value instead of mutating
its fields. `nameof`, inferred tuple/object member names and independent locals
with the same spelling are preserved. Lambda captures retain the state holder;
a method group such as `count.ToString` binds the current value when created.

Unity Edit Mode tests are in `Assets/Tests/Editor/DelegatedState`, assembly
`Compose.Net.Unity.DelegatedState.Tests`. Run generator diagnostics with
`dotnet run --project Compose.Net.CodeGen.Tests -c Release`; runtime regressions
are included in `Compose.Net.Modifiers.Tests`.

Library builds leave the generated composable implementations in the DLL without
patching it. The Unity integration applies `Compose.Net.Patcher` inside the Editor:

- Imported managed plug-ins are checked after import and on domain reload. DLLs
  with generated composable members are patched in place, together with their PDBs,
  and reimported before Play Mode. Unrelated and already patched DLLs are unchanged.
- Player builds patch the staged managed assemblies in
  `IPostBuildPlayerScriptDLLs`, before managed stripping and IL2CPP conversion.
- Scripts compiled by Unity continue to use `ComposeILPostProcessor`.

Install the patcher and Mono.Cecil as Editor-only plug-ins. These are build tools,
not runtime references of `Compose.Net`. Imported DLLs must be writable; replacing
or restoring a package causes Unity to apply the patch again. Source generation
still runs when the original library is compiled.

For a non-Unity consumer that needs a patched library directly, opt in with
`dotnet build -p:ComposePatchAfterBuild=true`, or run
`dotnet run --project Compose.Net.Patch -- path/to/library.dll`.
The .NET regression executable patches only its own output copies.

[Unity plug-in platform settings](https://docs.unity3d.com/Manual/PluginInspector.html)

`AnimatedContent` follows the [AndroidX AnimatedContent API](https://github.com/androidx/androidx/blob/androidx-main/compose/animation/animation/src/commonMain/kotlin/androidx/compose/animation/AnimatedContent.kt).
Use the state passed to the content lambda: outgoing and incoming content coexist during a transition.

```csharp
AnimatedContent(
    targetState: page,
    contentKey: value => value.Id,
    label: "Pages",
    contentAlignment: Alignment.Center,
    transitionSpec: scope => scope.SlideIntoContainer(SlideDirection.Left)
        .TogetherWith(scope.SlideOutOfContainer(SlideDirection.Left))
        .Using(SizeTransform(clip: false,
            sizeAnimationSpec: (initial, target) => Tween(500))),
    content: (scope, value) => Text(value.Title,
        modifier: Modifier.AnimateEnterExit(scope, enter: FadeIn(), exit: FadeOut())));
```

The state-only content overload is also available. `UpdateTransition(targetState, label)`
returns a transition with `CurrentState`, `TargetState`, and `IsRunning`; call
`transition.AnimatedContent(...)` to share it between containers. Each content scope exposes
its own `Transition<EnterExitState>` and supports child `AnimateEnterExit` animations.
Child animations and `KeepUntilTransitionsFinished` participate in content disposal.

`ContentTransform` exposes `TargetContentEnter`, `InitialContentExit`, `TargetContentZIndex`
and `SizeTransform` (`Enter`/`Exit` remain aliases). `.Using(null)` disables size animation
and clipping; `.Using(SizeTransform(clip: false))` preserves animation without clipping.
The default is a 90 ms fade-out followed by a 220 ms fade/scale-in delayed by 90 ms,
with initial scale 0.92 and a critically damped size spring (stiffness 200).
Retargeting preserves the rendered values, remembered content for surviving keys, and the
size spring's velocity. Equal content keys update data without starting another transition.

C# keeps the required content delegate second, and uses the existing `Size` (UI units),
`AnimationSpec`, and `SlideDirection` types. Start/End follow this library's LTR alignment
aliases. Adapters provide compound composition through `IComposeLayouts`; Unity owns its
internal animated-content layout/item nodes. Adapters implement
`ITransitionSnapshotNode` for uninterrupted visual transitions.
The Unity adapter includes these implementations, `AnimatedContentDemo` (21 examples),
and Edit Mode UI tests under `Assets/Tests/Editor/AnimatedContent`.

Run the platform-independent regressions with
`dotnet run --project Compose.Net.Modifiers.Tests -c Release`.


`VerticalScroll` and `HorizontalScroll` follow the parameter order and edge semantics of
[AndroidX Scroll.kt](https://github.com/androidx/androidx/blob/androidx-main/compose/foundation/foundation/src/commonMain/kotlin/androidx/compose/foundation/Scroll.kt):

```csharp
var scroll = RememberScrollState(initial: 120);
Column(modifier: Modifier.Height(240.Dp()).VerticalScroll(
    scroll, enabled: true, flingBehavior: null, reverseScrolling: false), content: Items);
// Explicit null disables elastic overscroll; omitting it uses the adapter default.
var horizontalScroll = RememberScrollState();
Row(modifier: Modifier.Width(320.Dp()).HorizontalScroll(
    horizontalScroll, overscrollEffect: null, enabled: false), content: Cards);
scroll.ScrollTo(80); // Works even when user input is disabled; returns consumed distance.
await scroll.AnimateScrollTo(scroll.MaxValue, Tween(500));
```

Use a separate state for each viewport. Zero means top/left, or bottom/right when reversed;
reversing does not reorder children. `enabled` controls user input, while measurement,
clipping and programmatic scrolling remain active. `RememberScrollState(initial)` retains its
first initial value. `MaxValue` is `float.MaxValue` before layout; `ViewportSize` is initially zero.
Bounds, scrollability, direction and `IsScrollInProgress` are observable. New commands and native
input cancel pending programmatic animations; forgetting remembered state cancels its animation.
Drag interactions are available through `state.InteractionSource.CollectIsDraggedAsState()`.

The Unity adapter uses UI Toolkit `ScrollView` for input, layout, clipping, native inertia,
overscroll and nested boundary forwarding. State updates are subscribed directly, without
recomposing the scrolling content. Both native scrollbars are hidden. C# positions remain
fractional UI units; horizontal Start/End use the library's existing LTR conventions.
`FlingBehavior(decelerationRate)` and `OverscrollEffect(elasticity)` configure native physics;
they do not reproduce Android's custom coroutine-based fling or effect-node interfaces.
Programmatic animations use the existing `AnimationSpec` system.

Migration: replace `userScrollEnabled:` with `enabled:`. The old `scrollMultiplier` and modifier
`interactionSource` arguments and `SmoothScrollBy` are removed; wheel sensitivity comes from
UI Toolkit, interactions from the state, and animation uses `AnimateScrollBy`/`AnimateScrollTo`.
Adapters implement `IScrollStateAdapter` to publish atomic measurements and native input, and to
subscribe/unsubscribe position changes and programmatic-scroll requests.

The Unity project contains **Tools > Compose.Net > Scroll Demo**, an add-to-scene menu,
18 interactive examples, and Edit Mode UI tests in `Assets/Tests/Editor/Scroll`.
Run those tests with Unity Test Runner (assembly `Compose.Net.Unity.Scroll.Tests`).
Platform-independent scroll regressions run with the existing `Compose.Net.Modifiers.Tests` executable.

`HorizontalPager` and `VerticalPager` follow the
[AndroidX Foundation Pager contract](https://developer.android.com/develop/ui/compose/layouts/pager).
The existing C# argument order is retained (required `pageContent` comes second).
Both support content padding, spacing, `PageSize.Fill`, `PageSize.Fixed` and custom
`PageSize` implementations, cross-axis alignment, stable keys, reverse layout,
`userScrollEnabled`, `beyondViewportPageCount`, and Start/Center/End snapping.
A fixed size is independent of content padding. Page offsets include spacing and
are constrained to `[-0.5, 0.5]`; out-of-range page requests clamp to the data set.

```csharp
var pager = RememberPagerState(() => items.Count, initialPage: 2);
HorizontalPager(pager,
    pageContent: page => Text(items[page].Title),
    modifier: Modifier.FillMaxWidth().Height(180.Dp()),
    contentPadding: new PaddingValues(Horizontal: 16.Dp()),
    pageSpacing: 8.Dp(),
    key: page => items[page].Id);
// Use ScrollToPage for a jump, or await AnimateScrollToPage for an animation.
```

`CurrentPage` is the nearest page during motion, `SettledPage` changes when motion
stops, and `TargetPage` describes the running animation. The state also exposes
`CurrentPageOffsetFraction`, `LayoutInfo.VisiblePagesInfo`, scroll availability,
and `GetOffsetDistanceInPages`. Recomposition updates the page-count provider;
resizing retains the selected page and offset, and keyed insertions retain the
selected item. A new scroll request cancels the previous animation. Only visible
pages and the requested extra pages are composed; leaving the window disposes
page effects and remembered state, as with Compose's lazy layouts.

The Unity adapter uses UI Toolkit `ScrollView` for touch gestures, wheel input,
nested-scroll forwarding, clipping, and content translation. It adds lazy page
placement, keyboard paging and settling through the existing animation/frame
clock. `PagerDefaults.FlingBehavior` configures the page limit, positional
threshold and snap animation. Platform adaptation: input physics and nested
scroll routing follow UI Toolkit; Android-specific `NestedScrollConnection`,
`OverscrollEffect` and coroutine `ScrollableState` types are not exposed as empty
compatibility parameters. The library's alignment aliases currently use LTR.

The Unity project includes **PagerDemo** (21 interactive examples) in SampleScene,
a repeatable **Tools → Compose.Net → Add Pager Demo to Scene** command, and real
UI Toolkit panel tests under `Assets/Tests/Editor/Pager`. Platform-independent
pager regressions run with the existing `Compose.Net.Modifiers.Tests` executable.

`PointerInput` follows the keyed lifetime and three-pass event model of
[AndroidX pointer input](https://developer.android.com/develop/ui/compose/touch-input/pointer-input/understand-gestures)
and [SuspendingPointerInputFilter](https://github.com/androidx/androidx/blob/androidx-main/compose/ui/ui/src/commonMain/kotlin/androidx/compose/ui/input/pointer/SuspendingPointerInputFilter.kt):

```csharp
Modifier.PointerInput(key1: item.Id, key2: enabled, block: async input =>
{
    await input.AwaitEachGesture(async events =>
    {
        var down = await events.AwaitFirstDown();
        down.Consume();
        var up = await events.WaitForUpOrCancellation();
        if (up != null) { up.Consume(); HandleTap(up.Position); }
    });
});

Modifier.PointerInput(keys: new object?[] { item.Id, enabled, mode }, block: input =>
    input.DetectTapGestures(onTap: HandleTap, onPress: async (press, position) =>
    {
        ShowPressed(position);
        var released = await press.TryAwaitRelease(); // false on cancellation
        HidePressed(released);
    }));
```

One key (including null), two keys and a nonempty key array are supported. Keys use value
comparison; key arrays are copied and compared element by element. A block starts on its first
input event. Changing a key or the delegate's method cancels it and starts a fresh block on the
next event. Recreating a closure at the same call site does not restart an active block. Captured
values therefore stay captured: use a stable remembered state holder to read the latest value.
Removing the modifier or detaching its element cancels its waits and releases native capture.
A completed block stays completed until reset. Each run has its own `CancellationToken`.

`AwaitPointerEvent()` returns a durable `PointerEvent` with `Changes`, `Type`, `Buttons` and
`KeyboardModifiers`. `Initial` and `Final` travel parent to child; `Main` travels child to parent,
including multiple modifiers on one element. One snapshot is reused across the passes.
`Consume()` shares its flag with other handlers without hiding the event from them.
`CurrentEvent`, `Size`, `ExtendedTouchPadding`, `ViewConfiguration`, generic result scopes,
`WithTimeout` and `WithTimeoutOrNull` are available. Concurrent listeners need separate
`AwaitPointerEventScope` blocks. Events are not queued while a block is doing unrelated work.
Inside an event scope, consume before awaiting again; C# cannot enforce Kotlin's restricted
suspension rules. Use the run's `Token` for any additional asynchronous work.

Tap detection supports release/cancel-aware asynchronous `onPress`, double-tap minimum and
maximum intervals, long press, bounds cancellation and consumption in the Final pass. Drag
supports touch/mouse slop, over-slop first deltas, horizontal/vertical axes, pointer handoff,
cancellation and long-press drag. Transform detection supports pan, zoom, weighted rotation,
touch slop and `panZoomLock`. `CalculateCentroid`, `CalculateCentroidSize`, `CalculatePan`,
`CalculateZoom` and `CalculateRotation` operate on a whole event and exclude entering/leaving
pointers. The required C# gesture callback comes first; optional callbacks use named arguments.

The Unity adapter uses UI Toolkit picking, pointer capture, panel coordinates and native
mouse/touch/pen/wheel events. A small `PointerManipulator` bridge supplies Compose's extra
Final pass and independent consumption state. Native trickle-down guards run first; consumed gestures are withheld from native
controls only after every Compose pass. No polling, custom raycaster, gesture event queue,
reflection into Unity, or retained pooled Unity events is used. Positions remain fractional UI
units. Desktop defaults are 8 UI units of touch slop, 0.5 mouse slop, 500 ms long press and
40–300 ms between taps. Extended touch padding is zero: the native hit target is authoritative.
Raw pointer input does not add keyboard activation, accessibility semantics or an indication;
use a higher-level control when those interactions are needed.

Migration: prefer `AwaitPointerEvent().Changes` and `ChangedToDown()`; the old single-change
`AwaitPointerInputChange()` and `ChangedToPressed()` remain compatibility helpers. `onPress`
now accepts `(IPressGestureScope, Offset) => Task`; gesture thresholds come from
`scope.ViewConfiguration` instead of global constants. The Unity project includes 18 live
examples in `PointerInputDemo`, an Editor window under **Tools > Compose.Net > Pointer Input
Demo**, `Assets/Scenes/PointerInputDemo.unity`, and Edit Mode UI tests in assembly
`Compose.Net.Unity.PointerInput.Tests`.


`SharedBounds` and `SharedElement` follow the parameter order and matching semantics of
[AndroidX SharedTransitionScope](https://github.com/androidx/androidx/blob/androidx-main/compose/animation/animation/src/commonMain/kotlin/androidx/compose/animation/SharedTransitionScope.kt).
Create a separate remembered state at each modifier site; equal keys match only within one scope.

```csharp
SharedTransitionLayout(shared => AnimatedContent(page, (visibility, value) =>
    Box(content: () => Text(value.Title), modifier: Modifier
        .Size(value.Width.Dp(), value.Height.Dp())
        .SharedBounds(shared.RememberSharedContentState("card"), visibility,
            boundsTransform: (_, _) => Tween(700),
            resizeMode: ResizeMode.ScaleToBounds(ContentScale.Fit, Alignment.Center),
            placeholderSize: PlaceholderSize.ContentSize,
            clipInOverlayDuringTransition: new OverlayClip(RoundedCornerShape(16.Dp()))))));
```

`SharedElement` draws only the incoming content and remeasures it at the animated bounds.
`SharedBounds` draws both contents with independent enter/exit timing; its default resize mode is
`ScaleToBounds(FillWidth, Center)`. `RemeasureToBounds` uses native layout on every size change.
`ScaleToBounds` also supports FillHeight, FillBounds, Fit, Crop, Inside and None with all alignments.
`PlaceholderSize.ContentSize`, `AnimatedSize`, or a custom size function controls the original layout slot.
The default bounds spring is critically damped with stiffness 400 and animates rectangle edges.
Custom bounds transforms use the existing finite `AnimationSpec` and receive root-relative rectangles.
An interrupted tween continues with a spring (stiffness 1500), carrying its current velocity; the bounds
transform is initialized once per transition, not called again on every retarget. Subpixel float noise
does not restart the animation. A target change on the completion frame still animates to the new bounds.

The Unity adapter moves the live VisualElement into a scope overlay, preserving its logical slot.
UI Toolkit provides layout, transforms, native picking, sibling ordering and rounded overflow clipping;
there are no cloned draw commands, screenshot textures or custom hit testing. The overlay escapes parent
alpha, scale and clipping. `renderInOverlayDuringTransition: false` keeps those parent effects.
`zIndexInOverlay` controls ordering and `OverlayClip.ParentClip` inherits the nearest explicit shared
ancestor `SharedBounds` clip. `OverlayClip.None` disables inherited clipping. Ordinary modifier clipping
still applies. `SharedContentState.ClipPathInOverlay` exposes the resolved root-relative path during drawing;
it is borrowed from the host and must not be disposed. `ParentSharedContentState` refers to the nearest
`SharedBounds`, skipping intermediate shared elements.

`IsMatchFound` and `IsTransitionActive` are observable. Unmatched or disabled modifiers do not animate.
`SharedContentConfig` controls enabling and whether disabling interrupts an ongoing transition.
Override `IsEnabledFor(SharedContentState)` for AndroidX's state-receiver property, or the existing
`IsEnabled` property when the state is not needed. State read by the config is observed even while idle.
`AlternativeTargetBoundsInTransitionScopeAfterRemoval(state, previousBounds, scopeSize)` may supply a
replacement destination when the target is disposed. Returning null (the default) cancels the transition.
Removing the source alone allows the destination to finish. `SharedTransitionDefaults.SharedContentConfig`
is the immutable default configuration.
Bounds are measured again after layout changes; reversal and retargeting start at the rendered bounds.
Shared animations retain outgoing `AnimatedContent`/scoped `AnimatedVisibility` content even when the
parent transition is shorter or instantaneous. `SharedElementWithCallerManagedVisibility` keeps layout
ownership with the caller; remove inactive content after `IsTransitionActive` becomes false.

`SharedTransitionScope((shared, rootModifier) => ...)` creates a scope without an extra container.
Apply `rootModifier` to exactly one topmost layout before composing shared children. Ordinary content can
use `Modifier.RenderInSharedTransitionScopeOverlay(shared, zIndexInOverlay, renderInOverlay)`; the observed
predicate defaults to `shared.IsTransitionActive`. Root shared layers at the default z-index draw below
ordinary overlay content, allowing controls such as a floating action button to stay above the transition.

Call `state.PrepareTransitionWithInitialVelocity(new Velocity(x, y))` when releasing a gesture, before
changing the destination. Velocity is in logical layout units per second. The next transition adds a
spring momentum offset to its bounds (also for custom tweens); disabled states ignore the request.

C# uses top-level ResizeMode/PlaceholderSize/OverlayClip types and the existing Shape and AnimationSpec.
Overlay clips support rectangles and rounded rectangles, rather than arbitrary Android Path callbacks.
Unity has no Compose lookahead measurement pass: scale mode retains the measured native content size,
while remeasure mode constrains the live native subtree. Intrinsic content changes during remeasure mode
should supply explicit target dimensions. `SkipToLookaheadSize`, `SkipToLookaheadPosition` and interactive
`DeferredTransition` require a lookahead/approach-layout engine and are not implemented. Use one shared or
overlay modifier per native layout; nest layouts to combine them. Ancestor transforms are excluded from target coordinates, as are parent
enter/exit transforms; native ScrollView offsets are included.

The Unity project includes `SharedLayoutDemo` in `Assets/Scenes/SampleScene.unity`,
**Tools > Compose.Net > Shared Layout Demo**, and 27 interactive examples. Run
`Compose.Net.Unity.SharedLayout.Tests` in the Edit Mode Test Runner, plus the normal
`Compose.Net.Modifiers.Tests` executable for platform-independent regressions.

`Draggable`, `Scrollable` and `Hoverable` follow the state and interaction lifecycle of
[Compose Draggable](https://github.com/androidx/androidx/blob/androidx-main/compose/foundation/foundation/src/commonMain/kotlin/androidx/compose/foundation/gestures/Draggable.kt),
[Scrollable](https://github.com/androidx/androidx/blob/androidx-main/compose/foundation/foundation/src/commonMain/kotlin/androidx/compose/foundation/gestures/Scrollable.kt) and
[Hoverable](https://github.com/androidx/androidx/blob/androidx-main/compose/foundation/foundation/src/commonMain/kotlin/androidx/compose/foundation/Hoverable.kt).

```csharp
var interactions = Remember(MutableInteractionSource);
var dragged = interactions.CollectIsDraggedAsState().Value;
var drag = RememberDraggableState(delta => offset.Value += delta);
var modifier = Modifier.Draggable(drag, Orientation.Horizontal,
    enabled: true, interactionSource: interactions, startDragImmediately: false,
    onDragStarted: position => Log(position),
    onDragStopped: velocity => Log(velocity), reverseDirection: false);

var scroll = RememberScrollableState(delta =>
{
    var previous = offset.Value;
    offset.Value = Math.Clamp(previous + delta, 0, 500);
    return offset.Value - previous; // Return only the consumed distance.
});
modifier = Modifier.Scrollable(scroll, Orientation.Vertical,
    enabled: true, reverseDirection: false, flingBehavior: new FlingBehavior(0.135f),
    interactionSource: interactions);
modifier = Modifier.Hoverable(interactions, enabled: true);
```

These low-level modifiers do not move, measure, clip or focus the element. The application
applies the reported delta to its own content. Drag deltas follow pointer travel; wheel
scrolling has the opposite sign. `reverseDirection` changes input and release velocity,
not programmatic state operations. `Draggable` supports mouse, pen and touch, touch slop,
immediate capture, local start coordinates, velocity in UI units/second, and touch pointer
handoff. `Scrollable` accepts touch/pen drags, wheel/trackpad input and PageUp/PageDown when
focused; mouse dragging belongs to `Draggable`. It forwards unconsumed input to ancestors,
including native `VerticalScroll`/`HorizontalScroll` containers. A tap produces no drag
interaction. `Hoverable` observes mouse/pen enter/leave without consuming input.

`RememberDraggableState` and `RememberScrollableState` retain identity while updating their
callbacks. `Drag`/`Scroll` blocks expose `DragBy`/`ScrollBy` and a cancellation token.
Equal or higher `MutatePriority` cancels the previous operation; a lower priority is rejected.
Use the scope token for asynchronous work and finish cancellation promptly; native input
that arrives while asynchronous cleanup still owns the state is not replayed later. A completed or cancelled scope rejects further
deltas. `DispatchRawDelta` deliberately bypasses priority, reversal and nested scrolling.
`ScrollableState` exposes progress, availability and last consumed direction, with `ScrollBy`,
`AnimateScrollBy` and `StopScroll` helpers. `ScrollState` also implements `IScrollableState`;
its programmatic animations and low-level mutations share the same priority mechanism.
The old callback-only `Draggable` overload remains obsolete for migration.

Drag starts have matching Stop/Cancel instances; hover enters have matching Exit instances.
Disable, removal, panel detach and capture loss clean up active interactions. Callback
updates preserve an active gesture; replacing its interaction source closes the old pair.
Hover source replacement waits for the next enter event, as in Compose. Shared sources track
all active interactions by reference, so ending one does not clear another's state.

Unity uses `PointerManipulator`, native picking/transforms/capture, pooled bubbling events,
and UI Toolkit value animations for fling. It does not insert a hidden `ScrollView` or run
an additional frame loop. Only the missing state/consumption bridge, axis recognition and
velocity sampling live in the adapter. Platform adaptation: `FlingBehavior` retains the
library's native deceleration configuration; drag callbacks are synchronous C# delegates.
The common `Scrollable(state, orientation, enabled, reverseDirection, flingBehavior,
interactionSource)` overload is supported. Android effect-node/overscroll and
`BringIntoViewSpec` overloads are not represented by ignored parameters.

The Unity project includes **InteractionDemo** in SampleScene and **Tools > Compose.Net >
Interaction Demo**, with 24 examples, live values, event counters and controls for disabling,
removal, callback/source replacement and programmatic changes. Its UI tests are in
`Assets/Tests/Editor/Interaction` (`Compose.Net.Unity.Interaction.Tests`), including all demo
cards, nested input, click arbitration, capture lifecycle, source sharing and touch handoff.
State, priority and collection regressions run with `Compose.Net.Modifiers.Tests`.

`Box` follows the [AndroidX Box contract](https://github.com/androidx/androidx/blob/androidx-main/compose/foundation/foundation-layout/src/commonMain/kotlin/androidx/compose/foundation/layout/Box.kt):

```csharp
Box(modifier: Modifier.SizeIn(minWidth: 120.Dp(), minHeight: 80.Dp()),
    contentAlignment: Alignment.Center, propagateMinConstraints: false,
    content: () =>
    {
        Box(Modifier.Size(160.Dp(), 50.Dp()).Background(Color.DodgerBlue));
        Box(Modifier.Size(40.Dp(), 90.Dp()).Align(Alignment.BottomEnd));
        Box(Modifier.MatchParentSize().TestTag("overlay"));
    });
```

Ordinary children determine the maximum width and height independently and overlap in
composition order. Empty content uses the minimum constraints. `Box(modifier)` is the empty
layout overload. `contentAlignment` defaults to `TopStart`; a child's `Align` overrides it.
`propagateMinConstraints` defaults to false, while incoming maximum constraints still apply.
`MatchParentSize` excludes a child from measurement and sizes it to the resulting inner Box.
`FillMaxSize` participates in measurement and can make the Box fill the available space.
As in Compose, the first `Align` or `MatchParentSize` in a modifier chain supplies the complete
Box parent data. Parent data only affects direct children of a Box.

The Unity adapter uses one native CSS Grid cell for measurement, overlap and all nine
alignments, with absolute native insets for matching overlays. It no longer uses a synthetic
spacer, negative margins or translation to stack children. A constraint bridge handles minimum
propagation, bounded fractional fill and unbounded native scroll content; animated shells retain
their natural target measurement. Picking, drawing order, z-index, text wrapping and scrolling
remain native UI Toolkit operations. Unity 7's **Project Settings > UI Toolkit > Enable Grid
Layout** must be enabled (the sample project includes this setting).

C# keeps required content first. Layout functions and node factories no longer accept a string `type`. Start/End retain the library's
existing LTR conventions; this change does not introduce a layout-direction API. When migrating
an adapter, add `propagateMinConstraints` to `IBoxNodeFactory.Apply` and implement
`IAlignmentModifiersFactory.MatchParentSize`. `IBoxNodeFactory` only creates ordinary boxes. `AnimatedContent`, `AnimatedVisibility`, lazy lists,
grids and pagers delegate through `IComposeAdapter.Layouts` (`IComposeLayouts`). Their composition
and native wrapper creation live in Unity's `UnityComposeLayouts`; state and public APIs remain
in Compose.Net. Custom hosts must provide or forward `Layouts` along with `NodeFactory`.
Unity provides factory overloads in `ComposeUnityFunctions`: `ReusableComposeNode` accepts any
`VisualElement` factory; `Box`, `Row` and `Column` accept factories of their corresponding Unity
view classes, and `Spacer` accepts a `VisualElement` factory. Factories run when the node is created;
modifiers, layout parameters and initializers update on recomposition. `Float` and `Position` remain Unity-specific extensions.

The Unity project includes **Tools > Compose.Net > Box Demo**, an idempotent add-to-scene
command, and active `Samples/BoxDemo` in `Assets/Scenes/SampleScene.unity`. Its 32 examples use
a responsive native Flexbox, native buttons, live measurements and a ScrollView. Run
`Compose.Net.Unity.Box.Tests` for layout, update, input, demo and scene coverage; run
`Compose.Net.Modifiers.Tests` for API/default/adapter forwarding regressions.

`LazyVerticalGrid` and `LazyHorizontalGrid` support fixed/adaptive/fixed-size tracks,
custom spans, keyed content, sticky headers, reverse layout, arrangements and observable
item-based scrolling. Unity uses native `ListView` row virtualization and a bounded
horizontal `ScrollView` window. `GridDemo` includes 24 interactive examples and Edit Mode
UI coverage. See [lazy-grid API, behavior and adapter details](docs/lazy-grids.md).


## LazyColumn and LazyRow

`LazyColumn` and `LazyRow` compose a visible window, with stable keyed item identity and disposal
when items leave that window. `Items(count, ...)` stores an interval; declaring a million items
does not invoke a million content/key delegates. This follows the
[Compose lazy-list DSL](https://github.com/androidx/androidx/blob/androidx-main/compose/foundation/foundation/src/commonMain/kotlin/androidx/compose/foundation/lazy/LazyDsl.kt).

```csharp
var state = RememberLazyListState(initialFirstVisibleItemIndex: 20,
    initialFirstVisibleItemScrollOffset: 8);
LazyColumn(
    state: state,
    modifier: Modifier.Size(320.Dp(), 240.Dp()),
    contentPadding: new PaddingValues(12.Dp()),
    verticalArrangement: Arrangement.SpacedBy(8.Dp()),
    content: list =>
    {
        list.StickyHeader(index => Text("Section"), key: "section");
        list.Items(100_000, index => Text($"Item {index}"),
            key: index => index, contentType: _ => "row");
    });
state.ScrollToItem(50_000, scrollOffset: 12);
```

Both lists accept `contentPadding`, `reverseLayout`, `flingBehavior`, `userScrollEnabled`, and
an optional `overscrollEffect` (`null` explicitly disables it). Existing C# parameter order remains
compatible. `reverseLayout` reverses item order; index zero starts at the bottom/right by default.
Explicit main-axis arrangements still apply to short content. `Arrangement.SpacedBy` supports
spacing, overlap through negative spacing, and optional alignment. Cross-axis size can wrap the
visible content, or be constrained by modifiers. The viewport needs a finite size on its scroll axis.

`ILazyListScope` provides `Item`, `Items(count, ...)`, collection `Items` / `ItemsIndexed`, and
`StickyHeader`; keys must be unique and stable. Inserting before the viewport retains the first
visible key and its offset. `RequestScrollToItem` overrides that anchor for the next layout.
Item content inherits composition locals. `FillParentMaxSize`, `FillParentMaxWidth`, and
`FillParentMaxHeight` fill a fraction (0–1) of the lazy viewport after content padding; use them
inside lazy item content, where ordinary `FillMax*` cannot fill an unbounded scroll axis.

`LazyListState` is publicly constructible. `RememberLazyListState(index, offset)` consumes its
initial arguments once. State exposes `FirstVisibleItemIndex`, `FirstVisibleItemScrollOffset`,
`LayoutInfo.VisibleItemsInfo`, total count, orientation, padding, spacing, viewport and reverse flag.
`ScrollToItem`, `RequestScrollToItem`, and `AnimateScrollToItem` work before measurement and for
uncomposed targets. A positive offset moves the target further out of the leading edge; negative
offsets are allowed. Negative indices and non-finite offsets throw; indices beyond the data clamp.
The existing `ScrollToItemVisibility` helpers and `IScrollState` pixel operations remain available.
Pixel range/total size are estimates until items are measured; use index-based navigation for
unmeasured items. Animation completion waits for target measurement and alignment. A new request
or native gesture cancels the old animation. Disabling user input keeps programmatic scrolling,
measurement, clipping and child controls available.

Unity uses native `ListView` dynamic-height virtualization for columns. UI Toolkit only supplies
vertical collection virtualization, so rows use a sparse measured-size index and a bounded window
on native `ScrollView`. Toolkit owns wheel/touch/keyboard handling, capture, nested scrolling,
inertia and overscroll on both axes. No reflection or custom gesture/physics implementation is used.
The native overscan strategy replaces Compose-specific coroutine prefetch/cache configuration.
C# `FlingBehavior` and `OverscrollEffect` configure Toolkit physics, as for the scroll modifiers.

The Unity project includes `LazyListDemo` (18 interactive cards), a standalone scene, an entry in
`SampleScene → Demos`, editor menu commands, and the `Compose.Net.Unity.Lazy.Tests` panel suite.
Core regression coverage lives in `Compose.Net.Modifiers.Tests/LazyListTests.cs`.

## Navigation regression tests

`NavDisplay` uses `NavEntry.ContentKey` to identify screen content. Replacing route
arguments with the same content key updates that screen while preserving its
`Remember` state. Give separate screen instances distinct keys. By default,
entries still present in the back stack retain their composition; popped entries
are disposed after their exit animation. Set `retainEntries: false` to dispose
covered screens as well. Decorators receive `OnPop` once per removed content key,
including when the stack is cleared.

Run `dotnet run --project Compose.Net.Modifiers.Tests -c Release` for navigation
regressions covering stack operations, providers, push/pop, interrupted transitions,
retention, decorators, empty stacks, hash collisions and content-key updates.
The companion Unity project's `Compose.Net.Unity.Navigation.Tests` Edit Mode
assembly tests the actual `NavigationSample`, including button clicks, labels,
counter retention, settings reset, product routes and background/text draw order.


The shared-transition audit and regression coverage are described in [docs/shared-transitions.md](docs/shared-transitions.md).
