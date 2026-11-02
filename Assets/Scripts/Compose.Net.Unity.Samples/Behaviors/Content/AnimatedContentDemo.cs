using System;
using Compose.Net;
using static Compose.Net.ComposeFunctions;

namespace Compose.Net.Unity.Samples.Behaviors.Content
{

public partial class AnimatedContentDemo : ComposeUI
{
    [Composable] protected override void Content() => Screen(Remember(() => new AnimatedContentDemoState()));
    [Composable] protected override void Preview() => Screen(Remember(() => new AnimatedContentDemoState()));

    [Composable]
    public static void Screen(AnimatedContentDemoState state)
    {
        var scroll = RememberScrollState();
        Column(modifier: Modifier.FillMaxSize().Background(new Color(.04f, .055f, .085f)).VerticalScroll(scroll)
            .Padding(20.Dp()).TestTag("animated-content-demo"), content: () =>
        {
            Text("AnimatedContent", fontSize: 32.Sp(), color: Color.White);
            Text("21 live examples · change targets at any point, including during enter and exit", fontSize: 14.Sp(), color: Color.LightGray);
            Row(content: () =>
            {
                Control("ac.all-a", "All → A", () => state.SelectAll(0), 110);
                Control("ac.all-b", "All → B", () => state.SelectAll(1), 110);
                Control("ac.all-c", "All → C", () => state.SelectAll(2), 110);
            });
            for (var row = 0; row < 7; row++)
            {
                var first = row * 3;
                Row(content: () => { Card(state.Cases[first]); Card(state.Cases[first + 1]); Card(state.Cases[first + 2]); });
            }
        });
    }
    [Composable]
    private static void Card(AnimatedContentDemoCase item)
    {
        var revision = item.Revision.Value;
        var target = item.Target.Value;
        var alignment = AnimatedContentDemoCase.Alignments[item.AlignmentIndex.Value];
        var tag = "ac." + item.Id;
        Column(modifier: Modifier.Width(340.Dp()).Height(320.Dp()).Padding(6.Dp())
            .Background(new Color(.085f, .11f, .17f), RoundedCornerShape(10.Dp())).Padding(10.Dp()), content: () =>
        {
            Text(item.Title, fontSize: 18.Sp(), color: Color.White);
            Text(item.Description, fontSize: 12.Sp(), color: Color.LightGray, modifier: Modifier.Height(42.Dp()));
            Box(modifier: Modifier.Width(308.Dp()).Height(158.Dp()).Background(new Color(.035f, .05f, .08f))
                .TestTag(tag + ".stage"), content: () =>
            {
                Column(content: () =>
                {
                    if (item.Id == "transition")
                    {
                        item.Transition = UpdateTransition(target, "Demo external transition");
                        item.Transition.AnimatedContent(content: (scope, value) => Page(item, scope, value),
                            transitionSpec: scope => Transform(item, scope), contentAlignment: alignment,
                            modifier: Modifier.TestTag(tag + ".container"));
                    }
                    else
                        AnimatedContent(target, content: (scope, value) => Page(item, scope, value),
                            transitionSpec: item.Id == "default" ? null : scope => Transform(item, scope),
                            contentAlignment: alignment, label: item.Title,
                            contentKey: value => item.Id == "key" ? value / 10 : value,
                            modifier: Modifier.TestTag(tag + ".container"));
                    if (item.Id == "size")
                        Text("Following sibling", color: Color.LightGray, fontSize: 11.Sp(), modifier: Modifier.TestTag(tag + ".neighbor"));
                });
            });
            Row(content: () =>
            {
                Control(tag + ".a", "A", () => item.Select(0), 72);
                Control(tag + ".b", "B", () => item.Select(1), 72);
                Control(tag + ".c", "C", () => item.Select(2), 72);
                Control(tag + ".refresh", "↻", item.Refresh, 72);
            });
            if (item.Id == "alignment")
                Control(tag + ".align", AnimatedContentDemoCase.AlignmentNames[item.AlignmentIndex.Value], item.Align, 290);
            else
                Text($"Target: {(char)('A' + target / 10)} · revision {target % 10 + revision}", color: Color.LightGray,
                    fontSize: 12.Sp(), modifier: Modifier.TestTag(tag + ".status"));
        });
    }
    private static ContentTransform Transform(AnimatedContentDemoCase item, AnimatedContentTransitionScope<int> scope)
    {
        item.Segments.Add((scope.InitialState, scope.TargetState));
        var spec = Tween(1000, easing: LinearEasing);
        IEnterTransition enter = FadeIn(spec);
        IExitTransition exit = FadeOut(Tween(500, easing: LinearEasing));
        if (item.Id is "horizontal" or "vertical" or "direction" or "interrupt" or "unclipped")
        {
            var direction = item.Id == "vertical" ? SlideDirection.Up : item.Id == "direction" && scope.TargetState < scope.InitialState
                ? SlideDirection.Right : SlideDirection.Left;
            enter = scope.SlideIntoContainer(direction, spec) + FadeIn(spec);
            exit = scope.SlideOutOfContainer(direction, spec) + FadeOut(spec);
        }
        if (item.Id == "expand")
        {
            enter = ExpandIn(spec, expandFrom: Alignment.Center);
            exit = ShrinkOut(spec, shrinkTowards: Alignment.Center);
        }
        if (item.Id == "delay")
        {
            enter = FadeIn(Tween(600, 400, LinearEasing));
            exit = FadeOut(Tween(300, 300, LinearEasing));
        }
        if (item.Id == "size-only") { enter = IEnterTransition.None; exit = IExitTransition.None; }
        if (item.Id == "keep") exit += scope.KeepUntilTransitionsFinished;
        if (item.Id == "none") { enter = IEnterTransition.None; exit = IExitTransition.None; }
        var transform = new ContentTransform(enter, exit, item.Id == "z-order" ? -scope.TargetState : 0);
        return transform.Using(item.Id is "no-size" or "none" ? null : SizeTransform(clip: item.Id != "unclipped",
            sizeAnimationSpec: (_, _) => spec));
    }
    [Composable]
    private static void Page(AnimatedContentDemoCase item, AnimatedContentScope scope, int value)
    {
        var page = value / 10;
        var identity = Remember(() => new object());
        item.Remembered[page] = identity;
        DisposableEffect(identity, () =>
        {
            item.Active.Add(identity);
            return OnDispose(() => { item.Active.Remove(identity); item.Disposals++; });
        });
        if (item.Id == "empty" && page == 2) return;
        var tag = "ac." + item.Id + ".page-" + page;
        var width = page == 0 ? 100 : page == 1 ? 220 : 150;
        var height = page == 0 ? 48 : page == 1 ? 112 : 76;
        Box(modifier: Modifier.Size(width.Dp(), height.Dp()).Background(page switch
            { 0 => new Color(.12f, .42f, .66f), 1 => new Color(.13f, .58f, .48f), _ => new Color(.58f, .3f, .68f) })
            .TestTag(tag).Clickable(() => item.Select((page + 1) % 3)), contentAlignment: Alignment.Center, content: () =>
        {
            var modifier = Modifier.TestTag(tag + ".text");
            if (item.Id == "child") modifier = modifier.AnimateEnterExit(scope, FadeIn(Tween(1200)), FadeOut(Tween(1500)));
            Text($"{(char)('A' + page)} · {value % 10}", fontSize: 22.Sp(), color: Color.White, modifier: modifier);
        });
    }
    [Composable]
    private static void Control(string tag, string label, Action action, int width) =>
        Box(modifier: Modifier.Width(width.Dp()).Height(30.Dp()).Padding(2.Dp())
            .Background(new Color(.18f, .23f, .32f), RoundedCornerShape(4.Dp())).TestTag(tag).Clickable(action),
            contentAlignment: Alignment.Center, content: () => Text(label, color: Color.White, fontSize: 12.Sp()));
}

}
