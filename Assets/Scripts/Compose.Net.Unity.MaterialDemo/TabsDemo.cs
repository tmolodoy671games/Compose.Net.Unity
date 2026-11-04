using Compose.Net.MaterialDemo;

namespace Compose.Net.Unity.MaterialDemo;

public sealed partial class TabsDemo : ComposeUI
{
    [Composable] protected override void Content() => TabsLab.Content();
    [Composable] protected override void Preview() => TabsLab.Content();
}
