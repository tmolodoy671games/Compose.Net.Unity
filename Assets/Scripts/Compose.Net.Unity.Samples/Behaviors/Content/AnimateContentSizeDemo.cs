using System;
using Compose.Net;
using UnityEngine.UIElements;
using static Compose.Net.ComposeFunctions;

namespace Compose.Net.Unity.Samples.Behaviors.Content;

[AiGenerated]
public partial class AnimateContentSizeDemo : ComposeUI
{
    [Composable] protected override void Content() => Screen(Remember(() => new AnimateContentSizeDemoState()));
    [Composable] protected override void Preview() => Screen(Remember(() => new AnimateContentSizeDemoState()));

    [Composable]
    public static void Screen(AnimateContentSizeDemoState state)
    {
        Column(modifier: Modifier.FillMaxWidth().Background(new Color(0.045f, 0.06f, 0.095f))
            .Padding(20.Dp()).TestTag("animate-size-demo"), content: () =>
        {
            Text("Modifier.AnimateContentSize", color: Color.White, fontSize: 30.Sp());
            Text("Click content to expand / collapse. S, M and L select a target; click again during motion to interrupt.",
                color: Color.LightGray, fontSize: 14.Sp());
            Row(content: () =>
            {
                SizeControl("animate.expand-all", "Expand all", state.ExpandAll, 150);
                SizeControl("animate.collapse-all", "Collapse all", state.CollapseAll, 150);
            });
            Spacer(Modifier.Height(12.Dp()));
            for (var row = 0; row < 5; row++)
            {
                var first = row * 3;
                Row(content: () =>
                {
                    SizeCard(state.Cases[first]);
                    SizeCard(state.Cases[first + 1]);
                    SizeCard(state.Cases[first + 2]);
                });
            }
            Text("First layout does not animate. Cancelled animations do not report completion. " +
                "The final callback uses the latest listener.\n" +
                "This implementation uses Size (UI units) and AnimationSpec; its default is Tween. " +
                "Start / End currently follow the library's LTR alignment aliases.",
                color: Color.LightGray, fontSize: 13.Sp(), modifier: Modifier.TestTag("animate.help"));
        });
    }

    [Composable]
    private static void SizeCard(AnimateContentSizeDemoCase item)
    {
        var tag = "animate." + item.Id;
        // Reading Revision deliberately recomposes this card without changing its target size.
        var revision = item.Revision.Value;
        Column(modifier: Modifier.Width(336.Dp()).Height(370.Dp()).Padding(6.Dp())
            .Background(new Color(0.095f, 0.12f, 0.18f), RoundedCornerShape(10.Dp())).Padding(12.Dp()), content: () =>
        {
            Text(item.Title, color: Color.White, fontSize: 18.Sp());
            Text(item.Description, color: Color.LightGray, fontSize: 12.Sp(), modifier: Modifier.Height(36.Dp()));
            Box(modifier: Modifier.Width(296.Dp()).Height(164.Dp())
                .Background(new Color(0.055f, 0.075f, 0.12f)).Clip().Padding(8.Dp())
                .TestTag(tag + ".stage"), content: () =>
            {
                if (item.Scenario == AnimateSizeScenario.Neighbors)
                    Column(content: () =>
                    {
                        SizeTarget(item);
                        Text("↑ Following sibling", color: Color.White, fontSize: 12.Sp(),
                            modifier: Modifier.TestTag(tag + ".neighbor"));
                    });
                else
                    SizeTarget(item);
            });
            Row(content: () =>
            {
                SizeControl(tag + ".small", "S", item.Small, 56);
                SizeControl(tag + ".medium", "M", item.Medium, 56);
                SizeControl(tag + ".large", "L", item.Large, 56);
            });
            if (item.Scenario == AnimateSizeScenario.Alignment)
                SizeControl(tag + ".align", "Anchor: " + AnimateContentSizeDemoCase.AlignmentNames[item.AlignmentIndex.Value],
                    item.NextAlignment, 290);
            else if (item.Scenario == AnimateSizeScenario.Parameters)
                Row(content: () =>
                {
                    SizeControl(tag + ".speed", item.Slow.Value ? "3000 ms" : "1000 ms", item.ToggleSpec, 140);
                    SizeControl(tag + ".listener", item.AlternateListener.Value ? "Listener B" : "Listener A", item.ToggleListener, 150);
                });
            else if (item.Scenario == AnimateSizeScenario.Modifier)
                SizeControl(tag + ".enabled", item.AnimationEnabled.Value ? "Animation ON" : "Animation OFF", item.ToggleModifier, 290);
            else if (item.Scenario == AnimateSizeScenario.Content)
                SizeControl(tag + ".visible", item.Visible.Value ? "Remove content" : "Restore content", item.ToggleVisible, 290);
            else if (item.Scenario == AnimateSizeScenario.Recompose)
                Row(content: () =>
                {
                    SizeControl(tag + ".prefix", item.Decorated.Value ? "Opacity 65%" : "Opacity 100%", item.ToggleDecoration, 145);
                    SizeControl(tag + ".same", "Same target #" + revision, item.NoChange, 145);
                });
            else
                Spacer(Modifier.Height(30.Dp()));
            Text(item.Scenario == AnimateSizeScenario.Default ? "Default spec · no listener" :
                    $"Finished: A {item.CompletedA.Value} / B {item.CompletedB.Value}",
                color: Color.White, fontSize: 12.Sp(), modifier: Modifier.TestTag(tag + ".count"));
            Text(item.LastResult.Value, color: Color.LightGray, fontSize: 12.Sp(),
                modifier: Modifier.TestTag(tag + ".result"));
        });
    }

