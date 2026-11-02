using Compose.Net;

namespace Compose.Net.Unity.Samples.Behaviors.Content
{
    [global::Unity.Scripting.LifecycleManagement.NoAutoStaticsCleanup]
    public sealed partial class DemoHighlightIndication : IIndication
    {
        public static readonly DemoHighlightIndication Instance = new();
        private DemoHighlightIndication() { }

        [Composable]
        public IIndicationInstance RememberUpdatedInstance(IInteractionSource interactionSource)
        {
            var hovered = interactionSource.CollectIsHoveredAsState().Value;
            var pressed = interactionSource.CollectIsPressedAsState().Value;
            return Remember((hovered, pressed), () => new Highlight(hovered, pressed));
        }

        private sealed class Highlight(bool hovered, bool pressed) : IIndicationInstance
        {
            public void DrawIndication(IDrawScope scope)
            {
                if (pressed) scope.DrawRect(Color.Orange, size: scope.Size, alpha: 0.55f);
                else if (hovered) scope.DrawRect(Color.DodgerBlue, size: scope.Size, alpha: 0.35f);
            }
        }
    }
}
