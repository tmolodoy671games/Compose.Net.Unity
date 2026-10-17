// ReSharper disable CheckNamespace

using System;
using Compose.Net;
using SharpExtensions;
using StableCollections;
using Compose.Net.Unity.Runtime.Adapters.Extensions.Values;
using Compose.Net.Unity.Runtime.Adapters.Nodes;
using UnityEngine.UIElements;
using TimeUtils = Compose.Net.Unity.Runtime.Adapters.Utils.TimeUtils;

namespace Compose.Net.Unity;

public class UnityReusableComposeNode : IReusableComposeNode
{
    public readonly VisualElement VisualElement;
    private AnchorManager? _anchorManager;

    private VisualElement? _contentContainerInstance;
    private VisualElement? _contentContainer;

    private DrawBehind? _drawBehindInstance;
    private DrawBehind? _drawBehind;

    // DrawOnTop content is drawn by the element itself while it has no children,
    // and by a separate DrawOn view on top of the children otherwise.
    private readonly IMutableStableList<Action<MeshGenerationContext>> _drawOnTopContent =
        MutableStableListOf<Action<MeshGenerationContext>>();
    private Action<MeshGenerationContext>? _drawOnTopInvoke;
    private bool _drawOnTopOnElement;
    private DrawOn? _drawOnInstance;
    private DrawOn? _drawOn;

    private Shadow? _foregroundShadow;
    private Shadow? _foregroundShadowInstance;

    public UnityReusableComposeNode(VisualElement visualElement) : this(visualElement, false)
    {
    }

    private VisualElement ContentContainer => _contentContainer ?? VisualElement;

    public void Dispose()
    {
        foreach (var entry in VisualElement.UserData().Values)
        {
            if (entry is IDisposable disposable and not IReusableComposeNode)
                disposable.Dispose();
        }
    }

    internal UnityReusableComposeNode(VisualElement visualElement, bool isRoot)
    {
        IsRoot = isRoot;
        VisualElement = visualElement;
        VisualElement.pickingMode = UnityEngine.UIElements.PickingMode.Ignore;
        visualElement.SetReusableComposeNode(this);
    }

    public void Remove(IReusableComposeNode child)
    {
        var childVisualElement = child.VisualElement();
        (ContentContainer as ILayout)?.Untrack(childVisualElement);
        ContentContainer.Remove(childVisualElement);
        SyncDrawOnTop();
    }

    public void FastRemove(int index, IReusableComposeNode child)
    {
        var childVisualElement = child.VisualElement();
        if (childVisualElement.parent != ContentContainer)
            return;
        (ContentContainer as ILayout)?.Untrack(childVisualElement);
        if (_drawBehind != null)
            index++;
        if (ContentContainer.GetOrNull(index) == childVisualElement)
            ContentContainer.RemoveAt(index);
        else
            ContentContainer.Remove(childVisualElement);
        SyncDrawOnTop();
    }

    public void Insert(int index, IReusableComposeNode child)
    {
        if (_drawBehind != null)
            index++;
        ContentContainer.Insert(index, child.VisualElement());
        (ContentContainer as ILayout)?.Track(child.VisualElement());
        SyncDrawOnTop();
    }

    public void Reinsert(int index, IReusableComposeNode child)
    {
        if (_drawBehind != null)
            index++;
        var childVisualElement = child.VisualElement();
        var parent = ContentContainer;
        if (parent.GetOrNull(index) != childVisualElement)
        {
            (childVisualElement.parent as ILayout)?.Untrack(childVisualElement);
            childVisualElement.RemoveFromHierarchy();
            parent.Insert(index, childVisualElement);
        }

        // Compose.Net inserts every child through Reinsert, so this is where a layout
        // has to start positioning it.
        (parent as ILayout)?.Track(childVisualElement);
        SyncDrawOnTop();
    }

    public AnchorManager RequireAnchorManager()
    {
        if (_anchorManager == null)
            _anchorManager = AnchorManager.Get();
        return _anchorManager;
    }

    public bool IsRoot { get; }
    public IReusableComposeNode? Parent => VisualElement.parent?.GetReusableComposeNode();
    public int IndexInParent => VisualElement.parent.IndexOf(VisualElement);
    public AnchorManager? AnchorManager => _anchorManager;

