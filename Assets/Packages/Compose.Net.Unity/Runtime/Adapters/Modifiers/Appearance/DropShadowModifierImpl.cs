// ReSharper disable CheckNamespace

using Compose.Net;
using Compose.Net.Unity.Runtime.Adapters.Extensions.Values;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record DropShadowModifierImpl(
    Shape Shape,
    Compose.Net.Shadow Shadow
) : ModifierNodeElement<DropShadowModifierImpl.DropShadowNode>, IAppearanceModifier
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
    private VisualElement? _shadowElement;

    public override void OnAttach()
    {
        _node = (UnityReusableComposeNode)Target;
        var host = inset ? _node.AcquireForegroundShadow() : _node.AcquireDrawBehind();
        _shadowElement = new VisualElement
        {
            pickingMode = UnityEngine.UIElements.PickingMode.Ignore,
            style =
            {
                position = Position.Absolute,
                top = 0,
                bottom = 0,
                left = 0,
                right = 0,
            }
        };
        host.Add(_shadowElement);
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
        _shadowElement?.RemoveFromHierarchy();
        _shadowElement = null;
        if (_node != null)
        {
            if (inset)
                _node.ReleaseForegroundShadow();
            else
                _node.ReleaseDrawBehind();
        }
        _node = null;
    }

    private void Apply()
    {
        if (_shadowElement == null)
            return;
        var style = _shadowElement.style;
        var density = UnityDensity.Current;
        style.borderTopLeftRadius = _shape.TopLeft.ToLength(density);
        style.borderTopRightRadius = _shape.TopRight.ToLength(density);
        style.borderBottomLeftRadius = _shape.BottomLeft.ToLength(density);
        style.borderBottomRightRadius = _shape.BottomRight.ToLength(density);
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
