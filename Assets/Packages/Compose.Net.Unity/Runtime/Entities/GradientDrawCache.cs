using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Compose.Net.Unity;

// Unity 7000.0.0a12's attached Painter2D retains a new LUT texture and
// VectorImage for every gradient Fill/Stroke until the panel is disposed.
// Bake gradient paths with an owned, detached painter instead. Each draw slot
// keeps one reusable vector image, released when replaced or detached.
internal sealed class GradientDrawCache : IDisposable
{
    private MeshGenerationContext _context;
    private readonly List<Command> _path = new();
    private readonly List<Entry> _entries = new();
    private readonly List<VectorImage> _retired = new();
    private GradientDefinition? _gradient;
    private int _slot;
    private float _lineWidth = 1;
    private LineCap _lineCap;
    private Vector2 _min, _max;

    internal GradientDrawCache(MeshGenerationContext context) => _context = context;
    internal void Update(MeshGenerationContext context) => _context = context;
    internal void UseNative() => _gradient = null;

    internal void BeginFrame()
    {
        foreach (var image in _retired) Destroy(image);
        _retired.Clear();
        _slot = 0;
        _gradient = null;
    }

    internal void EndFrame()
    {
        for (var i = _entries.Count - 1; i >= _slot; i--)
        {
            _retired.Add(_entries[i].Image);
            _entries.RemoveAt(i);
        }
        _path.Clear();
        _gradient = null;
    }

    internal float lineWidth
    {
        get => _lineWidth;
        set { _lineWidth = value; _context.painter2D.lineWidth = value; }
    }
    internal LineCap lineCap
    {
        get => _lineCap;
        set { _lineCap = value; _context.painter2D.lineCap = value; }
    }
    internal UnityEngine.Color fillColor
    {
        set { _gradient = null; _context.painter2D.fillColor = value; }
    }
    internal UnityEngine.Color strokeColor
    {
        set { _gradient = null; _context.painter2D.strokeColor = value; }
    }
    internal Texture2D fillTexture
    {
        set { _gradient = null; _context.painter2D.fillTexture = value; }
    }
    internal GradientDefinition fillGradient { set => _gradient = value; }
    internal GradientDefinition strokeFillGradient { set => _gradient = value; }

    internal void BeginPath()
    {
        if (!_gradient.HasValue) { _context.painter2D.BeginPath(); return; }
        _path.Clear();
        _min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        _max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
    }
    internal void MoveTo(Vector2 p) => Add(new Command(Kind.Move, p));
    internal void LineTo(Vector2 p) => Add(new Command(Kind.Line, p));
    internal void ClosePath() => Add(new Command(Kind.Close));
    internal void ArcTo(Vector2 p, Vector2 q, float radius) => Add(new Command(Kind.ArcTo, p, q, default, radius));
    internal void Arc(Vector2 center, float radius, float from, float to) => Add(new Command(Kind.Arc, center, new Vector2(from, to), default, radius));
    internal void BezierCurveTo(Vector2 p, Vector2 q, Vector2 end) => Add(new Command(Kind.Cubic, p, q, end));
    internal void QuadraticCurveTo(Vector2 p, Vector2 end) => Add(new Command(Kind.Quadratic, p, end));

    private void Add(Command command)
    {
        if (!_gradient.HasValue) { command.Apply(_context.painter2D); return; }
        _path.Add(command);
        if (command.Type == Kind.Close) return;
        Include(command.A);
        if (command.Type is Kind.Cubic or Kind.Quadratic or Kind.ArcTo) Include(command.B);
        if (command.Type == Kind.Cubic) Include(command.C);
        if (command.Type is Kind.Arc or Kind.ArcTo)
        {
            var radius = Vector2.one * Math.Abs(command.Radius);
            Include(command.A - radius); Include(command.A + radius);
            if (command.Type == Kind.ArcTo) { Include(command.B - radius); Include(command.B + radius); }
        }
    }
    private void Include(Vector2 p) { _min = Vector2.Min(_min, p); _max = Vector2.Max(_max, p); }

    internal void Fill()
    {
        if (!_gradient.HasValue) _context.painter2D.Fill();
        else Draw(false);
    }
    internal void Stroke()
    {
        if (!_gradient.HasValue) _context.painter2D.Stroke();
        else Draw(true);
    }

