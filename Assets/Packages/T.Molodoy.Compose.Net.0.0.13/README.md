# Compose.Net

.Net implementation of Compose.

Composable content supports both explicit lambdas and trailing `using` blocks:

```csharp
Column(content: () =>
{
    Text("Explicit content");
});

using (Column(modifier: Modifier.FillMaxSize()))
using (Row())
{
    Text("Trailing content");
}
```

The generator lowers a trailing block into the original method's content lambda
before processing composition groups. The component still controls whether, when,
and how many times to invoke its content. Both forms can be freely nested, and
`ColumnScope`, `RowScope`, and `BoxScope` apply to the trailing block.

For a method with multiple delegate parameters, mark the block parameter with
`[TrailingLambda]`; the other callbacks remain ordinary arguments:

```csharp
[Composable]
public void Button(Action onClick, [Composable, TrailingLambda] Action content)
{
    // ...
}

// Inside a [Composable] method:
using (Button(onClick: Save))
{
    Text("Save");
}
```

A trailing lambda can receive arguments through a typed `Value` property:

```csharp
using (var content = SharedTransitionLayout())
{
    SharedTransitionScope scope = content.Value;
    // ...
}

using (var content = AnimatedContent(state.Value))
{
    var (scope, value) = content.Value;
    Text(value.ToString());
}
```

For one lambda argument, `Value` has that argument's type. For multiple arguments,
it is a tuple in delegate parameter order, including tuples with more than seven
elements. A single tuple argument remains a single argument. The marker and its
`Value` reads are replaced during generation; closures capture the actual lambda
arguments. The marker itself cannot be copied, passed elsewhere or disposed.

A single delegate parameter is selected automatically if it is a required,
void composable delegate with by-value arguments. Explicit selection has the same
requirements. The containing composable method must return `void`, and its types
must be `partial`. Methods with `params` arguments and optional content parameters
do not receive trailing overloads. Keeping optional-content calls unchanged avoids
changing existing overload resolution.
Trailing overloads are also omitted when their signatures would collide with an
existing overload or another generated trailing overload; explicit calls continue
to work in those cases. An explicitly marked `[TrailingLambda]` overload takes
precedence over automatic selection when their trailing signatures coincide.
`AnimatedContent` marks its `(AnimatedVisibilityScope, T)` content overload, so its
`Value` is `(scope, state)`; both explicit-lambda overloads remain available.

Use `using (Component(...)) { ... }` or
`using (var content = Component(...)) { ... }`, including chained `using` statements.
Unbracketed `using var content = ...;`, `await`/`yield` in the content block, and control flow that leaves
the block are rejected with `CN_TRAILING_LAMBDA`. C# also disallows jumping into
the block. Exits inside nested
loops, local functions, and lambdas keep their normal meaning. Trailing blocks
require the source generator and the same assembly patching as explicit content.

When updating a Unity project from a local build, replace both
`Assets/Packages/Compose.Net.Unity/Assemblies/Compose.Net.dll` and
`Assets/Analyzers/Compose.Net.CodeGen.dll`. The trailing overloads live in the
runtime DLL, while block lowering lives in the analyzer DLL; updating only one
does not enable the new syntax. Keep their existing Unity `.meta` files.

`LazyColumn` and `LazyRow` accept `contentPadding`, `reverseLayout`,
`flingBehavior`, and `userScrollEnabled`. Omitting `overscrollEffect` uses
`RememberOverscrollEffect()`; pass `overscrollEffect: null` to disable it.
Existing argument positions are preserved, with `content` first as on other
Compose.Net layouts.

```csharp
LazyColumn(
    modifier: Modifier.FillMaxSize(),
    contentPadding: new PaddingValues(All: 16.Dp()),
    reverseLayout: false,
    content: scope =>
    {
        scope.Item(content: () => Text("Messages"), key: "header", contentType: "header");
        scope.Items(messages,
            itemContent: message => Text(message.Text),
            key: message => message.Id,
            contentType: _ => "message");
        scope.ItemsIndexed(messages,
            itemContent: (index, message) => Text($"{index}: {message.Text}"));
    });
```

