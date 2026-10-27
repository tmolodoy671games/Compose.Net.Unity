// ReSharper disable CheckNamespace

using Compose.Net;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record ScaleModifierImpl(float ScaleX, float ScaleY) : ModifierNodeElement<ScaleModifierImpl.ScaleNode>
{
    public override ScaleNode Create() => new(ScaleX, ScaleY);
    public override void Update(ScaleNode node) => node.Update(ScaleX, ScaleY);
    public override object GetTarget(IReusableComposeNode node) => node.VisualElement();

    internal sealed class ScaleNode(float scaleX, float scaleY) : IModifier.Node
    {
        private float _scaleX = scaleX;
        private float _scaleY = scaleY;
        private StyleScale _original;
        private VisualElement? _element;

        public override void OnAttach()
        {
            _element = (VisualElement)Target;
            _original = _element.style.scale;
            _element.style.scale = new Vector2(_scaleX, _scaleY);
        }

        internal void Update(float nextX, float nextY)
        {
            if (_scaleX == nextX && _scaleY == nextY) return;
            _scaleX = nextX;
            _scaleY = nextY;
            if (_element != null)
                _element.style.scale = new Vector2(nextX, nextY);
        }

        public override void OnDetach()
        {
            if (_element != null)
                _element.style.scale = _original;
            _element = null;
        }
    }
}
