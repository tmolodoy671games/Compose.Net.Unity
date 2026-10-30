# Compose.Net.Kaspresso

A small, independent C# testing DSL for Compose.Net, inspired by
[Kaspresso](https://github.com/KasperskyLab/Kaspresso) and its
[Kakao](https://github.com/KakaoCup/Kakao) view DSL. It includes typed screen and
view objects, matcher blocks, scoped child/list selectors, before/after sections,
named steps, reusable scenarios and assertion retries.
It is not affiliated with Kaspersky and is not an implementation of Android
Espresso, UI Automator, screenshot comparison, device control or Allure.

The library targets `netstandard2.1`, has no external dependencies, and can be
called from NUnit or another test runner. Test failures are exceptions; no
test-runner base class is required.

## Kakao-style views and screen blocks

Use `KScreen<TSelf>` and `PerformAsync` to group operations with a typed receiver:

```csharp
sealed class FormScreen : KScreen<FormScreen>
{
    public KEditText Email { get; }
    public KButton Submit { get; }

    public FormScreen(Kaspresso ui) : base(ui)
    {
        Email = new KEditText(ui, b => b.WithTestTag("email"));
        Submit = new KButton(ui, b => b.WithTestTag("submit").WithText("Send"));
    }
}

// In an async test:
// await new FormScreen(ui).PerformAsync(async form =>
// {
//     await form.Email.PerformAsync(async email =>
//     {
//         await email.ReplaceTextAsync("user@example.com");
//         await email.ContainsTextAsync("@example.com");
//     });
//     await form.Submit.ClickAsync();
// });
```

`KView` provides general actions/checks; `KTextView` and `KButton` add text
assertions (`HasTextAsync`, `ContainsTextAsync`, `HasAnyTextAsync`,
`HasEmptyTextAsync`); `KEditText` also has `TypeTextAsync` and `ClearTextAsync`.
`TypeTextAsync` appends through the backend's text replacement operation, rather
than simulating individual key events. These wrappers express roles in your
Compose UI; they do not require a particular host widget type for lookup.
Text editing support depends on the backend driver.

Each condition in a `ViewBuilder` block is combined with AND. `WithMatcher`
accepts a custom `NodeMatcher`; `WithParent`, `WithAncestor`, and `WithDescendant`
accept nested matcher blocks. `MatchesAsync` checks a matcher against the current
view including its live ancestry. An empty matcher block is rejected.

```csharp
// Duplicate title tags in different cards are disambiguated by the parent scope.
var card = new KView(ui, b => b.WithTestTag("profile-card"));
var title = new KTextView(card, b => b.WithTestTag("title"));
await title.HasTextAsync("Profile");
await title.MatchesAsync(b => b.WithAncestor(a => a.WithTestTag("profile-card")));
```

Scoped view constructors and `KNode.Child(matcher)` search all descendants of
exactly one parent, excluding the parent itself. Parent scopes and ancestors are
resolved again on each operation. `WithParent` uses the immediate backend parent;
use `WithAncestor` to look through containers introduced by Compose modifiers.
A missing or ambiguous parent fails, including for child absence checks.

`KList` adds `ChildAt` (zero-based), `FirstChild`, `LastChild`, `ChildWith`, and
`HasSizeAsync`. Without an item matcher these operate on immediate children.
With an `items` matcher they operate on matching descendants in depth-first
hierarchy order, so host wrapper elements can be skipped:

```csharp
var products = new KList(ui, b => b.WithTestTag("products"),
    items: b => b.WithTestTag("product-item"));
await products.HasSizeAsync(2);
await products.ChildAt(0).IsDisplayedAsync();
await products.ChildWith(b => b.WithDescendant(d => d.WithText("Apple")))
    .IsDisplayedAsync();
```

Define custom item/view objects by extending `KBaseView<TSelf>` and accepting
a `KNode` query in the constructor. Child properties can use that object as their
scope. Pass an item factory to list accessors, for example
`products.ChildAt(0, query => new ProductItem(query))`. Item objects retain queries,
so their children follow replacements and reordering on the next operation.
All list operations cover currently materialized items; they do not scroll or
instantiate virtualized rows. `ChildWith` must match exactly one item.

For custom widgets, `AssertAsync(predicate, expectation)` retries a predicate;
`ActAsync(action)` executes a custom backend action once after readiness checks.
Both receive an `IComposeTestNode`. `KBaseView<TSelf>` supports adding your own
typed methods. View/screen `PerformAsync` blocks execute once; their individual
checks retain the usual retries. Pass cancellation tokens into individual
operations as well as the block when you need cancellation during a wait.

## Selectors and checks

`OnNodeWithTag` uses the existing `Modifier.TestTag`; how that maps to host
element identity is defined by the driver. Names set outside Compose also match
when the driver exposes them. `OnNodeWithText` matches the driver's text surface.
Searches include the root and all hierarchy descendants, including modifier
wrapper containers.

Combine selectors with `NodeMatcher.HasTestTag("title").And(NodeMatcher.HasText("Hello"))`.
`Or`, `Not`, and custom predicates via `new NodeMatcher(description, predicate)`
are also available. Predicates should be side-effect free.

Each operation resolves its selector again, so a screen object survives replaced
nodes and recomposition. A selector must match exactly one element for checks
and actions; duplicate matches fail rather than picking an arbitrary element.

- `AssertExistsAsync`, `AssertDoesNotExistAsync`
- `AssertIsDisplayedAsync`, `AssertIsNotDisplayedAsync`
- `AssertIsEnabledAsync`, `AssertIsDisabledAsync`, `AssertHasTextAsync`
- `ClickAsync`, `ReplaceTextAsync`

`AssertIsNotDisplayedAsync` requires one existing hidden node. Use
`AssertDoesNotExistAsync` to check removal. Visibility semantics are defined by
the driver (typically attachment, bounds, effective visibility and ancestor
display); they do not necessarily detect occlusion, scroll clipping or hit-test
accessibility. `ReplaceTextAsync` support depends on the driver; Compose text
labels are read-only. Click sends a pointer down/up pair to the selected element
to reach Compose's `PointerInput`/`Clickable` implementation; it is synthetic
input, not an OS-level mouse action.

## Retries, cancellation and cleanup

Assertions and action readiness checks retry for five seconds by default with
a 50 ms interval. Configure these in `new Kaspresso(driver, timeout, pollInterval)`.
A zero timeout performs one attempt. Only `KaspressoAssertionException` is
retried; programming errors, unsupported actions and cancellation propagate.
Once a target is ready, actions run once, followed by a driver idle wait.

```csharp
await test.FlakySafelyAsync(async () =>
{
    await screen.Value.AssertHasTextAsync("Loaded", test.CancellationToken);
}, timeout: System.TimeSpan.FromSeconds(10));
```

An explicit `FlakySafelyAsync` block owns the retry budget of its nested
assertions. Put checks in these blocks: retrying a block containing a click can
repeat the click. Timeouts bound retries, not the execution of a user callback
or a driver operation that never completes. Pass cancellation tokens to node
operations for cancellation during their waits.

`Before`/`After` accept synchronous actions; `BeforeAsync`/`AfterAsync` accept
`Func<TestContext, Task>`. Hooks run in registration order. All cleanup hooks run
even when setup fails or the test is canceled, using an uncanceled cleanup
context. A single failure keeps its original exception and stack; multiple
failures are preserved in an `AggregateException` with the test/setup error first.

`StepAsync` supports sequential nested steps, logs start/pass, and attaches the
step path and hierarchy to failures. `IScenario.RunAsync(TestContext)` allows
reusable groups of steps, invoked via `test.ScenarioAsync(scenario)`. Each run
has a separate context; run steps sequentially within that context.

## Other backends and validation

Implement `IComposeTestDriver` and `IComposeTestNode` for another Compose.Net
backend or a headless fixture. Keep reads live, provide actual visibility and
interaction semantics, and let `WaitForIdleAsync` advance that backend's work.

Run the dependency-free regression executable:

```text
dotnet run --project Compose.Net.Kaspresso.Tests -p:NuGetAudit=false
```
