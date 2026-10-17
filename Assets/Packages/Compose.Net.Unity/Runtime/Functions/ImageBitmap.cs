// ReSharper disable CheckNamespace

using System;
using Compose.Net;
using SharpExtensions;
using UnityEngine;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

public static class ImageBitmap
{
    public static IImageBitmap ImageResource(Texture texture)
    {
        texture.NotNull();
        return new TextureImageBitmapImpl(texture);
    }

    public static IImageBitmap ImageResource(Texture2D texture)
    {
        texture.NotNull();
        return new Texture2DImageBitmapImpl(texture);
    }

    public static IImageBitmap ImageResource(Sprite sprite)
    {
        sprite.NotNull();
        return new SpriteImageBitmapImpl(sprite);
    }

    public static IImageBitmap ImageResource(VectorImage vectorImage)
    {
        vectorImage.NotNull();
        return new VectorImageBitmapImpl(vectorImage);
    }
}

internal record TextureImageBitmapImpl(
    Texture Texture
) : IImageBitmap
{
    public Size Size => new(Texture.width, Texture.height);

    public void SetImage(IReusableComposeNode imageNode)
    {
        imageNode.VisualElement<Image>().image = Texture;
    }

    public void SetImage(IDrawScope drawScope)
    {
        if (Texture is Texture2D texture2D)
            drawScope.Context().painter2D.fillTexture = texture2D;
    }
}

internal record Texture2DImageBitmapImpl(
    Texture2D Texture
) : IImageBitmap
{
    public Size Size => new(Texture.width, Texture.height);

    public void SetImage(IReusableComposeNode imageNode) => imageNode.VisualElement<Image>().image = Texture;
    public void SetImage(IDrawScope drawScope) => drawScope.Context().painter2D.fillTexture = Texture;
}

internal record SpriteImageBitmapImpl(
    Sprite Sprite
) : IImageBitmap
{
    public Size Size => new(Sprite.rect.width, Sprite.rect.height);

    public void SetImage(IReusableComposeNode imageNode) => imageNode.VisualElement<Image>().sprite = Sprite;
    public void SetImage(IDrawScope drawScope) => drawScope.Context().painter2D.fillTexture = Sprite.texture;
}

internal record VectorImageBitmapImpl(
    VectorImage VectorImage
) : IImageBitmap
{
    public Size Size => new(VectorImage.width, VectorImage.height);

    public void SetImage(IReusableComposeNode imageNode) => imageNode.VisualElement<Image>().vectorImage = VectorImage;

    public void SetImage(IDrawScope drawScope)
    {
    }
}