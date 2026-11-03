namespace Compose.Net.Unity;

internal record VerticalScrollModifierImpl(IScrollState State, bool Enabled, FlingBehavior? FlingBehavior,
    bool ReverseScrolling, OverscrollEffect? OverscrollEffect)
    : ScrollModifierImpl(State, Enabled, FlingBehavior, ReverseScrolling, OverscrollEffect, false);
