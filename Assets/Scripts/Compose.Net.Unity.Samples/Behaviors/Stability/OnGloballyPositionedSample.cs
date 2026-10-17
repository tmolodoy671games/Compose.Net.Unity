// ReSharper disable ArrangeNamespaceBody

using Compose.Net;
using SharpExtensions;

namespace Compose.Net.Unity.Samples.Behaviors.Stability
{
    internal partial class OnGloballyPositionedSample : ComposeUI
    {
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
            var parentCoordinates = Remember(() => MutableStateOf(Optional.Empty<ILayoutCoordinates>()));
            Column(
                horizontalAlignment: Alignment.CenterHorizontally,
                modifier: Modifier.FillMaxSize()
                    .Padding(all: 100.Dp())
                    .OnGloballyPositioned(it => parentCoordinates.Value = it.ToOptional()),
                content: () =>
                {
                    var isSwitched = Remember(static () => MutableStateOf(false));
                    var layout = Remember(static () => MutableStateOf(Optional.Empty<Offset>()));
                    Box(
                        modifier: Modifier.FillMaxSize(),
                        content: () =>
                        {
                            var transitionSpec = Tween(1_000);
                            Box(
                                contentAlignment: Alignment.Center,
                                modifier: Modifier
                                    .Size(40.Dp())
                                    .Background(Color.BlueColor)
                                    .Offset(
                                        x: AnimateFloatAsState(
                                            targetValue: 500 * isSwitched.Value.ToInt(),
                                            animationSpec: transitionSpec
                                        ).Value.Dp()
                                    ),
                                content: () =>
                                {
                                    Box(() =>
                                    {
                                        Box(() =>
                                        {
                                            Spacer(
                                                Modifier
                                                    .Background(Color.GreenColor)
                                                    .Size(20.Dp())
                                                    .OnGloballyPositioned(it => layout.Value = it.PositionInRoot())
                                            );
                                        });
                                    });
                                }
                            );
                        }
                    );
                    Text(
                        modifier: Modifier
                            .Background(Color.BlueColor)
                            .Padding(all: 32.Dp())
                            .Clip(RoundedCornerShape(32.Dp()))
                            .Clickable(() => isSwitched.Value = !isSwitched.Value),
                        color: Color.White,
                        text: "Switch"
                    );

                    if (layout.Value.HasValue && parentCoordinates.Value.HasValue)
                    {
                        var parentCoordinatesValue = parentCoordinates.Value.Value;
                        Spacer(
                            modifier: Modifier
                                .Size(10.Dp())
                                .Background(Color.RedColor)
                                .Float()
                                .Position(
                                    left: parentCoordinatesValue.RootToLocal(layout.Value.Value).X.Dp(),
                                    top: parentCoordinatesValue.RootToLocal(layout.Value.Value).Y.Dp()
                                )
                        );
                    }
                }
            );
        }
    }
}