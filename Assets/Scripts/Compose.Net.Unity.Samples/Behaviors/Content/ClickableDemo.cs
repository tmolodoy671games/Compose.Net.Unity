using System;
using Compose.Net;

namespace Compose.Net.Unity.Samples.Behaviors.Content
{
    /// <summary>Attach beside a UIDocument containing ComposeView, or open Tools/Compose.Net/Clickable Demo.</summary>
    public partial class ClickableDemo : ComposeUI
    {
        [Composable] protected override void Content() => Screen(Remember(() => new ClickableDemoState()));
        [Composable] protected override void Preview() => Screen(Remember(() => new ClickableDemoState()));

        // The component, Editor window and UI tests all mount this exact screen.
        [Composable]
        public static void Screen(ClickableDemoState state)
        {
            Observe(state.Tracked);
            Observe(state.Silent);
            Observe(state.Custom);
            Observe(state.Lifetime);
            Observe(state.Shared);
            var scroll = RememberScrollState();
            Column(modifier: Modifier.FillMaxSize().Background(new Color(0.055f, 0.07f, 0.11f))
                    .VerticalScroll(scroll).Padding(24.Dp()).TestTag("clickable-demo"), content: () =>
            {
                Text("Modifier.Clickable", color: Color.White, fontSize: 30.Sp());
                Text("Press, hold, release and hover. Each example has independent counters.",
                    color: Color.LightGray, fontSize: 15.Sp());
                Spacer(Modifier.Height(16.Dp()));
                Row(content: () =>
                {
                    Card("01 / Default", "Short overload · LocalIndication", () =>
                    {
                        Target("basic", "Click me", state.Basic.Click, count: state.Basic.Count.Value);
                    });
                    Card("02 / Enabled", "Short overload · same callback when toggled", () =>
                    {
                        Target("enabled", state.Enabled.Value ? "Enabled" : "Disabled", state.EnabledCase.Click,
                            state.Enabled.Value, state.EnabledCase.Count.Value);
                        Control("toggle-enabled", "Toggle enabled", state.ToggleEnabled);
                    });
                    Card("03 / Explicit source", "Ripple · press / release / cancel / hover", () =>
                    {
                        TrackedTarget("tracked", state.Tracked, LocalIndication.Current, state.TrackedEnabled.Value);
                        Control("toggle-tracked", "Toggle enabled", state.ToggleTracked);
                    });
                });
                Spacer(Modifier.Height(12.Dp()));
                Row(content: () =>
                {
                    Card("04 / No indication", "indication: null · explicit interaction source", () =>
                        TrackedTarget("silent", state.Silent, null));
                    Card("05 / Both null", "No indication · source remembered internally", () =>
                    {
                        Target("implicit", "Click without feedback", state.Implicit.Click,
                            count: state.Implicit.Count.Value, explicitNulls: true);
                    });
                    Card("06 / Custom indication", "Blue hover · amber press · clipped overlay", () =>
                        TrackedTarget("custom", state.Custom, DemoHighlightIndication.Instance));
                });
                Spacer(Modifier.Height(12.Dp()));
                Row(content: () =>
                {
                    Card("07 / LocalIndication", "Short overload inherits the local highlight", () =>
                    {
                        CompositionLocalProvider(LocalIndication.Provides(DemoHighlightIndication.Instance), () =>
                            Target("local", "Inherited highlight", state.Local.Click, count: state.Local.Count.Value));
                    });
                    Card("08 / Latest callback", "Switch between +1 and +10 after composition", () =>
                    {
                        Target("callback", state.LargeStep.Value ? "Add 10" : "Add 1",
                            state.LargeStep.Value ? state.AddTen : state.AddOne, count: state.Callback.Count.Value);
                        Control("toggle-callback", "Switch callback", state.ToggleCallback);
                    });
                    Card("09 / Lifetime", "Remove / restore a target, including while held", () =>
                    {
                        if (state.Visible.Value)
                            TrackedTarget("lifetime", state.Lifetime, LocalIndication.Current);
                        else
                            Text("Target removed", color: Color.LightGray, modifier: Modifier.TestTag("lifetime-absent"));
                        Control("toggle-visible", "Remove / restore", state.ToggleVisible);
                    });
                });
                Spacer(Modifier.Height(12.Dp()));
                Row(content: () =>
                {
                    Card("10 / Shared source", "Two targets share hover and press feedback", () =>
                    {
                        Row(content: () =>
                        {
                            Box(modifier: TargetStyle("shared-a", 125).Clickable(state.Shared.Click,
                                DemoHighlightIndication.Instance, state.Shared.Source), contentAlignment: Alignment.Center,
                                content: () => Label("First", "shared-a.label"));
                            Spacer(Modifier.Width(8.Dp()));
                            Box(modifier: TargetStyle("shared-b", 125).Clickable(state.SharedSecond.Click,
                                DemoHighlightIndication.Instance, state.Shared.Source), contentAlignment: Alignment.Center,
                                content: () => Label("Second", "shared-b.label"));
                        });
                        Counter("shared-a", state.Shared.Count.Value);
                        Counter("shared-b", state.SharedSecond.Count.Value);
                        Status("shared", state.Shared);
                    });
                    Card("11 / Recomposition", "Change the modifier prefix without losing input", () =>
                    {
                        var prefix = TargetStyle("recompose");
                        if (state.Decorated.Value)
                            prefix = prefix.Scale(0.9f);
                        Box(modifier: prefix.Clickable(state.Recompose.Click), contentAlignment: Alignment.Center,
                            content: () => Label(state.Decorated.Value ? "Scaled to 90%" : "Original size", "recompose.label"));
                        Counter("recompose", state.Recompose.Count.Value);
                        Control("toggle-decoration", "Change modifier prefix", state.ToggleDecoration);
                    });
                    Card("12 / Disabled from start", "Both overloads · no click or interaction", () =>
                    {
                        Target("disabled-short", "Short overload", state.Disabled.Click, false, state.Disabled.Count.Value);
                        Box(modifier: TargetStyle("disabled-explicit").Alpha(0.45f).Clickable(state.Disabled.Click,
                            DemoHighlightIndication.Instance, state.Disabled.Source, enabled: false),
                            contentAlignment: Alignment.Center, content: () => Label("Explicit overload", "disabled-explicit.label"));
                    });
                });
                Spacer(Modifier.Height(12.Dp()));
                Text("The counters count on release. Hold a target to inspect Pressed; move away to inspect Hovered.\n" +
                    "Keyboard activation, double-click recognition and long-click callbacks are outside this API.",
                    color: Color.LightGray, fontSize: 14.Sp(), modifier: Modifier.TestTag("clickable-help"));
            });
        }

