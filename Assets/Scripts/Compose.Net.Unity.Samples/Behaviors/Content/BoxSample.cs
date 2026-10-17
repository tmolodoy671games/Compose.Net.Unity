// ReSharper disable ArrangeNamespaceBody

using System;
using Compose.Net;
using global::Unity.Scripting.LifecycleManagement;

namespace Compose.Net.Unity.Samples.Behaviors.Content
{
    [NoAutoStaticsCleanup]
    internal partial class BoxSample : ComposeUI
    {
        private static readonly Alignment[] Alignments =
        {
            Alignment.TopLeft, Alignment.TopCenter, Alignment.TopRight,
            Alignment.CenterLeft, Alignment.Center, Alignment.CenterRight,
            Alignment.BottomLeft, Alignment.BottomCenter, Alignment.BottomRight
        };

        private static readonly string[] AlignmentNames =
        {
            "TopLeft", "TopCenter", "TopRight",
            "CenterLeft", "Center", "CenterRight",
            "BottomLeft", "BottomCenter", "BottomRight"
        };

        private static readonly Color Background = Color.FromBytes(22, 24, 30);
        private static readonly Color Surface = Color.FromBytes(34, 37, 46);
        private static readonly Color Field = Color.FromBytes(44, 48, 60);
        private static readonly Color Outline = Color.FromBytes(255, 196, 0);
        private static readonly Color Accent = Color.FromBytes(88, 120, 255);
        private static readonly Color Muted = Color.FromBytes(150, 156, 176);

        private static readonly Color[] Colors =
        {
            Color.FromBytes(255, 82, 82, 200), Color.FromBytes(64, 156, 255, 200),
            Color.FromBytes(46, 204, 113, 200), Color.FromBytes(170, 100, 255, 200)
        };

        private static readonly string[] Labels = { "A", "B", "C", "D", "E", "F" };

        private const string LongText = "Long text that wraps inside the box width and makes it taller";

        private static readonly Sp TextSize = 14.Sp();
        private static readonly Sp LabelSize = 16.Sp();

        private static IModifier Frame => Modifier
            .Background(Field, RoundedCornerShape(8.Dp()))
            .Border(2.Dp(), SolidColor(Outline), RoundedCornerShape(8.Dp()));

        [Composable]
        protected override void Content() => Layout();

        [Composable]
        protected override void Preview() => Layout();

