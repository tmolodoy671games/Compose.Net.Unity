using System;
using Compose.Net;
using SharpExtensions;
using UnityEngine;
using UnityEngine.UIElements;
using ScaleMode = Compose.Net.ScaleMode;

namespace Compose.Net.Unity.Runtime.Adapters.Nodes;

internal class ImageNodeFactoryImpl : IImageNodeFactory
{
    public IReusableComposeNode CreateNode()
    {
        return new UnityReusableComposeNode(new Image
        {
            name = null
        });
    }

    public void Apply(IReusableComposeNode node, IImageBitmap bitmap, ScaleMode scaleMode, string? contentDescription = null)
    {
        var element = node.VisualElement().CastTo<Image>();
        bitmap.SetImage(node);
        element.scaleMode = scaleMode.ToUnityScaleMode();
        element.UserData()["Compose.Net.ContentDescription"] = contentDescription;
    }

    public void Apply(IReusableComposeNode node, IPainter painter, ScaleMode scaleMode, string? contentDescription = null)
    {
        painter.SetImage(node);
        var element = node.VisualElement<Image>();
        element.scaleMode = scaleMode.ToUnityScaleMode();
        element.UserData()["Compose.Net.ContentDescription"] = contentDescription;
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
