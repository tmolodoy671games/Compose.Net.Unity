using System;
using Compose.Net;
using SharpExtensions;
using UnityEngine.UIElements;
using ScaleMode = Compose.Net.ScaleMode;

namespace Compose.Net.Unity.Runtime.Adapters.Nodes;

internal class ImageNodeFactoryImpl : IImageVectorNodeFactory
{
    public IReusableComposeNode CreateNode()
    {
#if COMPOSE_UNITY_VECTOR_GRAPHICS && UNITY_6000_3_OR_NEWER
        return new UnityReusableComposeNode(new ComposeImageElement());
#else
        return new UnityReusableComposeNode(new Image());
#endif
    }

    public void Apply(IReusableComposeNode node, IImageBitmap bitmap, ScaleMode scaleMode)
    {
        var element = node.VisualElement().CastTo<Image>();
#if COMPOSE_UNITY_VECTOR_GRAPHICS && UNITY_6000_3_OR_NEWER
        ((ComposeImageElement)element).ClearVector();
#endif
        bitmap.SetImage(node);
        element.scaleMode = scaleMode.ToUnityScaleMode();
    }

    public void Apply(IReusableComposeNode node, IPainter painter, ScaleMode scaleMode)
    {
#if COMPOSE_UNITY_VECTOR_GRAPHICS && UNITY_6000_3_OR_NEWER
        node.VisualElement<ComposeImageElement>().ClearVector();
#endif
        painter.SetImage(node);
    }

    public void Apply(IReusableComposeNode node, ImageVector imageVector, ScaleMode scaleMode, Size intrinsicSize,
        Color tint)
    {
#if COMPOSE_UNITY_VECTOR_GRAPHICS && UNITY_6000_3_OR_NEWER
        var element = node.VisualElement<ComposeImageElement>();
        element.SetVector(imageVector, intrinsicSize, tint);
        element.scaleMode = scaleMode.ToUnityScaleMode();
#else
        throw new NotSupportedException("Compose ImageVector rendering requires Unity 6.3 or newer with the Vector Graphics module enabled.");
#endif
    }
}

internal static class ScaleModeExtensions
{
    public static UnityEngine.ScaleMode ToUnityScaleMode(this ScaleMode scaleMode)
    {
        return scaleMode switch
        {
            ScaleMode.FillBounds => UnityEngine.ScaleMode.StretchToFill,
            ScaleMode.Crop => UnityEngine.ScaleMode.ScaleAndCrop,
            ScaleMode.Fit => UnityEngine.ScaleMode.ScaleToFit,
            _ => throw new ArgumentOutOfRangeException(nameof(scaleMode), scaleMode, null)
        };
    }
}