        [Composable]
        private static void Layout()
        {
            var alignment = Remember(static () => MutableStateOf(4));
            var overrideAlign = Remember(static () => MutableStateOf(false));
            var big = Remember(static () => MutableStateOf(false));
            var longText = Remember(static () => MutableStateOf(false));
            var padding = Remember(static () => MutableStateOf(false));
            var count = Remember(static () => MutableStateOf(3));
            var shift = Remember(static () => MutableStateOf(0));
            var scrollState = RememberScrollState();
            var contentAlignment = Alignments[alignment.Value];
            var size = (big.Value ? 80 : 40).Dp();
            var text = longText.Value ? LongText : "Short";
            var pad = (padding.Value ? 12 : 0).Dp();

            Column(
                modifier: Modifier
                    .FillMaxSize()
                    .Background(Background)
                    .Padding(12.Dp())
                    .VerticalScroll(scrollState),
                content: () =>
                {
                    Row(() =>
                    {
                        Btn($"Alignment: {AlignmentNames[alignment.Value]}", true,
                            () => alignment.Value = (alignment.Value + 1) % 9);
                        Btn($"Child Align: {(overrideAlign.Value ? "BottomRight" : "Off")}", overrideAlign.Value,
                            () => overrideAlign.Value = !overrideAlign.Value);
                        Btn($"Children: {(big.Value ? "Big" : "Small")}", big.Value, () => big.Value = !big.Value);
                        Btn($"Text: {(longText.Value ? "Long" : "Short")}", longText.Value,
                            () => longText.Value = !longText.Value);
                        Btn($"Padding: {(padding.Value ? 12 : 0)}", padding.Value,
                            () => padding.Value = !padding.Value);
                        Stepper(
                            $"Children: {count.Value}",
                            () => count.Value = Math.Max(count.Value - 1, 0),
                            () => count.Value = Math.Min(count.Value + 1, Labels.Length)
                        );
                        Btn($"Z-order: {shift.Value % Math.Max(count.Value, 1)}", false, () => shift.Value++);
                    });

                    Grid(
                        () => Card("Overlay + Align", "C ignores contentAlignment when Child Align is on", () => Box(
                            contentAlignment: contentAlignment,
                            modifier: Frame.Size(160.Dp()),
                            content: () =>
                            {
                                Tile(0, 110.Dp());
                                Tile(1, 70.Dp());
                                Tile(2, 35.Dp(), overrideAlign.Value ? Modifier.Align(Alignment.BottomRight) : null);
                            }
                        )),
                        () => Card("Wrap content", "Box takes the size of its largest child", () => Box(
                            contentAlignment: contentAlignment,
                            modifier: Frame,
                            content: () =>
                            {
                                Tile(1, size);
                                Text(text, color: Color.White, fontSize: TextSize, modifier: Modifier.Padding(4.Dp()));
                            }
                        )),
                        () => Card("Padding", "Padding on Box and on children adds to the size", () => Box(
                            contentAlignment: contentAlignment,
                            modifier: Frame.Padding(pad),
                            content: () =>
                            {
                                Tile(3, size, Modifier.Padding(pad));
                                Text(text, color: Color.White, fontSize: TextSize, modifier: Modifier
                                    .Padding(pad)
                                    .Background(Background, RoundedCornerShape(4.Dp()))
                                    .Padding(4.Dp()));
                            }
                        ))
                    );

                    Grid(
                        () => Card("Fixed size", "Box keeps 120x120, bigger children overflow", () => Box(
                            contentAlignment: contentAlignment,
                            modifier: Frame.Size(120.Dp()),
                            content: () =>
                            {
                                Tile(0, (big.Value ? 160 : 60).Dp());
                                Text(text, color: Color.White, fontSize: TextSize, modifier: Modifier.Padding(4.Dp()));
                            }
                        )),
                        () => Card("Dynamic children", "Add / remove; Z-order moves the bottom child up", () => Box(
                            contentAlignment: contentAlignment,
                            modifier: Frame.Size(160.Dp()),
                            content: () =>
                            {
                                for (var i = 0; i < count.Value; i++)
                                {
                                    var id = (i + shift.Value) % count.Value;
                                    Key(
                                        key: id,
                                        content: () => Tile(id, (45 + id * 12).Dp(),
                                            Modifier.Offset(x: (id * 8).Dp(), y: (id * 8).Dp()))
                                    );
                                }
                            }
                        )),
                        () => Card("Nested wrap content", "Four Boxes without size grow and shrink together",
                            () => Nested(4, size, pad))
                    );

                    Grid(
                        () => Card("FillMaxWidth", "Box fills the width; long text wraps", () => Box(
                            contentAlignment: contentAlignment,
                            modifier: Frame.FillMaxWidth(),
                            content: () =>
                            {
                                Tile(2, size);
                                Text(longText.Value ? $"{LongText}. {LongText}." : "Short", color: Color.White,
                                    fontSize: TextSize, modifier: Modifier.Padding(4.Dp()));
                            }
                        )),
                        () => Card("Row + Weight", "Middle Box takes the remaining width", () => Row(
                            verticalAlignment: Alignment.CenterVertically,
                            modifier: Modifier.FillMaxWidth(),
                            content: () =>
                            {
                                Text("Left", color: Muted, fontSize: TextSize, modifier: Modifier.Padding(4.Dp()));
                                Box(
                                    contentAlignment: contentAlignment,
                                    modifier: Frame.Weight(1),
                                    content: () => Text(text, color: Color.White, fontSize: TextSize,
                                        modifier: Modifier.Padding(8.Dp()))
                                );
                                Box(
                                    modifier: Frame.Padding(8.Dp()),
                                    content: () => Tile(1, size)
                                );
                                Text("Right", color: Muted, fontSize: TextSize, modifier: Modifier.Padding(4.Dp()));
                            }
                        )),
                        () => Card("Column", "Box between two texts pushes them apart", () => Column(() =>
                        {
                            Text("Top", color: Muted, fontSize: TextSize, modifier: Modifier.Padding(4.Dp()));
                            Box(
                                contentAlignment: contentAlignment,
                                modifier: Frame,
                                content: () =>
                                {
                                    Tile(3, size);
                                    Text(text, color: Color.White, fontSize: TextSize,
                                        modifier: Modifier.Padding(4.Dp()));
                                }
                            );
                            Text("Bottom", color: Muted, fontSize: TextSize, modifier: Modifier.Padding(4.Dp()));
                        }))
                    );
                }
            );
        }

