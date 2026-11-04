using Compose.Net.MaterialDemo;

namespace Compose.Net.Unity.MaterialDemo;

public sealed partial class MaterialDemo : ComposeUI
{
    private MaterialLabState? _state;
    public MaterialLabState State => _state ??= new MaterialLabState();
    [Composable] protected override void Content() => MaterialLab.Content(State);
    [Composable] protected override void Preview() => MaterialLab.Content(State);
}
