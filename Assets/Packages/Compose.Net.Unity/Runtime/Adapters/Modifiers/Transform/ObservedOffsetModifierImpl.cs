// ReSharper disable CheckNamespace

using System;
using Compose.Net;
using StableCollections;
using Compose.Net.Unity.Runtime.Adapters.Extensions.Values;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

// Offset pushed by a subscription, so it changes without recomposition (e.g. pager scroll).
internal record ObservedOffsetModifierImpl(
    Func<Action<Offset>, IDisposable> Subscribe
) : UnityModifier, IModifier
{
    public override ModifierNode CreateNode() => new ObservedOffsetNode(Subscribe);

    private sealed class ObservedOffsetNode : ModifierNode
    {
        private Func<Action<Offset>, IDisposable> _subscribe;
        internal ObservedOffsetNode(Func<Action<Offset>, IDisposable> subscribe) => _subscribe = subscribe;
        private IDisposable? _subscription;
        private ModifierValueBinding<StyleTranslate>? _value;
        public override bool CanUpdate(IModifier modifier) => modifier is ObservedOffsetModifierImpl;
        protected override void OnAttach()
        {
            _value = ((UnityReusableComposeNode)Owner).Styles.Translate.Add(this, new Translate(0, 0));
            _subscription = _subscribe(SetOffset);
        }
        public override void Update(IModifier modifier)
        {
            var next = ((ObservedOffsetModifierImpl)modifier).Subscribe;
            if (Equals(_subscribe, next)) return;
            _subscription?.Dispose();
            _subscribe = next;
            _subscription = _subscribe(SetOffset);
        }
        private void SetOffset(Offset offset) => _value?.Set(new Translate(offset.X, offset.Y));
        protected override void OnOrderChanged() => _value?.InvalidateOrder();
        protected override void OnDetach()
        {
            _subscription?.Dispose();
            _subscription = null;
            _value?.Dispose();
            _value = null;
        }
    }

}
