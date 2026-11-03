using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal record PointerInputModifierImpl<T>(T Key, Func<IPointerInputScope, Task> Body)
    : ModifierNodeElement<PointerInputModifierImpl<T>.PointerNode>
{
    public override PointerNode Create() => new(Key, Body);
    public override void Update(PointerNode node) => node.Update(Key, Body);
    public override object GetTarget(IReusableComposeNode node) => node.VisualElement();

    internal sealed class PointerNode(T key, Func<IPointerInputScope, Task> body) : IModifier.Node
    {
        private T _key = key;
        private Func<IPointerInputScope, Task> _body = body;
        private PointerInputScopeImpl? _handler;
        public override void OnAttach()
        {
            var element = (VisualElement)Target;
            element.PickingMode().Increment();
            _handler = new PointerInputScopeImpl(element, _body);
            _handler.Attach();
        }
        internal void Update(T nextKey, Func<IPointerInputScope, Task> nextBody)
        {
            var reset = !EqualityComparer<T>.Default.Equals(_key, nextKey) || _body.Method != nextBody.Method;
            _key = nextKey; _body = nextBody;
            _handler?.Update(nextBody, reset);
        }
        public override void OnDetach()
        {
            _handler?.Dispose(); _handler = null;
            ((VisualElement)Target).PickingMode().Decrement();
        }
    }
}
