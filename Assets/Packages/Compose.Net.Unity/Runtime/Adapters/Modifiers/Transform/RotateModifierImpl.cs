// ReSharper disable CheckNamespace

using Compose.Net;
using SharpExtensions;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record RotateModifierImpl(float Degrees) : ModifierNodeElement<RotateModifierImpl.RotateNode>
{
    public override RotateNode Create() => new(Degrees);
    public override void Update(RotateNode node) => node.Update(Degrees);
    public override object GetTarget(IReusableComposeNode node) => node.VisualElement();

    internal sealed class RotateNode(float degrees) : IModifier.Node
    {
        private float _degrees = degrees;
        private VisualElement? _element;

        public override void OnAttach()
        {
            _element = (VisualElement)Target;
            ApplyDelta(_degrees);
        }

        internal void Update(float next)
        {
            if (_degrees == next) return;
            ApplyDelta(-_degrees);
            _degrees = next;
            ApplyDelta(_degrees);
        }

        public override void OnDetach()
        {
            if (_element != null)
                ApplyDelta(-_degrees);
            _element = null;
        }

        private void ApplyDelta(float delta)
        {
            if (_element == null) return;
            var current = _element.style.rotate.value.angle.value;
            _element.style.rotate = ToNullIfNeeded(new Rotate(current + delta));
        }

        private static StyleRotate ToNullIfNeeded(Rotate rotate)
            => rotate.angle.value.AlmostEquals(0f) ? StyleKeyword.Null : rotate;
    }
}
