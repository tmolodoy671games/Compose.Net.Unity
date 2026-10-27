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
) : UnityModifier
{
    private const string Key = "Compose.Net.Unity_ObservedOffset";

    protected override void Apply(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    )
    {
        var state = new State(element);
        // Stored in user data, so the subscription is also disposed together with the node.
        element.UserData()[Key] = state;
        state.Subscription = Subscribe(state.SetOffset);
    }

    protected override void Revert(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    )
    {
        var data = element.UserData();
        if (data.GetOrNull(Key) is not State state)
            return;
        data[Key] = null;
        state.Dispose();
        state.SetOffset(Offset.Zero);
    }

    private sealed class State : IDisposable
    {
        private readonly VisualElement _element;
        private Offset _offset = Offset.Zero;

        public IDisposable? Subscription;

        public State(VisualElement element)
        {
            _element = element;
        }

        // Offset modifiers also use translate, so only our own part is replaced.
        public void SetOffset(Offset offset)
        {
            var style = _element.style;
            style.translate = style.translate
                .Subtract(new Translate(_offset.X, _offset.Y))
                .Add(new Translate(offset.X, offset.Y));
            _offset = offset;
        }

        public void Dispose()
        {
            Subscription?.Dispose();
            Subscription = null;
        }
    }
}
