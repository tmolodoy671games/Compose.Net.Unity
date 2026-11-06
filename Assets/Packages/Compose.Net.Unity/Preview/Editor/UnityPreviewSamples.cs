using static Compose.Net.ComposeFunctions;
using static Compose.Net.RememberFunctions;
using static Compose.Net.TextFunctions;
using static Compose.Net.BoxFunctions;

namespace Compose.Net.Unity.Preview;

[AiGenerated]
public static partial class UnityPreviewSamples
{
    [UnityPreview(Name = "Counter", Width = 400, Height = 400)]
    [Composable]
    private static void Counter()
    {
        var count = Remember(() => MutableStateOf(0));
        Box(
            modifier: Modifier
                .FillMaxSize()
                .TestTag("preview.counter")
                .Clickable(() => count.Value++),
            contentAlignment: Alignment.Center,
            content: () => Text($"Count: {count.Value} — click to increment")
        );
    }
}