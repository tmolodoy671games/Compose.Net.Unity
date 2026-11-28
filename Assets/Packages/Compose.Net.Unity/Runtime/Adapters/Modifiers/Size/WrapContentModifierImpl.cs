namespace Compose.Net.Unity;

internal sealed record WrapContentModifierImpl(Orientation? Axis, Alignment Align, bool Unbounded, Orientation? RequiredAxis = null)
    : UnityModifierNodeElement<WrapContentModifierImpl.WrapNode>, IContentMeasureModifier
{
    internal bool Wraps(bool horizontal) => Axis == null ||
        Axis == (horizontal ? Orientation.Horizontal : Orientation.Vertical);

    public override WrapNode Create() => new(this);
    public override void Update(WrapNode node) => node.Set(this);
    public override object GetTarget(IReusableComposeNode node) => node.VisualElement<Box>();

    internal sealed class WrapNode(WrapContentModifierImpl modifier) : IModifier.Node
    {
        public override void OnAttach() => Set(modifier);

        internal void Set(WrapContentModifierImpl next)
        {
            modifier = next;
            ((Box)Target).SetWrap(next);
        }

        public override void OnDetach()
        {
            ((Box)Target).SetWrap(null);
        }
    }
}