    [Composable]
    private static void SizeTarget(AnimateContentSizeDemoCase item)
    {
        var tag = "animate." + item.Id;
        var modifier = Modifier.Background(new Color(0.18f, 0.37f, 0.62f)).TestTag(tag + ".target");
        if (item.Decorated.Value) modifier = modifier.Alpha(0.65f);
        if (item.AnimationEnabled.Value)
        {
            var listener = item.AlternateListener.Value ? item.AlternativeCompleted : item.Completed;
            modifier = item.Scenario switch
            {
                AnimateSizeScenario.Default => modifier.AnimateContentSize(),
                AnimateSizeScenario.Both => modifier.AnimateContentSize(item.Spec, listener),
                _ => modifier.AnimateContentSize(item.Spec,
                    AnimateContentSizeDemoCase.Alignments[item.AlignmentIndex.Value], listener)
            };
        }
        Box(modifier: WithNativeClick(modifier, item.Toggle), content: () =>
        {
            if (item.Visible.Value)
            {
                if (item.Scenario == AnimateSizeScenario.Text)
                    Text(item.Target.Value == 0 ? "One line" : item.Target.Value == 1 ? "Two lines\nMeasured text" :
                            "Four lines\nMeasured text\nNo explicit height\nUI Toolkit layout",
                        fontSize: 18.Sp(), color: Color.White, modifier: Modifier.TestTag(tag + ".content"));
                else if (item.Scenario == AnimateSizeScenario.List)
                    Column(modifier: Modifier.TestTag(tag + ".content"), content: () =>
                    {
                        for (var i = 0; i <= item.Target.Value; i++)
                        {
                            var index = i;
                            Box(modifier: Modifier.Width((96 + i * 40).Dp()).Height(32.Dp())
                                    .Background(new Color(0.12f, 0.5f + i * 0.1f, 0.68f))
                                    .TestTag(tag + ".row-" + index),
                                content: () => Text("Row " + (index + 1), color: Color.White, fontSize: 14.Sp()));
                        }
                    });
                else
                {
                    var size = item.TargetSize;
                    Box(modifier: Modifier.Size(size.Width.Dp(), size.Height.Dp())
                            .Background(new Color(0.13f, 0.55f, 0.69f)).TestTag(tag + ".content"),
                        contentAlignment: Alignment.Center,
                        content: () => Text($"{size.Width:0} × {size.Height:0}", color: Color.White, fontSize: 16.Sp()));
                }
            }
        });
    }

    [Composable]
    private static void SizeControl(string tag, string label, Action action, int width) =>
        Box(modifier: WithNativeClick(Modifier.Width(width.Dp()).Height(30.Dp()).Padding(2.Dp())
                .Background(new Color(0.18f, 0.23f, 0.32f), RoundedCornerShape(4.Dp()))
                .TestTag(tag), action), contentAlignment: Alignment.Center,
            content: () => Text(label, color: Color.White, fontSize: 12.Sp(), modifier: Modifier.TestTag(tag + ".label")));

    [Composable]
    private static IModifier WithNativeClick(IModifier modifier, Action action)
    {
        // Demo controls use the UI Toolkit gesture recognizer independently of Modifier.Clickable.
        var clickable = Remember(action, () => new Clickable(action));
        var previousPickingMode = UnityEngine.UIElements.PickingMode.Ignore;
        return modifier.Custom(
            apply: node =>
            {
                var element = node.VisualElement();
                previousPickingMode = element.pickingMode;
                element.pickingMode = UnityEngine.UIElements.PickingMode.Position;
                element.AddManipulator(clickable);
            },
            revert: node =>
            {
                var element = node.VisualElement();
                element.RemoveManipulator(clickable);
                element.pickingMode = previousPickingMode;
            });
    }

}
