// ReSharper disable CheckNamespace

using Compose.Net;
using Compose.Net.Unity.Runtime.Adapters.Extensions.Values;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record DropShadowModifierImpl(
    Shape Shape,
    Compose.Net.Shadow Shadow
) : UnityModifierElement<DropShadowModifierImpl.DropShadowNode>, IAppearanceModifier
{
    public override DropShadowNode Create() => new(Shape, Shadow);
    public override void Update(DropShadowNode node) => node.Update(Shape, Shadow);
    public override object GetTarget(IReusableComposeNode node) => node;

    internal sealed class DropShadowNode(Shape shape, Compose.Net.Shadow shadow)
        : BoxShadowNode(shape, shadow, inset: false);
}

// Each modifier owns a separate box so shadows can coexist and use independent corner radii.
internal abstract class BoxShadowNode(Shape shape, Compose.Net.Shadow shadow, bool inset) : IModifier.Node
{
    private Shape _shape = shape;
    private Compose.Net.Shadow _shadow = shadow;
    private UnityReusableComposeNode? _node;

    public override void OnAttach()
    {
        _node = (UnityReusableComposeNode)Target;
        Apply();
    }

    internal void Update(Shape nextShape, Compose.Net.Shadow nextShadow)
    {
        if (_shape == nextShape && _shadow == nextShadow)
            return;
        _shape = nextShape;
        _shadow = nextShadow;
        Apply();
    }

    public override void OnDetach()
    {
        _node = null;
    }

    private void Apply()
    {
        if (_node == null)
            return;
        var style = _node.VisualElement.style;
        var density = UnityDensity.Current;
        style.boxShadow = new BoxShadow
        {
            inset = inset,
            offset = _shadow.Offset.ToVector2(),
            blurRadius = _shadow.Radius.ToPx(density),
            spreadRadius = _shadow.Spread.ToPx(density),
            color = _shadow.Color.ToUnityColor(),
        };
    }
}
