using System;
using Compose.Net;
using UnityEngine;
using UnityEngine.UIElements;

// ReSharper disable CheckNamespace

namespace Compose.Net.Unity;

internal sealed class PathDrawerImpl : IPathDrawer
{
    private readonly Painter2D _painter;

    public PathDrawerImpl(Painter2D painter)
    {
        _painter = painter;
        _painter.BeginPath();
    }

    public void MoveTo(Offset point)
    {
        _painter.MoveTo(point.ToVector2());
    }

    public void LineTo(Offset point)
    {
        _painter.LineTo(point.ToVector2());
    }

    public void QuadraticTo(Offset control, Offset end)
    {
        _painter.QuadraticCurveTo(
            control.ToVector2(),
            end.ToVector2()
        );
    }

    public void CubicTo(
        Offset control1,
        Offset control2,
        Offset end)
    {
        _painter.BezierCurveTo(
            control1.ToVector2(),
            control2.ToVector2(),
            end.ToVector2()
        );
    }

    public void Close()
    {
        _painter.ClosePath();
    }

    public void AddRect(
        Offset topLeft,
        Size size
    )
    {
        var x = topLeft.X;
        var y = topLeft.Y;
        var width = size.Width;
        var height = size.Height;

        _painter.MoveTo(new Vector2(x, y));
        _painter.LineTo(new Vector2(x + width, y));
        _painter.LineTo(new Vector2(x + width, y + height));
        _painter.LineTo(new Vector2(x, y + height));
        _painter.ClosePath();
    }

    public void AddRoundRect(
        Offset topLeft,
        Size size,
        float radiusX,
        float radiusY
    )
    {
        var x = topLeft.X;
        var y = topLeft.Y;
        var right = x + size.Width;
        var bottom = y + size.Height;

        radiusX = MathF.Min(radiusX, size.Width / 2f);
        radiusY = MathF.Min(radiusY, size.Height / 2f);

        _painter.MoveTo(new Vector2(x + radiusX, y));

        _painter.LineTo(new Vector2(right - radiusX, y));
        _painter.ArcTo(
            new Vector2(right, y),
            new Vector2(right, y + radiusY),
            radiusX
        );

        _painter.LineTo(new Vector2(right, bottom - radiusY));
        _painter.ArcTo(
            new Vector2(right, bottom),
            new Vector2(right - radiusX, bottom),
            radiusX
        );

        _painter.LineTo(new Vector2(x + radiusX, bottom));
        _painter.ArcTo(
            new Vector2(x, bottom),
            new Vector2(x, bottom - radiusY),
            radiusX
        );

        _painter.LineTo(new Vector2(x, y + radiusY));
        _painter.ArcTo(
            new Vector2(x, y),
            new Vector2(x + radiusX, y),
            radiusX
        );

        _painter.ClosePath();
    }

    public void AddOval(
        Offset topLeft,
        Size size
    )
    {
        // Painter2D.Arc() умеет только окружность,
        // поэтому эллипс строим через cubic Bézier.
        const float kappa = 0.5522847498f;

        var cx = topLeft.X + size.Width / 2f;
        var cy = topLeft.Y + size.Height / 2f;
        var rx = size.Width / 2f;
        var ry = size.Height / 2f;

        var kx = rx * kappa;
        var ky = ry * kappa;

        _painter.MoveTo(new Vector2(cx + rx, cy));

        _painter.BezierCurveTo(
            new Vector2(cx + rx, cy + ky),
            new Vector2(cx + kx, cy + ry),
            new Vector2(cx, cy + ry)
        );

        _painter.BezierCurveTo(
            new Vector2(cx - kx, cy + ry),
            new Vector2(cx - rx, cy + ky),
            new Vector2(cx - rx, cy)
        );

        _painter.BezierCurveTo(
            new Vector2(cx - rx, cy - ky),
            new Vector2(cx - kx, cy - ry),
            new Vector2(cx, cy - ry)
        );

        _painter.BezierCurveTo(
            new Vector2(cx + kx, cy - ry),
            new Vector2(cx + rx, cy - ky),
            new Vector2(cx + rx, cy)
        );

        _painter.ClosePath();
    }

    public void AddArc(
        Offset topLeft,
        Size size,
        float startAngle,
        float sweepAngle
    )
    {
        throw new NotSupportedException(
            "Elliptical arcs are not implemented by Painter2DPathAdapter."
        );
    }
}