        [Composable]
        private static void Grid([Composable] Action left, [Composable] Action middle, [Composable] Action right)
        {
            Row(
                modifier: Modifier.FillMaxWidth(),
                content: () =>
                {
                    // Padding before any appearance modifier becomes a margin: the gap between cards.
                    Box(modifier: Modifier.Weight(1).Padding(6.Dp()), content: left);
                    Box(modifier: Modifier.Weight(1).Padding(6.Dp()), content: middle);
                    Box(modifier: Modifier.Weight(1).Padding(6.Dp()), content: right);
                }
            );
        }

        [Composable]
        private static void Tile(int index, Dp size, IModifier? modifier = null)
        {
            Box(
                contentAlignment: Alignment.Center,
                modifier: modifier.Then(Modifier.Size(size)
                    .Background(Colors[index % Colors.Length], RoundedCornerShape(6.Dp()))),
                content: () => Text(Labels[index], color: Color.White, fontSize: LabelSize)
            );
        }

        [Composable]
        private static void Nested(int depth, Dp size, Dp pad)
        {
            Box(
                contentAlignment: Alignment.Center,
                modifier: Modifier
                    .Background(Colors[depth % Colors.Length], RoundedCornerShape(8.Dp()))
                    .Border(2.Dp(), SolidColor(Outline), RoundedCornerShape(8.Dp()))
                    .Padding(8.Dp() + pad),
                content: () =>
                {
                    if (depth > 1)
                        Nested(depth - 1, size, pad);
                    else
                        Tile(0, size);
                }
            );
        }

        [Composable]
        private static void Card(string title, string description, [Composable] Action content)
        {
            Column(
                modifier: Modifier.FillMaxWidth()
                    .Background(Surface, RoundedCornerShape(12.Dp()))
                    .Padding(12.Dp()),
                content: () =>
                {
                    Text(title, color: Color.White, fontSize: 16.Sp());
                    Text(description, color: Muted, fontSize: 12.Sp(), modifier: Modifier.Padding(bottom: 8.Dp()));
                    content();
                }
            );
        }

        [Composable]
        private static void Stepper(string text, Action onMinus, Action onPlus)
        {
            Row(
                verticalAlignment: Alignment.CenterVertically,
                modifier: Modifier
                    .Padding(4.Dp())
                    .Height(32.Dp())
                    .Background(Field, RoundedCornerShape(8.Dp())),
                content: () =>
                {
                    StepperButton("-", onMinus);
                    Text(text, color: Color.White, fontSize: 13.Sp());
                    StepperButton("+", onPlus);
                }
            );
        }

        [Composable]
        private static void StepperButton(string text, Action onClick)
        {
            Box(
                contentAlignment: Alignment.Center,
                modifier: Modifier.Size(32.Dp()).Clickable(onClick),
                content: () => Text(text, color: Color.White, fontSize: 16.Sp())
            );
        }

        [Composable]
        private static void Btn(string text, bool active, Action onClick)
        {
            Box(
                contentAlignment: Alignment.Center,
                modifier: Modifier
                    .Padding(4.Dp())
                    .Height(32.Dp())
                    .Background(active ? Accent : Field)
                    .Padding(horizontal: 12.Dp())
                    .Clip(RoundedCornerShape(8.Dp()))
                    .Clickable(onClick),
                content: () => Text(text, color: Color.White, fontSize: 13.Sp())
            );
        }
    }
}