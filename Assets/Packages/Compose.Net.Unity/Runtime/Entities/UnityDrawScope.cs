using System;
using Compose.Net;
using SharpExtensions;
using StableCollections;
using UnityEngine;
using UnityEngine.UIElements;
using Rect = Compose.Net.Rect;

// ReSharper disable CheckNamespace

namespace Compose.Net.Unity;

internal class UnityDrawScope : IDrawScope<MeshGenerationContext>, IDrawMeshScope
{
    private MeshGenerationContext _context;
    private readonly GradientDrawCache _painter;
    internal GradientDrawCache Painter => _painter;
    internal void BeginFrame() => _painter.BeginFrame();
    internal void EndFrame() => _painter.EndFrame();
    private readonly PathDrawerImpl _drawer;
    private readonly Action<Texture2D> _consumeTexture2D;
    private Size _size;

    public UnityDrawScope(MeshGenerationContext context)
    {
        _context = context;
        _painter = new GradientDrawCache(context);
        _drawer = new PathDrawerImpl(_painter);
        context.visualElement.RegisterCallback<DetachFromPanelEvent>(_ => _painter.Dispose());
        _consumeTexture2D = it => _painter.fillTexture = it;
    }

    public void DrawMesh(ReadOnlySpan<Offset> positions, ReadOnlySpan<Color> colors,
        ReadOnlySpan<ushort> indices, Rect bounds, float alpha)
    {
        var mesh = _context.Allocate(positions.Length, indices.Length);
        for (var i = 0; i < positions.Length; i++)
        {
            var color = colors[i].ToUnityColor();
            color.a *= alpha;
            mesh.SetNextVertex(new Vertex
            {
                position = new Vector3(bounds.Offset.X + positions[i].X * bounds.Size.Width,
                    bounds.Offset.Y + positions[i].Y * bounds.Size.Height, Vertex.nearZ),
                tint = color
            });
        }
        for (var i = 0; i < indices.Length; i++) mesh.SetNextIndex(indices[i]);
    }

    public MeshGenerationContext Context => _context;

    public void Update(MeshGenerationContext context)
    {
        _context = context;
        _painter.Update(context);
    }

    public Offset Center
    {
        get
        {
            try
            {
                return _context.visualElement.layout.center.ToOffset();
            }
            catch (NullReferenceException)
            {
                return Offset.Zero;
            }
        }
    }

    public Size Size
    {
        get
        {
            try
            {
                return _context.visualElement.layout.size.ToSize();
            }
            catch (NullReferenceException)
            {
                return new Size();
            }
        }
    }

    #region DrawLine

    public void DrawLine(
        Color color,
        Offset start,
        Offset end,
        float strokeWidth,
        StrokeCap strokeCap,
        float alpha
    )
    {
        var painter = _painter;
        var c = color.ToUnityColor();
        c.a *= alpha;
        painter.strokeColor = c;
        DrawLineImpl(
            start: start,
            end: end,
            strokeWidth: strokeWidth,
            strokeCap: strokeCap
        );
    }

    public void DrawLine(
        IBrush brush,
        Offset start,
        Offset end,
        float strokeWidth = 1,
        StrokeCap strokeCap = StrokeCap.Butt,
        float alpha = 1
    )
    {
        var min = new Offset(
            X: Math.Min(start.X, end.X),
            Y: Math.Min(start.Y, end.Y)
        );
        var max = new Offset(
            X: Math.Max(start.X, end.X),
            Y: Math.Max(start.Y, end.Y)
        );
        var contentRect = new Rect(
            Offset: min,
            Size: (max - min).ToSize()
        );
        _painter.UseNative();
        brush.Apply(this, contentRect, alpha, Stroke(strokeWidth));
        DrawLineImpl(
            start: start,
            end: end,
            strokeWidth: strokeWidth,
            strokeCap: strokeCap
        );
    }

    private void DrawLineImpl(
        Offset start,
        Offset end,
        float strokeWidth,
        StrokeCap strokeCap
    )
    {
        var painter = _painter;

        painter.lineWidth = strokeWidth;
        painter.lineCap = strokeCap.ToUnityLineCap();
        painter.BeginPath();
        painter.MoveTo(start.ToVector2());
        painter.LineTo(end.ToVector2());
        painter.Stroke();
    }

