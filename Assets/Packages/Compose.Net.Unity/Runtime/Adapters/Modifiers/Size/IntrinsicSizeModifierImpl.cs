namespace Compose.Net.Unity;

internal sealed record IntrinsicSizeModifierImpl(Orientation Axis, IntrinsicSize Size)
    : UnityModifierNodeElement<IntrinsicSizeModifierImpl.IntrinsicNode>, IContentMeasureModifier
{
    public override IntrinsicNode Create() => new(this);
    public override void Update(IntrinsicNode node) => node.Set(this);
    public override object GetTarget(IReusableComposeNode node) => node.VisualElement<Box>();

    internal sealed class IntrinsicNode(IntrinsicSizeModifierImpl modifier) : IModifier.Node
    {
        public override void OnAttach() => Set(modifier);

        internal void Set(IntrinsicSizeModifierImpl next)
        {
            modifier = next;
            ((Box)Target).SetIntrinsic(next);
        }

        public override void OnDetach() => ((Box)Target).SetIntrinsic(null);
    }
}

