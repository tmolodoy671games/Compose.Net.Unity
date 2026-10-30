using System;
using Compose.Net;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record ObservedSizeModifierImpl(Func<Action<Size>, IDisposable> Subscribe)
    : ModifierNodeElement<ObservedSizeModifierImpl.SizeNode>
{
    public override SizeNode Create() => new(Subscribe);
    public override void Update(SizeNode node) => node.Update(Subscribe);
    public override object GetTarget(IReusableComposeNode node) => node.VisualElement();

    internal sealed class SizeNode(Func<Action<Size>, IDisposable> subscribe) : IModifier.Node
    {
        private Func<Action<Size>, IDisposable> _subscribe = subscribe;
        private IDisposable? _subscription;
        private VisualElement? _element;
        private StyleLength _width, _height;

        public override void OnAttach()
        {
            _element = (VisualElement)Target;
            _width = _element.style.width;
            _height = _element.style.height;
            _subscription = _subscribe(SetSize);
        }

        internal void Update(Func<Action<Size>, IDisposable> next)
        {
            if (Equals(_subscribe, next)) return;
            _subscription?.Dispose();
            _subscribe = next;
            if (_element != null) _subscription = _subscribe(SetSize);
        }

        private void SetSize(Size size)
        {
            if (_element == null || size.IsUnspecified) return;
            // UI Toolkit geometry and these pixel sizes use the same panel units.
            if (!float.IsNaN(size.Width) && !float.IsInfinity(size.Width)) _element.style.width = size.Width;
            if (!float.IsNaN(size.Height) && !float.IsInfinity(size.Height)) _element.style.height = size.Height;
        }

        public override void OnDetach()
        {
            _subscription?.Dispose();
            _subscription = null;
            if (_element != null) { _element.style.width = _width; _element.style.height = _height; }
            _element = null;
        }
    }
}