    #endregion

    #region DrawRect

    public void DrawRect(
        Color color,
        Offset topLeft,
        Size size,
        float alpha,
        DrawStyle? style
    )
    {
        ApplyColor(color, alpha, style ?? Fill);
        DrawRectImpl(
            topLeft: topLeft,
            size: size,
            style: style
        );
    }

    public void DrawRect(
        IBrush brush,
        Offset topLeft = new(),
        Size size = default,
        float alpha = 1,
        DrawStyle? style = null
    )
    {
        var contentRect = new Rect(
            Offset: topLeft,
            Size: size.GetOrDefault(Size)
        );
        _painter.UseNative();
        brush.Apply(this, contentRect, alpha, style ?? Fill);
        DrawRectImpl(
            topLeft: topLeft,
            size: size,
            style: style
        );
    }

    private void DrawRectImpl(
        Offset topLeft = new(),
        Size size = default,
        DrawStyle? style = null
    )
    {
        var painter = _painter;
        var resolvedSize = size.GetOrDefault(Size);
        var rect = new UnityEngine.Rect(
            topLeft.X,
            topLeft.Y,
            resolvedSize.Width,
            resolvedSize.Height
        );

        painter.BeginPath();
        painter.MoveTo(rect.min);
        painter.LineTo(new Vector2(rect.xMax, rect.yMin));
        painter.LineTo(rect.max);
        painter.LineTo(new Vector2(rect.xMin, rect.yMax));
        painter.ClosePath();

        Draw(style ?? Fill);
    }

    #endregion

    #region DrawRoundRect

    public void DrawRoundRect(
        Color color,
        Offset topLeft,
        Size size,
        float cornerRadius,
        float alpha,
        DrawStyle? style
    )
    {
        ApplyColor(color, alpha, style ?? Fill);
        DrawRoundRectImpl(
            topLeft: topLeft,
            size: size,
            shape: RoundedCornerShape(cornerRadius.Dp()),
            style: style
        );
    }

    public void DrawRoundRect(
        IBrush brush,
        Offset topLeft,
        Size size,
        float cornerRadius,
        float alpha,
        DrawStyle? style
    )
    {
        var contentRect = new Rect(
            Offset: topLeft,
            Size: size.GetOrDefault(Size)
        );
        _painter.UseNative();
        brush.Apply(this, contentRect, alpha, style ?? Fill);
        DrawRoundRectImpl(
            topLeft: topLeft,
            size: size,
            shape: RoundedCornerShape(cornerRadius.Dp()),
            style: style
        );
    }

    public void DrawShape(
        Color color,
        Offset topLeft,
        Size size,
        Shape shape,
        float alpha = 1,
        DrawStyle? style = null
    )
    {
        ApplyColor(color, alpha, style ?? Fill);
        DrawRoundRectImpl(
            topLeft: topLeft,
            size: size,
            shape: shape.GetOrDefault(RectangleShape),
            style: style
        );
    }

    public void DrawShape(
        IBrush brush,
        Offset topLeft,
        Size size,
        Shape shape,
        float alpha = 1,
        DrawStyle? style = null
    )
    {
        var contentRect = new Rect(
            Offset: topLeft,
            Size: size.GetOrDefault(Size)
        );
        _painter.UseNative();
        brush.Apply(this, contentRect, alpha, style ?? Fill);
        DrawRoundRectImpl(
            topLeft: topLeft,
            size: size,
            shape: shape.GetOrDefault(RectangleShape),
            style: style
        );
    }

