using System;
using Compose.Net;
using SharpExtensions;

namespace UnityCompose.Packages.UnityCompose.Runtime.Adapters.Modifiers;

internal class AppearanceModifiersFactoryImpl : IAppearanceModifiersFactory
{
    public IModifier Alpha(float alpha) => new AlphaModifierImpl(alpha);
    public IModifier ZIndex(float zIndex) => new ZIndexModifierImpl(zIndex);
    public IModifier Blur(Dp radius) => new BlurModifierImpl(radius);
    public IModifier BackdropBlur(Dp radius) => new BackdropBlurModifierImpl(radius);

    public IModifier Clip(Optional<Shape> shape)
    {
        return new ClipModifierImpl(shape);
    }

    public IModifier Background(Color color, Optional<Shape> shape)
    {
        return shape.HasValue
            ? new BackgroundColorShapeModifierImpl(color, shape.Value)
            : new BackgroundColorModifierImpl(color);
    }

    public IModifier Background(IBrush brush, Optional<Shape> shape)
    {
        return new BackgroundBrushModifierImpl(brush, shape);
    }

    public IModifier Paint(IPainter painter, float alpha)
    {
        return new PaintModifierImpl(painter, alpha);
    }

    public IModifier DropShadow(Shape shape, Compose.Net.Shadow shadow)
    {
        return new DropShadowModifierImpl(shape, shadow);
    }

    public IModifier InnerShadow(Shape shape, Compose.Net.Shadow shadow)
    {
        return new InnerShadowModifierImpl(shape, shadow);
    }

    public IModifier DrawBehind(Action<IDrawScope> onDraw)
    {
        return new DrawBehindModifierImpl(onDraw);
    }

    public IModifier DrawBehind<T>(Action<IDrawScope<T>> onDraw)
    {
        return DrawBehind(it => onDraw((IDrawScope<T>)it));
    }

    public IModifier Draw(Action<IDrawScope> onDraw)
    {
        return new DrawModifierImpl(onDraw);
    }

    public IModifier Draw<T>(Action<IDrawScope<T>> onDraw)
    {
        return Draw(it => onDraw((IDrawScope<T>)it));
    }

    public IModifier DrawOnTop(Action<IDrawScope> onDraw)
    {
        return new DrawOnTopModifierImpl(onDraw);
    }

    public IModifier DrawOnTop<T>(Action<IDrawScope<T>> onDraw)
    {
        return DrawOnTop(it => onDraw((IDrawScope<T>)it));
    }

    public IModifier Border(Dp width, IBrush brush, Shape shape)
    {
        return new BorderModifierImpl(width, brush, shape);
    }
}