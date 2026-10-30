// ReSharper disable CheckNamespace

using Compose.Net;

namespace Compose.Net.Unity;

internal record InnerShadowModifierImpl(
    Shape Shape,
    Compose.Net.Shadow Shadow
) : UnityModifierElement<InnerShadowModifierImpl.InnerShadowNode>
{
    public override InnerShadowNode Create() => new(Shape, Shadow);
    public override void Update(InnerShadowNode node) => node.Update(Shape, Shadow);
    public override object GetTarget(IReusableComposeNode node) => node;

    internal sealed class InnerShadowNode(Shape shape, Compose.Net.Shadow shadow)
        : BoxShadowNode(shape, shadow, inset: true);
}
