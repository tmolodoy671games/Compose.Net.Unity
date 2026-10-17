// ReSharper disable ArrangeNamespaceBody

using Compose.Net;

namespace UnityCompose.Samples.Behaviors.Content
{
    internal partial class DragAndDropSample : ComposeUI
    {
        private static IModifier Cell => Modifier
            .Padding(all: 12.Dp())
            .Size(320.Dp())
            .Background(Color.DarkSlateGray);

        [Composable]
        protected override void Content()
        {
            Layout();
        }

        [Composable]
        protected override void Preview()
        {
            Layout();
        }

        [Composable]
        private static void Layout()
        {
            Debug.Log("Layout()");
            Column(
                verticalArrangement: Arrangement.Center,
                horizontalAlignment: Alignment.CenterHorizontally,
                modifier: Modifier.FillMaxSize(),
                content: () =>
                {
                    Row(() =>
                    {
                        Slider();
                        SpringBack();
                        Toggleable();
                    });
                    Row(() =>
                    {
                        DropTarget();
                        SwipeToDismiss();
                        Resizable();
                    });
                }
            );
        }

        [Composable]
        private static void Slider()
        {
            var value = Remember(static () => MutableStateOf(0f));
            Column(
                verticalArrangement: Arrangement.Center,
                horizontalAlignment: Alignment.CenterHorizontally,
                modifier: Cell,
                content: () =>
                {
                    Text($"Draggable Horizontal: {value.Value / 240:P0}", color: Color.White);
                    Box(
                        modifier: Modifier
                            .Width(280.Dp())
                            .Height(40.Dp())
                            .Background(Color.Gray),
                        content: () =>
                        {
                            Spacer(
                                Modifier
                                    .Size(40.Dp())
                                    .Offset(x: value.Value.Dp())
                                    .Background(Color.DodgerBlue)
                                    .Draggable(
                                        Orientation.Horizontal,
                                        delta => value.Value = Mathf.Clamp(value.Value + delta, 0, 240)
                                    )
                            );
                        }
                    );
                }
            );
        }

        [Composable]
        private static void SpringBack()
        {
            var offset = Remember(static () => MutableStateOf(0f));
            var dragging = Remember(static () => MutableStateOf(false));
            Column(
                horizontalAlignment: Alignment.CenterHorizontally,
                modifier: Cell,
                content: () =>
                {
                    Text(dragging.Value ? "Vertical: dragging" : "Vertical: idle", color: Color.White);
                    Spacer(
                        Modifier
                            .Size(80.Dp())
                            .Offset(
                                y: (110 + AnimateFloatAsState(
                                    offset.Value,
                                    Tween(dragging.Value ? 0 : 300)
                                ).Value).Dp()
                            )
                            .Background(AnimateColorAsState(dragging.Value ? Color.Coral : Color.CadetBlue).Value)
                            .Draggable(
                                Orientation.Vertical,
                                delta => offset.Value = Mathf.Clamp(offset.Value + delta, -110, 110),
                                onDragStarted: _ => dragging.Value = true,
                                onDragStopped: () =>
                                {
                                    dragging.Value = false;
                                    offset.Value = 0;
                                }
                            )
                    );
                }
            );
        }

        [Composable]
        private static void Toggleable()
        {
            var offset = Remember(static () => MutableStateOf(0f));
            var enabled = Remember(static () => MutableStateOf(true));
            Column(
                verticalArrangement: Arrangement.Center,
                horizontalAlignment: Alignment.CenterHorizontally,
                modifier: Cell,
                content: () =>
                {
                    Box(
                        modifier: Modifier
                            .Background(Color.SteelBlue)
                            .Padding(all: 8.Dp())
                            .Clip()
                            .Clickable(() => enabled.Value = !enabled.Value),
                        content: () =>
                        {
                            Text(
                                enabled.Value ? "enabled: true" : "enabled: false",
                                color: Color.White
                            );
                        }
                    );

                    Spacer(
                        Modifier
                            .Size(60.Dp())
                            .Offset(x: offset.Value.Dp())
                            .Background(enabled.Value ? Color.MediumSeaGreen : Color.DimGray)
                            .Draggable(
                                Orientation.Horizontal,
                                delta => offset.Value = Mathf.Clamp(offset.Value + delta, -120, 120),
                                enabled: enabled.Value
                            )
                    );
                }
            );
        }