    public void InsertDrawOnTop(int index, Action<MeshGenerationContext> content)
    {
        index = Math.Min(index, _drawOnTopContent.Size);
        _drawOnTopContent.Add(index, content);
        SyncDrawOnTop(attach: true);
        DrawOnTopHost()?.MarkDirtyRepaint();
    }

    /// <param name="keepHost">
    /// Keep the host even if no content is left, because another DrawOnTop modifier is about to be applied.
    /// </param>
    public void RemoveDrawOnTop(Action<MeshGenerationContext> content, bool keepHost)
    {
        _drawOnTopContent.Remove(content);
        if (_drawOnTopContent.Size == 0 && !keepHost)
        {
            DetachDrawOnTopFromElement();
            DetachDrawOnTopView();
            return;
        }

        DrawOnTopHost()?.MarkDirtyRepaint();
    }

    // Moves DrawOnTop content between the element and the DrawOn view when children appear or disappear.
    private void SyncDrawOnTop(bool attach = false)
    {
        var attached = _drawOn != null || _drawOnTopOnElement;
        if (!attached && !attach)
            return;
        var hasChildren = VisualElement.childCount > (_drawOn != null ? 1 : 0);
        if (hasChildren)
        {
            DetachDrawOnTopFromElement();
            AttachDrawOnTopView();
        }
        else
        {
            DetachDrawOnTopView();
            AttachDrawOnTopToElement();
        }
    }

    private VisualElement? DrawOnTopHost()
    {
        if (_drawOn != null)
            return _drawOn;
        return _drawOnTopOnElement ? VisualElement : null;
    }

    private void InvokeDrawOnTop(MeshGenerationContext context)
    {
        foreach (var content in _drawOnTopContent)
            content(context);
    }

    private void AttachDrawOnTopToElement()
    {
        if (_drawOnTopOnElement)
            return;
        _drawOnTopInvoke ??= InvokeDrawOnTop;
        VisualElement.GenerateVisualContent().OnTop = _drawOnTopInvoke;
        _drawOnTopOnElement = true;
    }

    private void DetachDrawOnTopFromElement()
    {
        if (!_drawOnTopOnElement)
            return;
        VisualElement.GenerateVisualContent().OnTop = null;
        _drawOnTopOnElement = false;
    }

    private void AttachDrawOnTopView()
    {
        if (_drawOn != null)
        {
            // Children are inserted before the view, but keep it last just in case.
            if (VisualElement.ElementAt(VisualElement.childCount - 1) != _drawOn)
                _drawOn.BringToFront();
            return;
        }

        _drawOnTopInvoke ??= InvokeDrawOnTop;
        _drawOnInstance ??= new DrawOn
        {
            pickingMode = UnityEngine.UIElements.PickingMode.Ignore,
            generateVisualContent = _drawOnTopInvoke,
            style =
            {
                position = Position.Absolute,
                top = 0,
                bottom = 0,
                left = 0,
                right = 0,
            }
        };
        _drawOn = _drawOnInstance;
        VisualElement.Add(_drawOn);
        _drawOn.MarkDirtyRepaint();
    }

    private void DetachDrawOnTopView()
    {
        if (_drawOn == null)
            return;
        var lastIndex = VisualElement.childCount - 1;
        if (VisualElement.GetOrNull(lastIndex) == _drawOn)
            VisualElement.RemoveAt(lastIndex);
        else
            _drawOn.RemoveFromHierarchy();
        _drawOn = null;
    }

    public VisualElement SetupForegroundShadow()
    {
        if (_foregroundShadow != null)
            return _foregroundShadow;
        _foregroundShadowInstance ??= new Shadow
        {
            pickingMode = UnityEngine.UIElements.PickingMode.Ignore,
            style =
            {
                position = Position.Absolute,
                top = 0,
                bottom = 0,
                left = 0,
                right = 0,
            }
        };
        _foregroundShadow = _foregroundShadowInstance;
        VisualElement.Insert(ForegroundShadowIndex(), _foregroundShadow);
        SyncDrawOnTop();
        return _foregroundShadow;
    }

