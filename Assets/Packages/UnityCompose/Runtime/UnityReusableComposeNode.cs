// ReSharper disable CheckNamespace

using System;
using Compose.Net;
using SharpExtensions;
using StableCollections;
using UnityCompose.Packages.UnityCompose.Runtime.Adapters.Extensions.Values;
using UnityCompose.Packages.UnityCompose.Runtime.Adapters.Nodes;
using UnityEngine.UIElements;
using TimeUtils = UnityCompose.Packages.UnityCompose.Runtime.Adapters.Utils.TimeUtils;

namespace UnityCompose;

public class UnityReusableComposeNode : IReusableComposeNode
{
    public readonly VisualElement VisualElement;
    private AnchorManager? _anchorManager;

    private Content? _contentContainerInstance;
    private Content? _contentContainer;

    private DrawBehind? _drawBehindInstance;
    private DrawBehind? _drawBehind;

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
        ContentContainer.Remove(childVisualElement);
    }

    public void FastRemove(int index, IReusableComposeNode child)
    {
        var childVisualElement = child.VisualElement();
        if (childVisualElement.parent != VisualElement)
            return;
        if (_drawBehind != null)
            index++;
        if (ContentContainer.GetOrNull(index) == childVisualElement)
        {
            ContentContainer.RemoveAt(index);
            return;
        }

        ContentContainer.Remove(childVisualElement);
    }

    public void Insert(int index, IReusableComposeNode child)
    {
        if (_drawBehind != null)
            index++;
        ContentContainer.Insert(index, child.VisualElement());
    }

    public void Reinsert(int index, IReusableComposeNode child)
    {
        if (_drawBehind != null)
            index++;
        var childVisualElement = child.VisualElement();
        var parent = ContentContainer;
        if (parent.GetOrNull(index) == childVisualElement)
            return;
        childVisualElement.RemoveFromHierarchy();
        parent.Insert(index, childVisualElement);
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

    public VisualElement SetupDrawOnTop()
    {
        if (_drawOn != null)
            return _drawOn;
        _drawOnInstance ??= new DrawOn
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
        _drawOn = _drawOnInstance;
        VisualElement.Insert(DrawOnIndex(), _drawOn);
        return _drawOn;
    }

    public void RemoveDrawOnTop()
    {
        if (_drawOn == null)
            return;
        if (VisualElement.GetOrNull(DrawOnIndex()) == _drawOn)
            VisualElement.RemoveAt(DrawOnIndex());
        else
            VisualElement.Remove(_drawOn);
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
        return _drawBehind;
    }

    public void RemoveDrawBehind()
    {
        if (_drawBehind == null)
            return;
        ContentContainer.Remove(_drawBehind);
        _drawBehind = null;
    }

    public VisualElement SetupContentContainer()
    {
        if (_contentContainer != null)
            return _contentContainer;
        _contentContainerInstance ??= new Content
        {
            pickingMode = UnityEngine.UIElements.PickingMode.Ignore,
        };
        _contentContainer = _contentContainerInstance;
        var children = VisualElement.Children().ToPersistentStableList();
        foreach (var child in children)
            child.parent.Remove(child);
        VisualElement.Insert(0, _contentContainerInstance);
        foreach (var child in children)
            _contentContainerInstance.Add(child);
        return _contentContainer;
    }

    public void RemoveContentContainer()
    {
        if (_contentContainer == null)
            return;
        var children = _contentContainer.Children().ToPersistentStableList();
        foreach (var child in children)
            child.parent.Remove(child);
        VisualElement.Remove(_contentContainer);
        foreach (var child in children)
            VisualElement.Add(child);
        _contentContainer = null;
    }

    public override string ToString()
    {
        return VisualElement.GetType().Name;
    }

    private int DrawOnIndex()
    {
        var index = VisualElement.childCount;
        return index;
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