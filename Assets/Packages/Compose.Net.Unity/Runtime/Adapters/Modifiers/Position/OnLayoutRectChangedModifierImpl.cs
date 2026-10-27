// ReSharper disable CheckNamespace

using System;
using Compose.Net;
using SharpExtensions;
using UnityEngine.UIElements;
using TimeUtils = Compose.Net.Unity.Runtime.Adapters.Utils.TimeUtils;

namespace Compose.Net.Unity;

internal record OnLayoutRectChangedModifierImpl(Action<RelativeLayoutBounds> OnLayoutRectChanged)
    : ModifierNodeElement<OnLayoutRectChangedModifierImpl.LayoutRectNode>
{
    public override LayoutRectNode Create() => new(OnLayoutRectChanged);
    public override void Update(LayoutRectNode node) => node.Update(OnLayoutRectChanged);
    public override object GetTarget(IReusableComposeNode node) => node.VisualElement();

    internal sealed class LayoutRectNode(Action<RelativeLayoutBounds> callback) : IModifier.Node
    {
        private Action<RelativeLayoutBounds> _callback = callback;
        private Optional<RelativeLayoutBounds> _previousRect;
        private VisualElement? _element;
        private IVisualElementScheduledItem? _scheduled;

        public override void OnAttach()
        {
            _element = (VisualElement)Target;
            _scheduled = _element.schedule.Execute(Check).Every(TimeUtils.Frametime);
            Check();
        }

        internal void Update(Action<RelativeLayoutBounds> next)
        {
            if (Equals(_callback, next)) return;
            _callback = next;
            _previousRect = default;
            Check();
        }

        public override void OnDetach()
        {
            _scheduled?.Pause();
            _scheduled = null;
            _element = null;
        }

        private void Check()
        {
            if (_element == null) return;
            var newCoordinates = _element.LayoutCoordinates();
            var newRect = newCoordinates.ToRelativeLayoutBounds();
            if (_previousRect.Equals(newRect)) return;
            _previousRect = newRect.ToOptional();
            _callback(newRect);
        }
    }
}