The collection overloads accept `IEnumerable<T>`, including lists and arrays.
The count overload remains `scope.Items(count, content, key, contentType)`.
Keys must be unique and preserve item composition identity when reordered; changing `contentType`
recreates that item's composition. `contentPadding` scrolls with the content,
and `reverseLayout` reverses both item placement and scrolling, with item zero
at the bottom/right. Explicit arrangements are respected.

The list builder is an ordinary delegate; use an explicit `content` lambda.
Item content is composable. The current implementation composes all items and
does not yet provide virtualization, sticky headers, `LazyItemScope`, or
automatic scroll anchoring by item key. These are differences from the
[Jetpack Compose lazy list API](https://developer.android.com/reference/kotlin/androidx/compose/foundation/lazy/package-summary).

Regression checks:

```text
dotnet run -c Release --project Compose.Net.CodeGen.Tests
dotnet run -c Release --project Compose.Net.Modifiers.Tests
dotnet run -c Release --project Compose.Net.Modifiers.Tests -- --lazy
```

Use Release DLLs in Unity. From this repository, run
`powershell -ExecutionPolicy Bypass -File Tools/Sync-UnityRelease.ps1`
(`-UnityProjectDirectory <path>` selects another Unity project). The script builds
the runtime, navigation, Kaspresso, patcher, and analyzer in Release, checks their
assembly configuration and JIT optimization flags, and updates Unity DLLs while
preserving `.meta` files. It also checks bundled StableCollections and
SharpExtensions DLLs, including the SharpExtensions analyzer. Embedded debug
symbols are retained for diagnostics and do not disable Release optimizations.

UI tests can use [Compose.Net.Kaspresso](Compose.Net.Kaspresso/README.md), a small
Kaspresso-inspired DSL with Kakao-style typed views, scoped list selectors,
screen objects, steps, retries, cleanup hooks and a Unity UI Toolkit driver.

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

Scroll modifiers follow the Compose argument order:

```csharp
var state = RememberScrollState(initial: 0);
Modifier.VerticalScroll(state, enabled: true, flingBehavior: null, reverseScrolling: false);
Modifier.HorizontalScroll(state, enabled: true, flingBehavior: null, reverseScrolling: false);
```

`enabled` controls user input; `ScrollTo` and `AnimateScrollTo` still work when it
is false. `reverseScrolling` places position zero at the bottom/right edge.
`RememberScrollState(initial)` remembers the initial pixel offset once. State exposes
`MaxValue`, `CanScrollForward`, `CanScrollBackward`, and an interaction source.
The Unity adapter observes state changes directly, so scrolling does not require
recomposition. Wheel events bubble to enclosing scroll containers at the boundary.
Custom `IFlingBehavior` implementations receive an `IScrollScope` with consumed
pixel deltas and return the remaining velocity. Wheel speed is handled by the adapter.
Existing scroll modifier calls using `userScrollEnabled` should use `enabled`;
`scrollMultiplier` and modifier-level `interactionSource` are no longer arguments.

Scroll modifiers use `LocalOverscrollFactory` by default. The default factory creates
an elastic effect for drag/fling input; mouse wheel scrolling stays bounded.
Use `RememberOverscrollEffect()` to remember an effect from the current factory.
The explicit overload accepts `overscrollEffect` after `state`; passing `null`
disables the effect for that container:

```csharp
Modifier.VerticalScroll(state, overscrollEffect: null);
Modifier.HorizontalScroll(state, overscrollEffect: RememberOverscrollEffect());
```

Provide `LocalOverscrollFactory.Provides(null)` to disable default overscroll in a
subtree. Custom effects implement `IOverscrollEffect`; `IOverscrollDisplacement`
is the Unity visual bridge for moving content without changing the bounded state.
The visual implementation is Unity elastic displacement, not Android's native glow
or stretch node; the full Compose `OverscrollEffect.node` rendering API is not implemented.

### Side effects and saveable navigation

`SideEffect(() => ...)` runs in registration order after a successful composition
or recomposition, after the composer has left the composition context. Pending
effects are discarded if composition fails, including effects registered by earlier
siblings in the same invalidation pass. The existing `SideEffect(key, action)`
overload remains available for compatibility; it runs after a successful composition
only on first use or when its key changes.

`RememberSaveableStateHolderNavEntryDecorator<T>()` remembers its decorator and
holder, scopes `RememberSaveable` values to each entry's `ContentKey`, and removes
saved state on `OnPop`. It also accepts an existing `ISaveableStateHolder`.
A screen can leave composition and later return with its saveable values restored;
ordinary `Remember` values still follow the composition lifetime.

```csharp
// Inside a decorated entry's composable content:
var count = RememberSaveable(() => MutableStateOf(0));
SideEffect(() => PublishCount(count.Value));
```

For custom values, pass `saver: new Saver<T, TSaved>(save, restore)`; for a mutable
state's value, use the `stateSaver` parameter. `RememberSaveableStateHolder()` and
`ISaveableStateHolder.SaveableStateProvider(key, content)` are also usable without
navigation. A holder key may be active only once at a time.

`SaveableStateRegistry(restoredValues, canBeSaved)` and
`LocalSaveableStateRegistry` let a host save and restore an entire subtree using
`registry.PerformSave()`. The default registry stores object snapshots in memory.
Persistence across application restarts requires the host to serialize those
snapshots and provide the restored registry; there is no automatic Unity disk
storage. The host's `canBeSaved` predicate must support its snapshot format,
including the arrays and dictionaries used for mutable state and nested holders.

Pagers use the same scroll adapter as scroll containers, including pointer dragging
and the mouse wheel. Releasing a drag snaps to the nearest page; a fast swipe
advances in its direction. Wheel input also settles on a page.

```csharp
var state = RememberPagerState(() => pages.Count, initialPage: 0);
HorizontalPager(state, page => Text(pages[page]),
    modifier: Modifier.FillMaxSize(),
    userScrollEnabled: true,
    reverseLayout: false,
    pageSize: PageSize.Fill,
    pageSpacing: 8.Dp(),
    key: page => pages[page].Id);
```

`VerticalPager` has the same options, with `horizontalAlignment` instead of
`verticalAlignment`. `PageSize.Fixed` specifies the actual page size; padding is
outside pages. Pages snap at the start of the padded viewport, using page size
plus spacing as the scroll stride. `userScrollEnabled: false` disables user input
while keeping programmatic scrolling available. `flingBehavior` can override the
default page snapping.

`IPagerState` exposes `CurrentPage`, `CurrentPageOffsetFraction`, `SettledPage`,
`TargetPage`, `PageCount`, scroll directions and `IsScrollInProgress`.
`CurrentPage` follows the nearest page during a drag or animation; `SettledPage`
changes when scrolling ends. `ScrollToPage` and `AnimateScrollToPage` accept
`pageOffsetFraction` in `[-0.5, 0.5]`. Page indices clamp to the available range.
The remembered page-count callback is refreshed on recomposition.

The pager currently composes all pages. AndroidX virtualization,
`beyondViewportPageCount`, `PagerScope`, configurable `SnapPosition`, and the
pager-specific nested-scroll API are not implemented.

Run pager regressions with:
`dotnet run -c Release --project Compose.Net.Modifiers.Tests -- --pager`.

### Remember with multiple keys

`Remember` accepts two through eight typed keys before `defaultValueFactory`:

```csharp
var value = Remember(userId, filter, () => BuildValue(userId, filter));
```

The generator combines the keys into one typed `ValueTuple` and stores/checks it
as a single key. The factory runs initially and when that composite key changes.
Key expressions evaluate once in source order, including reordered named
arguments. The existing zero-key, single-key, and explicit tuple-key forms remain
available.

Pagers accept `overscrollEffect` with Compose semantics: omit it to use
`RememberOverscrollEffect()` and `LocalOverscrollFactory`, pass an effect to use
that instance, or pass `overscrollEffect: null` to disable it. The pager applies
and renders the effect internally; no extra modifier is needed.

Page and content sizes are pushed to native layout nodes during the geometry
pass. `PageSize.Fill` does not wait for a second composition after measurement.

### Animation specifications

AnimationSpec supports Tween, Spring, Keyframes, Snap, Repeatable and
InfiniteRepeatable. Existing Tween calls and the global AnimationSpec.Default
remain compatible. AnimateFloatAsState and Animatable use Spring when their spec
is omitted.

```csharp
var position = AnimateFloatAsState(target, Spring(dampingRatio: 0.5f, stiffness: 100f));
var pulse = InfiniteRepeatable(Tween(duration: 600), RepeatMode.Reverse);
var staged = Keyframes(1000,
    new Keyframe(200, 0.7f, LinearEasing),
    new Keyframe(600, 0.7f));
var delayedJump = Snap(delay: 150);
```

Keyframe values are normalized interpolation fractions, rather than absolute
values of the animated type. Fractions outside 0..1 support overshoot; missing
endpoints default to 0 and 1. Easing on a keyframe applies to the following segment.
The caller's keyframe array is copied. Repeatable requires a finite duration-based
spec and a positive iteration count. Reverse retraces the curve; an even number
of reverse iterations finishes at the initial value. InfiniteRepeatable runs until
cancellation. TotalDuration is TimeSpan.MaxValue for infinite specs.

Spring uses the analytic damped oscillator for underdamped, critically damped and
overdamped cases. Damping, stiffness and visibility threshold must be finite and
positive. Settling checks displacement and velocity; settings that cannot settle
within 120 seconds are rejected. Generic interpolation and transition APIs sample
a normalized spring; the float Animatable uses the actual displacement and velocity.

```csharp
var motion = Remember(() => new Animatable(0f));
LaunchedEffect(target, scope => motion.AnimateTo(target,
    Spring(dampingRatio: 0.6f), cancellationToken: scope.Token));
Text(motion.Value.ToString());
```

Animatable exposes Value, Velocity, TargetValue and IsRunning, supports SnapTo and
Stop, and cancels a previous AnimateTo when retargeted. Float spring retargeting
preserves position and velocity, including in AnimateFloatAsState. Multi-component
velocity tracking with AnimationVector/TwoWayConverter is not implemented.

### Effect lifecycle

LaunchedEffect (both Task and time-coroutine forms) and DisposableEffect defer
setup until composition succeeds. They keep their committed key and resource
separate from speculative arguments. An unchanged key keeps the existing resource;
changing keys first cancels/disposes old resources, then starts new effects. All
these lifecycle callbacks run outside the composition context, before SideEffect.
Failed composition discards pending setups and keeps committed effects alive.
Successful removal and composer disposal clean resources up exactly once. If a
cleanup throws, other resources are still cleaned and errors are reported together.

Both effects accept two through eight typed keys as well as the original single
key. Multiple keys are combined into one ValueTuple:

```csharp
LaunchedEffect(userId, filter, async scope => await Refresh(scope.Token));
DisposableEffect(source, subscriptionId, () =>
{
    var subscription = Subscribe(source, subscriptionId);
    return OnDispose(subscription.Dispose);
});
```

The slot table itself is not transactional: after a failed structural change,
detached committed resources are retained until the next successful apply or
composer disposal. This effect lifecycle does not roll back arbitrary state
mutations performed by composable content.