        [Composable]
        private static void DropTarget()
        {
            var offset = Remember(static () => MutableStateOf(Offset.Zero));
            var dragging = Remember(static () => MutableStateOf(false));
            var drops = Remember(static () => MutableStateOf(0));
            var coordinates = Remember(static () => new ILayoutCoordinates?[2]);
            var over = dragging.Value && IsOver(coordinates);
            Column(
                horizontalAlignment: Alignment.CenterHorizontally,
                modifier: Cell,
                content: () =>
                {
                    Text($"DetectDragGestures, drops: {drops.Value}", color: Color.White);
                    Spacer(
                        Modifier
                            .Size(120.Dp())
                            .Background(over ? Color.LimeGreen : Color.DarkGray)
                            .OnGloballyPositioned(it => coordinates[1] = it)
                    );
                    Spacer(
                        Modifier
                            .Size(60.Dp())
                            .Offset(offset.Value.X.Dp(), (60 + offset.Value.Y).Dp())
                            .Background(dragging.Value ? Color.Orange : Color.Gold)
                            .OnGloballyPositioned(it => coordinates[0] = it)
                            .PointerInput(0, scope => scope.DetectDragGestures(
                                onDragStart: _ => dragging.Value = true,
                                onDrag: (change, delta) =>
                                {
                                    change.Consume();
                                    offset.Value += delta;
                                },
                                onDragEnd: () =>
                                {
                                    if (IsOver(coordinates)) drops.Value++;
                                    dragging.Value = false;
                                    offset.Value = Offset.Zero;
                                },
                                onDragCancel: () =>
                                {
                                    dragging.Value = false;
                                    offset.Value = Offset.Zero;
                                }
                            ))
                    );
                }
            );
        }

        [Composable]
        private static void SwipeToDismiss()
        {
            var offset = Remember(static () => MutableStateOf(0f));
            var dismissed = Remember(static () => MutableStateOf(0));
            Column(
                verticalArrangement: Arrangement.Center,
                horizontalAlignment: Alignment.CenterHorizontally,
                modifier: Cell,
                content: () =>
                {
                    Text($"DetectHorizontalDragGestures, dismissed: {dismissed.Value}", color: Color.White);
                    Box(
                        contentAlignment: Alignment.Center,
                        modifier: Modifier
                            .Width(240.Dp())
                            .Height(80.Dp())
                            .Offset(x: offset.Value.Dp())
                            .Alpha(Mathf.Clamp01(1 - Mathf.Abs(offset.Value) / 200))
                            .Background(Color.MediumPurple)
                            .PointerInput(0, scope => scope.DetectHorizontalDragGestures(
                                (_, delta) => offset.Value += delta,
                                onDragEnd: () =>
                                {
                                    if (Mathf.Abs(offset.Value) > 120) dismissed.Value++;
                                    offset.Value = 0;
                                }
                            )),
                        content: () => { Text("Swipe me", color: Color.White); }
                    );
                }
            );
        }

        [Composable]
        private static void Resizable()
        {
            var height = Remember(static () => MutableStateOf(120f));
            Column(
                horizontalAlignment: Alignment.CenterHorizontally,
                modifier: Cell,
                content: () =>
                {
                    Text($"DetectVerticalDragGestures: {height.Value:0}", color: Color.White);
                    Spacer(
                        Modifier
                            .Width(200.Dp())
                            .Height(height.Value.Dp())
                            .Background(Color.Teal)
                    );
                    Spacer(
                        Modifier
                            .Width(200.Dp())
                            .Height(24.Dp())
                            .Background(Color.LightGray)
                            .PointerInput(0, scope =>
                                scope.DetectVerticalDragGestures((_, delta) =>
                                    height.Value = Mathf.Clamp(height.Value + delta, 40, 240)
                                ))
                    );
                }
            );
        }

        private static bool IsOver(ILayoutCoordinates?[] coordinates)
        {
            if (coordinates[0] is not { } item || coordinates[1] is not { } target) return false;
            var center = target.RootToLocal(item.LocalToRoot(new Offset(item.Size.Width / 2, item.Size.Height / 2)));
            return center.X >= 0 && center.Y >= 0 && center.X <= target.Size.Width && center.Y <= target.Size.Height;
        }
    }
}