    private void DrawRoundRectImpl(
        Offset topLeft,
        Size size,
        Shape shape,
        DrawStyle? style
    )
    {
        var painter = _painter;

        var resolvedSize = size.GetOrDefault(Size);

        var x = topLeft.X;
        var y = topLeft.Y;
        var width = resolvedSize.Width;
        var height = resolvedSize.Height;

        painter.BeginPath();

        // Top-left
        var topLeftRadius = Mathf.Clamp(
            shape.TopLeft.ToPx(UnityDensity.Current),
            0f,
            Mathf.Min(width, height) / 2f
        );
        painter.MoveTo(new Vector2(x + topLeftRadius, y));

        // Top-right
        var topRightRadius = Mathf.Clamp(
            shape.TopRight.ToPx(UnityDensity.Current),
            0f,
            Mathf.Min(width, height) / 2f
        );
        painter.LineTo(new Vector2(x + width - topRightRadius, y));
        painter.ArcTo(
            new Vector2(x + width, y),
            new Vector2(x + width, y + topRightRadius),
            topRightRadius
        );

        // Bottom-right
        var bottomRightRadius = Mathf.Clamp(
            shape.BottomRight.ToPx(UnityDensity.Current),
            0f,
            Mathf.Min(width, height) / 2f
        );
        painter.LineTo(new Vector2(x + width, y + height - bottomRightRadius));
        painter.ArcTo(
            new Vector2(x + width, y + height),
            new Vector2(x + width - bottomRightRadius, y + height),
            bottomRightRadius
        );

        // Bottom-left
        var bottomLeftRadius = Mathf.Clamp(
            shape.BottomLeft.ToPx(UnityDensity.Current),
            0f,
            Mathf.Min(width, height) / 2f
        );
        painter.LineTo(new Vector2(x + bottomLeftRadius, y + height));
        painter.ArcTo(
            new Vector2(x, y + height),
            new Vector2(x, y + height - bottomLeftRadius),
            bottomLeftRadius
        );

        // Top-left
        painter.LineTo(new Vector2(x, y + topLeftRadius));
        painter.ArcTo(
            new Vector2(x, y),
            new Vector2(x + topLeftRadius, y),
            topLeftRadius
        );

        painter.ClosePath();
        Draw(style ?? Fill);
    }

    #endregion

    #region DrawCircle

    public void DrawCircle(
        Color color,
        float? radius,
        Offset center,
        float alpha,
        DrawStyle? style
    )
    {
        ApplyColor(color, alpha, style);
        DrawCircleImpl(radius, center, style);
    }

    public void DrawCircle(
        IBrush brush,
        float? radius,
        Offset center,
        float alpha,
        DrawStyle? style
    )
    {
        var resolvedRadius = radius ?? Size.MinDimension / 2;
        var resolvedCenter = center.IsSpecified ? center : Center;
        var contentRect = new Rect(
            Offset: resolvedCenter - new Offset(resolvedRadius, resolvedRadius),
            Size: new Size(resolvedRadius, resolvedRadius)
        );
        _painter.UseNative();
        brush.Apply(this, contentRect, alpha, style ?? Fill);
        DrawCircleImpl(radius, center, style);
    }

    private void DrawCircleImpl(
        float? radius,
        Offset center,
        DrawStyle? style
    )
    {
        var painter = _painter;
        painter.BeginPath();
        painter.Arc(
            (center.IsSpecified ? center : Center).ToVector2(),
            radius ?? Size.MinDimension / 2,
            0f,
            360f
        );
        painter.ClosePath();

        Draw(style ?? Fill);
    }

    #endregion

    #region DrawOval

    public void DrawOval(
        Color color,
        Offset topLeft,
        Size size,
        float alpha,
        DrawStyle? style
    )
    {
        ApplyColor(color, alpha, style);
        DrawOvalImpl(
            topLeft: topLeft,
            size: size,
            style: style
        );
    }

    public void DrawOval(
        IBrush brush,
        Offset topLeft,
        Size size,
        float alpha,
        DrawStyle? style
    )
    {
        var contentRect = new Rect(
            Offset: topLeft,
            Size: size.GetOrDefault(Size)
        );
        _painter.UseNative();
        brush.Apply(this, contentRect, alpha, style ?? Fill);
        DrawOvalImpl(
            topLeft: topLeft,
            size: size,
            style: style
        );
    }

    private void DrawOvalImpl(
        Offset topLeft,
        Size size,
        DrawStyle? style
    )
    {
        var painter = _painter;
        var resolvedSize = size.GetOrDefault(
            new Size(
                Size.Width - topLeft.X,
                Size.Height - topLeft.Y
            )
        );

        var rect = new UnityEngine.Rect(
            topLeft.X,
            topLeft.Y,
            resolvedSize.Width,
            resolvedSize.Height
        );

        painter.BeginPath();

        painter.Arc(
            rect.center,
            Mathf.Min(rect.width, rect.height) / 2f,
            0f,
            360f
        );

        painter.ClosePath();

        Draw(style);
    }

