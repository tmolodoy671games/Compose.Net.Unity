using Compose.Net;
using Compose.Net.Showcase;
using Compose.Net.Unity;
using static Compose.Net.ComposeFunctions;

namespace Compose.Net.Unity.Showcase;

public sealed partial class OrbitDemo : ComposeUI
{
    private OrbitState? _state;
    public OrbitState State => _state ??= new OrbitState();
    [Composable] protected override void Content() => Render();
    [Composable] protected override void Preview() => Render();
    [Composable] private void Render() => OrbitApp.Content(State, Remember(() => new OrbitUnityHost()));
}
