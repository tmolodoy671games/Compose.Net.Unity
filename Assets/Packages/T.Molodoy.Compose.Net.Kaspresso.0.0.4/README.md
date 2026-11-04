# Compose.Net.Kaspresso

Platform-independent UI test DSL targeting .NET Standard 2.1. No engine or test-runner dependencies.
An adapter implements `ITestDriver` and `ITestNode`; assertions throw `KaspressoAssertionException`.

```csharp
var test = new Kaspresso(driver);
test.Step("Submit", () => test.OnNodeWithTag("submit").AssertIsEnabled().Click());
yield return test.WaitUntil(() => test.OnNodeWithTag("status").AssertText("Saved"));
```

Selectors are resolved against the current tree on every call, so they survive recomposition.
Single-node actions and assertions reject duplicate matches. Use `AssertCount` for collections
or `DescendantWithTag` to scope a selector to a unique parent. `AssertIsNotDisplayed` requires
an existing hidden node; `AssertDoesNotExist` checks absence. Text matching is exact and ordinal.
`ScrollTo` works with materialized nodes; it does not materialize virtualized list entries.

`WaitUntil` is a coroutine: the host must advance it once per frame on the UI thread.
It retries only assertion failures, measures timeout with a monotonic wall clock, and
preserves the last assertion as the inner exception. Other exceptions fail immediately.
Keep its callback free of side effects; clicks and other actions are never retried automatically.
A zero timeout performs one check. `KScreen` is a base for application-specific screen objects.

Run regression checks with `dotnet run --project Compose.Net.Kaspresso.Tests -c Release`.