    #endregion

    #region DrawArc

    public void DrawArc(
        Color color,
        float startAngle,
        float sweepAngle,
        bool useCenter,
        Offset topLeft,
        Size size,
        float alpha,
        DrawStyle? style
    )
    {
        ApplyColor(color, alpha, style);
        DrawArcImpl(
            startAngle: startAngle,
            sweepAngle: sweepAngle,
            useCenter: useCenter,
            topLeft: topLeft,
            size: size,
            style: style
        );
    }

    public void DrawArc(
        IBrush brush,
        float startAngle,
        float sweepAngle,
        bool useCenter,
        Offset topLeft,
        Size size,
        float alpha,
        DrawStyle? style
    )
    {
        var contentRect = new Rect(
            Offset: topLeft,
            Size: size.GetOrDefault(Size)
        );
        _painter.UseNative();
        brush.Apply(this, contentRect, alpha, style ?? Fill);
        DrawArcImpl(
            startAngle: startAngle,
            sweepAngle: sweepAngle,
            useCenter: useCenter,
            topLeft: topLeft,
            size: size,
            style: style
        );
    }

    private void DrawArcImpl(
        float startAngle,
        float sweepAngle,
        bool useCenter,
        Offset topLeft,
        Size size,
        DrawStyle? style
    )
    {
        var painter = _painter;

        var resolvedSize = size.GetOrDefault(
            new Size(
                Size.Width - topLeft.X,
                Size.Height - topLeft.Y
            )
        );

        var rect = new UnityEngine.Rect(
            topLeft.X,
            topLeft.Y,
            resolvedSize.Width,
            resolvedSize.Height
        );

        var center = rect.center;
        var radius = Mathf.Min(rect.width, rect.height) / 2f;

        painter.BeginPath();

        if (useCenter)
            painter.MoveTo(center);

        painter.Arc(
            center,
            radius,
            startAngle,
            startAngle + sweepAngle
        );

        if (useCenter)
            painter.ClosePath();

        Draw(style);
    }

    #endregion

    #region DrawPath

    public void DrawPath(IPath path, Color color, float alpha, DrawStyle? style)
    {
        ApplyColor(color, alpha, style);
        DrawPathImpl(path, style);
    }

    public void DrawPath(
        IPath path,
        IBrush brush,
        float alpha = 1,
        DrawStyle? style = null
    )
    {
        _painter.UseNative();
        brush.Apply(this, new Rect(), alpha, style ?? Fill);
        DrawPathImpl(path, style);
    }

    private void DrawPathImpl(
        IPath path,
        DrawStyle? style
    )
    {
        // Scopes survive repaint and shared-transition captures. Each path must
        // start fresh on the current context, not the painter from construction.
        _drawer.Begin(_painter);
        path.Apply(_drawer);
        Draw(style);
    }

    #endregion

    #region DrawPoints

    public void DrawPoints(
        IStableList<Offset> points,
        PointMode pointMode,
        Color color,
        float strokeWidth,
        StrokeCap strokeCap,
        float alpha
    )
    {
        var style = pointMode switch
        {
            PointMode.Individual => Stroke(strokeWidth),
            PointMode.Lines => Stroke(strokeWidth),
            PointMode.Polygon => Fill,
            _ => throw new ArgumentOutOfRangeException(nameof(pointMode), pointMode, null)
        };
        ApplyColor(color, alpha, style);
        DrawPointsImpl(points, pointMode, strokeWidth, strokeCap);
    }

    public void DrawPoints(
        IStableList<Offset> points,
        PointMode pointMode,
        IBrush brush,
        float strokeWidth,
        StrokeCap strokeCap,
        float alpha
    )
    {
        var style = pointMode switch
        {
            PointMode.Individual => Stroke(strokeWidth),
            PointMode.Lines => Stroke(strokeWidth),
            PointMode.Polygon => Fill,
            _ => throw new ArgumentOutOfRangeException(nameof(pointMode), pointMode, null)
        };
        _painter.UseNative();
        brush.Apply(this, new Rect(), alpha, style);
        DrawPointsImpl(points, pointMode, strokeWidth, strokeCap);
    }