        [Composable]
        private static void Observe(ClickableDemoCase item)
        {
            LaunchedEffect(item.Source, async scope =>
                await item.Source.Interactions.Collect(item.Record, scope.Token));
        }

        [Composable]
        private static void Card(string title, string description, [Composable] Action content)
        {
            Column(modifier: Modifier.Width(310.Dp()).HeightIn(min: 310.Dp()).Padding(8.Dp())
                .Background(new Color(0.10f, 0.13f, 0.19f), RoundedCornerShape(12.Dp())).Padding(14.Dp()), content: () =>
            {
                Text(title, color: Color.White, fontSize: 18.Sp());
                Text(description, color: Color.LightGray, fontSize: 12.Sp(), modifier: Modifier.Height(38.Dp()));
                content();
            });
            Spacer(Modifier.Width(12.Dp()));
        }

        private static IModifier TargetStyle(string tag, float width = 0) =>
            (width > 0 ? Modifier.Width(width.Dp()) : Modifier.FillMaxWidth()).HeightIn(min: 44.Dp(), max: 44.Dp())
            .Clip(RoundedCornerShape(8.Dp())).Background(new Color(0.19f, 0.30f, 0.48f)).TestTag(tag);

        [Composable]
        private static void Label(string text, string tag) =>
            Text(text, color: Color.White, fontSize: 15.Sp(), modifier: Modifier.TestTag(tag));

        [Composable]
        private static void Target(string tag, string label, Action onClick, bool enabled = true,
            int count = 0, bool explicitNulls = false)
        {
            var modifier = TargetStyle(tag).Alpha(enabled ? 1 : 0.45f);
            modifier = explicitNulls ? modifier.Clickable(onClick, null, null, enabled) : modifier.Clickable(onClick, enabled);
            Box(modifier: modifier, contentAlignment: Alignment.Center, content: () => Label(label, tag + ".label"));
            Counter(tag, count);
        }

        [Composable]
        private static void TrackedTarget(string tag, ClickableDemoCase item, IIndication? indication, bool enabled = true)
        {
            Box(modifier: TargetStyle(tag).Alpha(enabled ? 1 : 0.45f).Clickable(item.Click, indication, item.Source, enabled),
                contentAlignment: Alignment.Center, content: () => Label(enabled ? "Press and hold" : "Disabled", tag + ".label"));
            Counter(tag, item.Count.Value);
            Status(tag, item);
        }

        [Composable]
        private static void Counter(string tag, int count) =>
            Text($"Clicks: {count}", color: Color.White, fontSize: 14.Sp(), modifier: Modifier.TestTag(tag + ".count"));

        [Composable]
        private static void Status(string tag, ClickableDemoCase item)
        {
            var pressed = item.Source.CollectIsPressedAsState().Value;
            var hovered = item.Source.CollectIsHoveredAsState().Value;
            Text($"Pressed: {pressed} · Hovered: {hovered}", color: Color.LightGray, fontSize: 12.Sp(),
                modifier: Modifier.TestTag(tag + ".state"));
            Text($"Press {item.Presses.Value} / Release {item.Releases.Value} / Cancel {item.Cancels.Value}",
                color: Color.LightGray, fontSize: 12.Sp(), modifier: Modifier.TestTag(tag + ".events"));
        }

        [Composable]
        private static void Control(string tag, string label, Action action) =>
            Box(modifier: Modifier.FillMaxWidth().HeightIn(min: 30.Dp(), max: 30.Dp()).Background(new Color(0.22f, 0.24f, 0.30f))
                .TestTag(tag).Clickable(action), contentAlignment: Alignment.Center, content: () => Label(label, tag + ".label"));
    }
}
