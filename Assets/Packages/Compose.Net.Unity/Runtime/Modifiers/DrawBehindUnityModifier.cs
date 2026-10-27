// ReSharper disable CheckNamespace

using System;
using Compose.Net;
using StableCollections;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

public interface IDrawBehindModifier : IAppearanceModifier
{
}

public abstract record DrawBehindUnityModifier(int DrawIndex = -1)
    : ModifierNodeElement<DrawBehindUnityModifier.DrawBehindNode>, IDrawBehindModifier
{
    protected abstract Action<MeshGenerationContext> GenerateVisualContent { get; }

    public sealed override DrawBehindNode Create() => new(this);

    public sealed override void Update(DrawBehindNode node) => node.Update(this);

    public override object GetTarget(IReusableComposeNode node) => node;

    public sealed override IModifier Fill(IStableList<IModifier> modifiers)
    {
        var index = 0;
        for (var i = 0; i < modifiers.Size; i++)
        {
            var modifier = modifiers[i];
            if (ReferenceEquals(modifier, this))
                break;
            if (modifier is IDrawBehindModifier)
                index++;
        }

        return DrawIndex == index ? this : this with { DrawIndex = index };
    }

    protected virtual void OnAttached(
        UnityReusableComposeNode node,
        VisualElement element,
        VisualElement drawBehind
    )
    {
    }

    protected virtual void OnDetached(
        UnityReusableComposeNode node,
        VisualElement element,
        VisualElement drawBehind
    )
    {
    }

    public sealed class DrawBehindNode(DrawBehindUnityModifier owner) : IModifier.Node
    {
        private DrawBehindUnityModifier _owner = owner;
        private int _drawIndex = owner.DrawIndex;
        private Action<MeshGenerationContext>? _insertedCallback;
        private UnityReusableComposeNode? _unityNode;
        private VisualElement? _drawBehind;

        internal void Update(DrawBehindUnityModifier next)
        {
            var indexChanged = _drawIndex != next.DrawIndex;
            var contentChanged = !ReferenceEquals(_owner.GenerateVisualContent, next.GenerateVisualContent);
            _owner = next;
            if (_drawBehind == null)
                return;
            if (!indexChanged && !contentChanged)
                return;
            RemoveCallback();
            _drawIndex = next.DrawIndex;
            InsertCallback();
            next.OnAttached(_unityNode!, _unityNode!.VisualElement, _drawBehind);
        }

        public override void OnAttach()
        {
            _unityNode = (UnityReusableComposeNode)Target;
            _drawBehind = _unityNode.AcquireDrawBehind();
            _drawIndex = _owner.DrawIndex;
            InsertCallback();
            _owner.OnAttached(_unityNode, _unityNode.VisualElement, _drawBehind);
        }

        public override void OnDetach()
        {
            if (_drawBehind != null && _unityNode != null)
            {
                RemoveCallback();
                _owner.OnDetached(_unityNode, _unityNode.VisualElement, _drawBehind);
                _unityNode.ReleaseDrawBehind();
            }

            _drawBehind = null;
            _unityNode = null;
        }

        private void InsertCallback()
        {
            _insertedCallback = _owner.GenerateVisualContent;
            _drawBehind!.GenerateVisualContent().Insert(_drawIndex, _insertedCallback);
        }

        private void RemoveCallback()
        {
            if (_drawBehind != null && _insertedCallback != null)
                _drawBehind.GenerateVisualContent().Remove(_insertedCallback);
            _insertedCallback = null;
        }
    }
}