    public void RemoveForegroundShadow()
    {
        if (_foregroundShadow == null)
            return;
        if (VisualElement.GetOrNull(ForegroundShadowIndex()) == _foregroundShadow)
            VisualElement.RemoveAt(ForegroundShadowIndex());
        else
            VisualElement.Remove(_foregroundShadow);
        _foregroundShadow = null;
        SyncDrawOnTop();
    }

    public VisualElement SetupDrawBehind()
    {
        if (_drawBehind != null)
            return _drawBehind;
        _drawBehindInstance ??= new DrawBehind
        {
            pickingMode = UnityEngine.UIElements.PickingMode.Ignore,
            style =
            {
                position = Position.Absolute,
                top = 0,
                bottom = 0,
                left = 0,
                right = 0,
            }
        };
        _drawBehind = _drawBehindInstance;
        ContentContainer.Insert(0, _drawBehind);
        SyncDrawOnTop();
        return _drawBehind;
    }

    public void RemoveDrawBehind()
    {
        if (_drawBehind == null)
            return;
        ContentContainer.Remove(_drawBehind);
        _drawBehind = null;
        SyncDrawOnTop();
    }

    public VisualElement SetupContentContainer()
    {
        if (_contentContainer != null)
            return _contentContainer;
        _contentContainerInstance ??= VisualElement is Box box
            ? box.CreateContentLayout()
            : new Content
            {
                pickingMode = UnityEngine.UIElements.PickingMode.Ignore,
            };
        _contentContainer = _contentContainerInstance;
        var children = VisualElement.Children().ToPersistentStableList();
        foreach (var child in children)
        {
            // The Box spacer and DrawOn belong to the element itself, not to its content.
            if (child is BoxPull || child == _drawOn)
                continue;
            (VisualElement as ILayout)?.Untrack(child);
            child.parent.Remove(child);
        }

        VisualElement.Insert(0, _contentContainerInstance);
        // With a content container (scroll, animateContentSize) the layout positions the container itself.
        (VisualElement as ILayout)?.Track(_contentContainerInstance);
        foreach (var child in children)
        {
            if (child is BoxPull || child == _drawOn)
                continue;
            _contentContainerInstance.Add(child);
            (_contentContainerInstance as ILayout)?.Track(child);
        }

        SyncDrawOnTop();
        return _contentContainer;
    }

    public void RemoveContentContainer()
    {
        if (_contentContainer == null)
            return;
        var children = _contentContainer.Children().ToPersistentStableList();
        foreach (var child in children)
        {
            if (child is BoxPull)
                continue;
            (_contentContainer as ILayout)?.Untrack(child);
            child.parent.Remove(child);
        }

        (VisualElement as ILayout)?.Untrack(_contentContainer);
        VisualElement.Remove(_contentContainer);
        foreach (var child in children)
        {
            if (child is BoxPull)
                continue;
            VisualElement.Add(child);
            (VisualElement as ILayout)?.Track(child);
        }

        _contentContainer = null;
        SyncDrawOnTop();
    }

    public override string ToString()
    {
        return VisualElement.GetType().Name;
    }

    private int ForegroundShadowIndex()
    {
        var index = VisualElement.childCount;
        if (_drawOn != null)
            index--;
        return index;
    }
}

public static class ReusableComposeNodeExtensions
{
    public static VisualElement VisualElement(this IReusableComposeNode node)
    {
        return node.CastTo<UnityReusableComposeNode>().VisualElement;
    }

    public static T VisualElement<T>(this IReusableComposeNode node) where T : VisualElement
    {
        return node.CastTo<UnityReusableComposeNode>().VisualElement.CastTo<T>();
    }
}

internal static class ReusableNodeVisualElementExtensions
{
    private const string ReusableComposeNodeKey = "ReusableComposeNode";

    internal static IReusableComposeNode? GetReusableComposeNode(this VisualElement visualElement)
    {
        return visualElement.UserData().GetOrDefault(ReusableComposeNodeKey, null) as IReusableComposeNode;
    }

    internal static void SetReusableComposeNode(
        this VisualElement visualElement,
        IReusableComposeNode node
    )
    {
        visualElement.UserData()[ReusableComposeNodeKey] = node;
    }
}

internal class DrawOn : VisualElement
{
}

internal class DrawBehind : VisualElement
{
}

internal class Shadow : VisualElement
{
}