    private void Draw(bool stroke)
    {
        if (_path.Count == 0 || !float.IsFinite(_min.x) || !float.IsFinite(_min.y) ||
            !float.IsFinite(_max.x) || !float.IsFinite(_max.y)) return;
        var gradient = _gradient!.Value;
        Entry entry;
        if (_slot < _entries.Count && _entries[_slot].Matches(_path, gradient, stroke, _lineWidth, _lineCap))
            entry = _entries[_slot];
        else
        {
            entry = Bake(gradient, stroke);
            if (_slot < _entries.Count)
            {
                // Keep the previous image alive until its replacement has rendered.
                _retired.Add(_entries[_slot].Image);
                _entries[_slot] = entry;
            }
            else _entries.Add(entry);
        }
        _slot++;
        _context.DrawVectorImage(entry.Image, entry.Origin, 0, Vector2.one);
    }

    private Entry Bake(GradientDefinition gradient, bool stroke)
    {
        var fill = gradient.Create(out var colors, out var alphas);
        using var painter = new Painter2D();
        painter.lineWidth = _lineWidth;
        painter.lineCap = _lineCap;
        if (stroke) painter.strokeFillGradient = fill;
        else painter.fillGradient = fill;
        painter.BeginPath();
        foreach (var command in _path) command.Apply(painter);
        if (stroke) painter.Stroke(); else painter.Fill();

        // SaveToVectorImage normalizes geometry to its bounding-box origin.
        // Transparent bounds make that origin explicit using only public APIs.
        // Control points bound Bezier curves; the padding also bounds stroke joins.
        var padding = Vector2.one * (stroke ? Math.Abs(_lineWidth) * 10 + 2 : 2);
        var min = _min - padding;
        var max = _max + padding;
        painter.fillColor = UnityEngine.Color.clear;
        painter.BeginPath();
        painter.MoveTo(min); painter.LineTo(new Vector2(max.x, min.y));
        painter.LineTo(max); painter.LineTo(new Vector2(min.x, max.y));
        painter.ClosePath(); painter.Fill();

        var image = ScriptableObject.CreateInstance<VectorImage>();
        image.name = "Compose gradient path";
        image.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            if (!painter.SaveToVectorImage(image)) throw new InvalidOperationException("Could not bake the gradient path.");
            return new Entry(image, min, _path.ToArray(), gradient, colors, alphas, stroke, _lineWidth, _lineCap);
        }
        catch { Destroy(image); throw; }
    }

    public void Dispose()
    {
        foreach (var entry in _entries) Destroy(entry.Image);
        foreach (var image in _retired) Destroy(image);
        _entries.Clear(); _retired.Clear(); _path.Clear();
        _slot = 0; _gradient = null;
    }

    private static void Destroy(VectorImage image)
    {
        // VectorImage.OnDestroy releases its generated atlas as well.
        if (!image) return;
        if (Application.isPlaying) Object.Destroy(image); else Object.DestroyImmediate(image);
    }

    private enum Kind { Move, Line, Close, ArcTo, Arc, Cubic, Quadratic }
    private readonly record struct Command(Kind Type, Vector2 A = default, Vector2 B = default, Vector2 C = default, float Radius = 0)
    {
        internal void Apply(Painter2D p)
        {
            switch (Type)
            {
                case Kind.Move: p.MoveTo(A); break;
                case Kind.Line: p.LineTo(A); break;
                case Kind.Close: p.ClosePath(); break;
                case Kind.ArcTo: p.ArcTo(A, B, Radius); break;
                case Kind.Arc: p.Arc(A, Radius, B.x, B.y); break;
                case Kind.Cubic: p.BezierCurveTo(A, B, C); break;
                case Kind.Quadratic: p.QuadraticCurveTo(A, B); break;
            }
        }
    }

    private sealed record Entry(VectorImage Image, Vector2 Origin, Command[] Path, GradientDefinition Gradient,
        GradientColorKey[] Colors, GradientAlphaKey[] Alphas, bool Stroke, float Width, LineCap Cap)
    {
        internal bool Matches(List<Command> path, GradientDefinition gradient, bool stroke, float width, LineCap cap)
        {
            if (!Image || Stroke != stroke || (stroke && (Width != width || Cap != cap)) || Path.Length != path.Count ||
                !gradient.Matches(Gradient, Colors, Alphas)) return false;
            for (var i = 0; i < Path.Length; i++) if (Path[i] != path[i]) return false;
            return true;
        }
    }
}
