using Compose.Net.SoftDemo;

namespace Compose.Net.Unity.SoftDemo;

public sealed partial class SoftDemo : ComposeUI
{
    private SoftState? _state;
    public SoftState State => _state ??= new SoftState();
    [Composable] protected override void Content() => SoftApp.Content(State);
    [Composable] protected override void Preview() => SoftApp.Content(State);
}
