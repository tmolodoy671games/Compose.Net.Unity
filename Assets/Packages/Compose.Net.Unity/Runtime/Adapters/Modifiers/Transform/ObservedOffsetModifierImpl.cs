using System;
using Compose.Net;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record ObservedOffsetModifierImpl(Func<Action<Offset>, IDisposable> Subscribe)
    : UnityModifierNodeElement<ObservedOffsetModifierImpl.ObservedOffsetNode>
{
    public override ObservedOffsetNode Create() => new(Subscribe);
    public override void Update(ObservedOffsetNode node) => node.Update(Subscribe);
    public override object GetTarget(IReusableComposeNode node) => node.VisualElement();

    internal sealed class ObservedOffsetNode(Func<Action<Offset>, IDisposable> subscribe) : OffsetPlacementNode(Offset.Zero)
    {
        private IDisposable? _subscription;
        private int _generation;

        public override void OnAttach()
        {
            base.OnAttach();
            StartSubscription();
        }

        internal void Update(Func<Action<Offset>, IDisposable> next)
        {
            if (Equals(subscribe, next)) return;
            StopSubscription();
            subscribe = next;
            SetOffset(Offset.Zero);
            if (IsAttached) StartSubscription();
        }

        public override void OnDetach()
        {
            try { StopSubscription(); }
            finally { base.OnDetach(); }
        }

        private void StartSubscription()
        {
            var generation = _generation;
            _subscription = subscribe(value =>
            {
                if (generation == _generation) SetOffset(value);
            });
        }

        private void StopSubscription()
        {
            _generation++;
            var subscription = _subscription;
            _subscription = null;
            subscription?.Dispose();
        }
    }
}
