using System;
using Compose.Net;
using SharpExtensions;
using UnityEngine.UIElements;

// ReSharper disable ArrangeNamespaceBody

namespace Compose.Net.Unity.Samples.Behaviors.Content
{
    internal partial class AnimatedVisibilitySample : ComposeUI
    {
        private int Foo
        {
            [Composable]
            get => 1;
        }
        
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
            Column(
                horizontalAlignment: Alignment.CenterHorizontally,
                verticalArrangement: Arrangement.Center,
                modifier: Modifier.FillMaxSize(),
                content: () =>
                {
                    var isVisible = Remember(() => MutableStateOf(true));
                    Spacer(
                        Modifier
                            .Size(100.Dp(), 100.Dp())
                            .Background(Color.ForestGreen)
                            .Clip(RoundedCornerShape(16.Dp()))
                            .Clickable(() => isVisible.Value = !isVisible.Value)
                    );
                    AnimatedVisibility(
                        visible: isVisible.Value,
                        enter: FadeIn(Tween(1_000)) + ExpandVertically(Tween(1_000)),
                        exit: FadeOut(Tween(1_000)) + ShrinkVertically(Tween(1_000)),
                        modifier: Modifier
                            .Padding(16.Dp())
                            .Background(Color.LightBlue),
                        content: () => Text(
                            "Text",
                            fontSize: 64.Sp())
                    );
                }
            );
        }
    }
}