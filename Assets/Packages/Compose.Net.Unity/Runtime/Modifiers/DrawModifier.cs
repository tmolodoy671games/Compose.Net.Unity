// ReSharper disable CheckNamespace

using System;
using Compose.Net;
using StableCollections;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

public interface IDrawModifier : IAppearanceModifier
{
}

public abstract record DrawUnityModifier(int DrawIndex = -1)
    : ModifierNodeElement<DrawUnityModifier.DrawNode>, IDrawModifier
{
    protected abstract Action<MeshGenerationContext> GenerateVisualContent { get; }

    public sealed override DrawNode Create() => new(this);

    public sealed override void Update(DrawNode node) => node.Update(this);

    public override object GetTarget(IReusableComposeNode node) => node.VisualElement();

    public sealed override IModifier Fill(IStableList<IModifier> modifiers)
    {
        var index = 0;
        for (var i = 0; i < modifiers.Size; i++)
        {
            var modifier = modifiers[i];
            if (ReferenceEquals(modifier, this))
                break;
            if (modifier is IDrawModifier)
                index++;
        }

        return DrawIndex == index ? this : this with { DrawIndex = index };
    }

    protected virtual void OnAttached(VisualElement element)
    {
    }

    protected virtual void OnDetached(VisualElement element)
    {
    }

    public sealed class DrawNode(DrawUnityModifier owner) : IModifier.Node
    {
        private DrawUnityModifier _owner = owner;
        private int _drawIndex = owner.DrawIndex;
        private Action<MeshGenerationContext>? _insertedCallback;
        private VisualElement? _element;

        internal void Update(DrawUnityModifier next)
        {
            var indexChanged = _drawIndex != next.DrawIndex;
            var contentChanged = !ReferenceEquals(_owner.GenerateVisualContent, next.GenerateVisualContent);
            _owner = next;
            if (_element == null)
                return;
            if (!indexChanged && !contentChanged)
                return;
            RemoveCallback();
            _drawIndex = next.DrawIndex;
            InsertCallback();
            next.OnAttached(_element);
        }

        public override void OnAttach()
        {
            _element = (VisualElement)Target;
            _drawIndex = _owner.DrawIndex;
            InsertCallback();
            _owner.OnAttached(_element);
        }

        public override void OnDetach()
        {
            if (_element != null)
            {
                RemoveCallback();
                _owner.OnDetached(_element);
            }

            _element = null;
        }

        private void InsertCallback()
        {
            _insertedCallback = _owner.GenerateVisualContent;
            _element!.GenerateVisualContent().Insert(_drawIndex, _insertedCallback);
        }

        private void RemoveCallback()
        {
            if (_element != null && _insertedCallback != null)
                _element.GenerateVisualContent().Remove(_insertedCallback);
            _insertedCallback = null;
        }
    }
}
