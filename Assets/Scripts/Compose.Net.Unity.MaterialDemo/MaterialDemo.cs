using Compose.Net.MaterialDemo;

namespace Compose.Net.Unity.MaterialDemo;

public sealed partial class MaterialDemo : ComposeUI
{
    [Composable] protected override void Content() => MaterialLab.Content();
    [Composable] protected override void Preview() => MaterialLab.Content();
}
