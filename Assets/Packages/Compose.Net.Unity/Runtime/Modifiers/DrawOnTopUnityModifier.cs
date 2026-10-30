// ReSharper disable CheckNamespace

using System;
using Compose.Net;
using StableCollections;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

public interface IDrawOnModifier : IAppearanceModifier
{
}

public abstract record DrawOnTopUnityModifier(int DrawIndex = -1)
    : ModifierNodeElement<DrawOnTopUnityModifier.DrawOnTopNode>, IDrawOnModifier
{
    protected abstract Action<MeshGenerationContext> GenerateVisualContent { get; }

    public sealed override DrawOnTopNode Create() => new(this);

    public sealed override void Update(DrawOnTopNode node) => node.Update(this);

    public override object GetTarget(IReusableComposeNode node) => node;

    public sealed override IModifier Fill(IStableList<IModifier> modifiers)
    {
        var index = 0;
        for (var i = 0; i < modifiers.Size; i++)
        {
            var modifier = modifiers[i];
            if (ReferenceEquals(modifier, this))
                break;
            if (modifier is IDrawOnModifier)
                index++;
        }

        return DrawIndex == index ? this : this with { DrawIndex = index };
    }

    protected virtual void OnAttached(UnityReusableComposeNode node, VisualElement element)
    {
    }

    protected virtual void OnDetached(UnityReusableComposeNode node, VisualElement element)
    {
    }

    public sealed class DrawOnTopNode(DrawOnTopUnityModifier owner) : IModifier.Node
    {
        private DrawOnTopUnityModifier _owner = owner;
        private int _drawIndex = owner.DrawIndex;
        private Action<MeshGenerationContext>? _content;
        private UnityReusableComposeNode? _unityNode;

        internal void Update(DrawOnTopUnityModifier next)
        {
            var indexChanged = _drawIndex != next.DrawIndex;
            var contentChanged = !ReferenceEquals(_owner.GenerateVisualContent, next.GenerateVisualContent);
            _owner = next;
            if (_unityNode == null)
                return;
            if (!indexChanged && !contentChanged)
                return;
            if (_content != null)
                _unityNode.RemoveDrawOnTop(_content, keepHost: true);
            _drawIndex = next.DrawIndex;
            _content = next.GenerateVisualContent;
            _unityNode.InsertDrawOnTop(_drawIndex, _content);
            next.OnAttached(_unityNode, _unityNode.VisualElement);
        }

        public override void OnAttach()
        {
            _unityNode = (UnityReusableComposeNode)Target;
            _drawIndex = _owner.DrawIndex;
            _content = _owner.GenerateVisualContent;
            _unityNode.InsertDrawOnTop(_drawIndex, _content);
            _owner.OnAttached(_unityNode, _unityNode.VisualElement);
        }

        public override void OnDetach()
        {
            if (_unityNode != null && _content != null)
            {
                _unityNode.RemoveDrawOnTop(_content, keepHost: false);
                _owner.OnDetached(_unityNode, _unityNode.VisualElement);
            }

            _unityNode = null;
            _content = null;
        }
    }
}
