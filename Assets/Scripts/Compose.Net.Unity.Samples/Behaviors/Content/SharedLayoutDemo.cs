using System;
using Compose.Net;
using static Compose.Net.ComposeFunctions;

namespace Compose.Net.Unity.Samples.Behaviors.Content
{
public partial class SharedLayoutDemo : ComposeUI
{
    [Composable] protected override void Content() => Screen(Remember(() => new SharedLayoutDemoState()));
    [Composable] protected override void Preview() => Screen(Remember(() => new SharedLayoutDemoState()));
    [Composable] public static void Screen(SharedLayoutDemoState state)
    {
        Column(modifier: Modifier.FillMaxSize().Background(new Color(.035f, .05f, .08f)).VerticalScroll(RememberScrollState()).Padding(16.Dp()), content: () =>
        {
            Text("SharedLayout", color: Color.White, fontSize: 32.Sp());
            Text("27 interactive examples · A → B → C, reverse at any point · native UI Toolkit overlay", color: Color.LightGray, fontSize: 14.Sp());
            Row(content: () => { Control("sl.all-a", "All → A", () => state.SelectAll(0)); Control("sl.all-b", "All → B", () => state.SelectAll(1)); Control("sl.all-c", "All → C", () => state.SelectAll(2)); });
            for (var row = 0; row < 9; row++)
            {
                var index = row * 3;
                Row(content: () => { Card(state.Cases[index]); Card(state.Cases[index + 1]); Card(state.Cases[index + 2]); });
            }
        });
    }
    [Composable] private static void Card(SharedLayoutCase item)
    {
        var revision = item.Revision.Value;
        Column(modifier: Modifier.Size(350.Dp(), 300.Dp()).Padding(6.Dp()).Background(new Color(.08f, .11f, .16f), RoundedCornerShape(10.Dp())).Padding(10.Dp()), content: () =>
        {
            Text(item.Title, color: Color.White, fontSize: 18.Sp());
            Text(item.Description, color: Color.LightGray, fontSize: 12.Sp(), modifier: Modifier.Height(40.Dp()));
            SharedTransitionLayout(shared =>
            {
                item.Scope = shared;
                if (item.Id == "visibility")
                {
                    for (var i = 0; i < 3; i++) { var page = i; Key(page, () => AnimatedVisibility(item.Page.Value == page,
                        visibility => Page(item, shared, visibility, page), enter: IEnterTransition.None, exit: IExitTransition.None)); }
                }
                else if (item.Id == "caller")
                {
                    for (var i = 0; i < 3; i++) { var page = i; Key(page, () => Page(item, shared, new AnimatedVisibilityScope(item.Page.Value == page), page)); }
                }
                else AnimatedContent(item.Page.Value, content: (scope, page) => Page(item, shared, scope, page),
                    transitionSpec: _ => FadeIn(Tween(item.ParentDuration)).TogetherWith(FadeOut(Tween(item.ParentDuration))).Using(null),
                    modifier: Modifier.Size(310.Dp(), 166.Dp()).Clip());
            }, modifier: Modifier.Size(310.Dp(), 166.Dp()).Background(new Color(.025f, .035f, .055f)).TestTag("sl." + item.Id + ".stage"));
            Row(content: () =>
            {
                Control("sl." + item.Id + ".a", "A", () => item.Select(0));
                Control("sl." + item.Id + ".b", "B", () => item.Select(1));
                Control("sl." + item.Id + ".c", "C", () => item.Select(2));
                Control("sl." + item.Id + ".refresh", item.Id == "retarget" ? "Resize" : item.Id == "disabled" ? "Enable ↔" : "Refresh", item.Refresh);
            });
            Text($"Page {(char)('A' + item.Page.Value)} · revision {revision}", color: Color.LightGray, fontSize: 11.Sp());
        });
    }
    [Composable] private static void Page(SharedLayoutCase item, SharedTransitionScope shared, AnimatedVisibilityScope visibility, int page)
    {
        var state = shared.RememberSharedContentState(item.Id == "unmatched" ? page : "main", new DemoConfig(item.Enabled.Value, item.KeepEnabledForOngoingAnimation));
        item.States[page] = state;
        DisposableEffect(page, () => { item.Active.Add(page); return OnDispose(() => item.Active.Remove(page)); });
        var width = page == 0 ? 80 : page == 1 ? item.Large.Value ? 210 : 160 : 110;
        var height = page == 0 ? 60 : page == 1 ? 100 : 80;
        if (item.Id == "empty" && page == 1) { width = 0; height = 0; }
        var tween = Tween(item.Id == "instant" ? 0 : 1000, item.Id == "delay" ? 300 : 0, LinearEasing);
        Func<Rect, Rect, AnimationSpec>? transform = item.Id == "spring" ? null : (_, _) => tween;
        var clip = item.Id is "clip" or "crop" or "nested" ? new OverlayClip(RoundedCornerShape(18.Dp())) : OverlayClip.ParentClip;
        var mode = item.Id switch
        {
            "remeasure" => ResizeMode.RemeasureToBounds,
            "fit" => ResizeMode.ScaleToBounds(ContentScale.Fit),
            "crop" => ResizeMode.ScaleToBounds(ContentScale.Crop),
            "fill-bounds" => ResizeMode.ScaleToBounds(ContentScale.FillBounds),
            "fill-height" => ResizeMode.ScaleToBounds(ContentScale.FillHeight),
            "inside" => ResizeMode.ScaleToBounds(ContentScale.Inside),
            "no-scale" => ResizeMode.ScaleToBounds(ContentScale.None, Alignment.BottomEnd),
            _ => ResizeMode.ScaleToBounds()
        };
        Row(modifier: Modifier.TestTag("sl." + item.Id + ".page-" + page), content: () =>
        {
            Spacer(modifier: Modifier.Width((page == 0 ? 8 : page == 1 ? 110 : 40).Dp()));
            Column(content: () =>
            {
                Spacer(modifier: Modifier.Height((page == 0 ? 8 : page == 1 ? 36 : 16).Dp()));
                var modifier = Modifier.Size(width.Dp(), height.Dp()).Background(page == 0 ? new Color(.1f, .45f, .7f) : new Color(.12f, .65f, .46f))
                    .TestTag("sl." + item.Id + ".shared-" + page).Clickable(() => item.Select((page + 1) % 3));
                if (item.Id is "element" or "caller" or "z-order")
                    modifier = modifier.SharedElement(state, visibility, transform, zIndexInOverlay: 2);
                else modifier = modifier.SharedBounds(state, visibility,
                    enter: item.Id == "instant" ? IEnterTransition.None : item.Id == "delay" ? FadeIn(Tween(700, easing: LinearEasing)) : null,
                    exit: item.Id == "instant" ? IExitTransition.None : item.Id == "delay" ? FadeOut(Tween(200, easing: LinearEasing)) : null,
                    boundsTransform: transform, resizeMode: mode,
                    placeholderSize: item.Id == "placeholder" ? PlaceholderSize.AnimatedSize : item.Id == "custom-placeholder"
                        ? new PlaceholderSize((content, animated) => new Size(Math.Max(content.Width, animated.Width), content.Height)) : PlaceholderSize.ContentSize,
                    renderInOverlayDuringTransition: item.Id != "local", clipInOverlayDuringTransition: clip);
                Box(modifier: modifier, contentAlignment: Alignment.Center, content: () =>
                {
                    var textModifier = Modifier.TestTag("sl." + item.Id + ".text-" + page);
                    if (item.Id == "nested") textModifier = textModifier.SharedElement(shared.RememberSharedContentState("nested"), visibility, transform);
                    Text(item.Id == "empty" && page == 1 ? "" : item.Id is "element" or "caller" or "z-order" ? "★" : page == 0 ? "Compact" : "Expanded shared content wraps into several lines", color: Color.White, fontSize: 16.Sp(), modifier: textModifier);
                });
                if (item.Id == "placeholder") Text("Following sibling", color: Color.White, fontSize: 10.Sp(), modifier: Modifier.TestTag("sl.placeholder.sibling-" + page));
                if (item.Id == "z-order")
                    Box(content: () => { }, modifier: Modifier.Size(40.Dp()).Background(new Color(1, 0, 0)).SharedElement(shared.RememberSharedContentState("second"), visibility, (_, _) => Tween(2000, easing: LinearEasing), zIndexInOverlay: -1));
            });
        });
    }
    private sealed class DemoConfig(bool enabled, bool keep) : SharedContentConfig
    { public override bool IsEnabled => enabled; public override bool ShouldKeepEnabledForOngoingAnimation => keep; }
    [Composable] private static void Control(string tag, string text, Action click) => Box(
        modifier: Modifier.Size(75.Dp(), 30.Dp()).Padding(2.Dp()).Background(new Color(.17f, .24f, .33f), RoundedCornerShape(4.Dp())).Clickable(click).TestTag(tag),
        contentAlignment: Alignment.Center, content: () => Text(text, color: Color.White, fontSize: 11.Sp()));
}
}