    private void DrawPointsImpl(
        IStableList<Offset> points,
        PointMode pointMode,
        float strokeWidth,
        StrokeCap strokeCap
    )
    {
        if (points.Size == 0)
            return;

        var painter = _painter;
        painter.lineWidth = strokeWidth;
        painter.lineCap = strokeCap.ToUnityLineCap();

        switch (pointMode)
        {
            case PointMode.Individual:
                DrawPoints(painter, points);
                break;

            case PointMode.Lines:
                DrawPointLines(painter, points);
                break;

            case PointMode.Polygon:
                DrawPointPolygon(painter, points);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(pointMode), pointMode, null);
        }
    }

    private static void DrawPoints(
        GradientDrawCache painter,
        IStableList<Offset> points
    )
    {
        var radius = painter.lineWidth / 2f;

        foreach (var point in points)
        {
            var p = point.ToVector2();

            painter.BeginPath();
            painter.Arc(p, radius, 0f, 360f);
            painter.ClosePath();
            painter.Fill();
        }
    }

    #endregion

    #region DrawPointLines

    private static void DrawPointLines(
        GradientDrawCache painter,
        IStableList<Offset> points
    )
    {
        for (var i = 0; i + 1 < points.Size; i += 2)
        {
            painter.BeginPath();
            painter.MoveTo(points[i].ToVector2());
            painter.LineTo(points[i + 1].ToVector2());
            painter.Stroke();
        }
    }

    private static void DrawPointPolygon(
        GradientDrawCache painter,
        IStableList<Offset> points
    )
    {
        if (points.Size < 2)
            return;

        painter.BeginPath();
        painter.MoveTo(points[0].ToVector2());

        for (var i = 1; i < points.Size; i++)
            painter.LineTo(points[i].ToVector2());

        painter.Stroke();
    }

    #endregion

    #region DrawImage

    public void DrawImage(
        IImageBitmap image,
        Offset topLeft = new(),
        float alpha = 1,
        Size size = default,
        DrawStyle? style = null
    )
    {
        var drawSize = size.GetOrDefault(image.Size);
        _painter.UseNative();
        image.SetImage(this);
        DrawRectImpl(
            topLeft: topLeft,
            size: drawSize,
            style: style
        );
    }

    #endregion

    private void Draw(DrawStyle? style)
    {
        var painter = _painter;
        if (painter == null)
            return;
        switch (style ?? Fill)
        {
            case DrawStyle.Fill:
                painter.Fill();
                break;
            case DrawStyle.Stroke stroke:
                painter.lineWidth = stroke.Width;
                painter.Stroke();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(style));
        }
    }

    private void ApplyColor(Color color, float alpha, DrawStyle? style)
    {
        color = color with { Alpha = color.Alpha * alpha };
        var painter = _painter;
        switch (style ?? Fill)
        {
            case DrawStyle.Fill:
                painter.fillColor = color.ToUnityColor();
                break;
            case DrawStyle.Stroke stroke:
                painter.lineWidth = stroke.Width;
                painter.strokeColor = color.ToUnityColor();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(style));
        }
    }
}

public static partial class DrawScopeExtensions
{
    public static MeshGenerationContext Context(this IDrawScope scope) => scope.CastTo<UnityDrawScope>().Context;
    internal static GradientDrawCache Painter(this IDrawScope scope) => scope.CastTo<UnityDrawScope>().Painter;
}

public static partial class VisualElementExtensions
{
    internal static IDrawScope DrawScope(this MeshGenerationContext context)
    {
        const string key = "Compose.Net.Unity_DrawScope";
        var visualElement = context.visualElement;
        if (visualElement.UserData().GetOrDefault(key, null) is { } cached)
        {
            var cachedResult = (UnityDrawScope)cached.NotNull();
            cachedResult.Update(context);
            return cachedResult;
        }

        var scope = new UnityDrawScope(context);
        visualElement.UserData()[key] = scope;
        return scope;
    }
}

public static class StrokeCapExtensions
{
    public static LineCap ToUnityLineCap(this StrokeCap cap) =>
        cap switch
        {
            StrokeCap.Butt => LineCap.Butt,
            StrokeCap.Round => LineCap.Round,
            _ => LineCap.Butt
        };
}