using System;
using Compose.Net;
using SharpExtensions;
using StableCollections;
using UnityEngine;
using UnityEngine.UIElements;
using Rect = Compose.Net.Rect;

// ReSharper disable CheckNamespace

namespace Compose.Net.Unity;

internal class UnityDrawScope : IDrawScope<MeshGenerationContext>
{
    private MeshGenerationContext _context;
    private readonly IPathDrawer _drawer;
    private readonly Action<Texture2D> _consumeTexture2D;
    private Size _size;

    public UnityDrawScope(MeshGenerationContext context)
    {
        _context = context;
        _drawer = new PathDrawerImpl(_context.painter2D);
        _consumeTexture2D = it => _context.painter2D.fillTexture = it;
    }

    public MeshGenerationContext Context => _context;

    public void Update(MeshGenerationContext context)
    {
        _context = context;
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
        var painter = _context.painter2D;
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
        var painter = _context.painter2D;

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
        var painter = _context.painter2D;
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
        var painter = _context.painter2D;

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
            shape.TopRight.ToPx(UnityDensity.Current),
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
            shape.TopRight.ToPx(UnityDensity.Current),
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
        Optional<float> radius,
        Optional<Offset> center,
        float alpha,
        DrawStyle? style
    )
    {
        ApplyColor(color, alpha, style);
        DrawCircleImpl(radius, center, style);
    }

    public void DrawCircle(
        IBrush brush,
        Optional<float> radius,
        Optional<Offset> center,
        float alpha,
        DrawStyle? style
    )
    {
        var resolvedRadius = radius.GetOrDefault(Size.MinDimension / 2);
        var resolvedCenter = center.GetOrDefault(Center);
        var contentRect = new Rect(
            Offset: resolvedCenter - new Offset(resolvedRadius, resolvedRadius),
            Size: new Size(resolvedRadius, resolvedRadius)
        );
        brush.Apply(this, contentRect, alpha, style ?? Fill);
        DrawCircleImpl(radius, center, style);
    }

    private void DrawCircleImpl(
        Optional<float> radius,
        Optional<Offset> center,
        DrawStyle? style
    )
    {
        var painter = _context.painter2D;
        painter.BeginPath();
        painter.Arc(
            center.GetOrDefault(Center).ToVector2(),
            radius.GetOrDefault(Size.MinDimension / 2),
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
        var painter = _context.painter2D;
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
        var painter = _context.painter2D;

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
        brush.Apply(this, new Rect(), alpha, style ?? Fill);
        DrawPathImpl(path, style);
    }

    private void DrawPathImpl(
        IPath path,
        DrawStyle? style
    )
    {
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

        var painter = _context.painter2D;
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
        Painter2D painter,
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
        Painter2D painter,
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
        Painter2D painter,
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
        var painter = _context.painter2D;
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
        color = color with { Alpha = alpha };
        var painter = _context.painter2D;
        switch (style ?? Fill)
        {
            case DrawStyle.Fill:
                painter.fillColor = color.ToUnityColor();
                break;
            case DrawStyle.Stroke stroke:
                painter.lineWidth = stroke.Width;
                painter.fillColor = color.ToUnityColor();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(style));
        }
    }
}

public static partial class DrawScopeExtensions
{
    public static MeshGenerationContext Context(this IDrawScope scope) => scope.CastTo<UnityDrawScope>().Context;
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