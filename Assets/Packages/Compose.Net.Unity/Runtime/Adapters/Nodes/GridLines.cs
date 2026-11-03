using System;
using System.Collections.Generic;
using System.Linq;
using Compose.Net;

namespace Compose.Net.Unity.Runtime.Adapters.Nodes;

// Only span metadata is indexed. Uniform grids are O(1) in item count, including million-item grids.
// Custom spans require a compact line-start index; content is still composed only for native slots.
internal sealed class GridLines
{
    internal readonly record struct Cell(int Index, int Slot, int Span);
    private readonly ILazyGridItemProvider _items;
    private readonly int _tracks;
    private readonly List<int>? _starts;
    internal int Count => _starts?.Count ?? (_items.Count / _tracks + (_items.Count % _tracks == 0 ? 0 : 1));
    internal GridLines(ILazyGridItemProvider items, int tracks)
    {
        _items = items; _tracks = tracks;
        if (!items.HasCustomSpans) return;
        _starts = new();
        for (var index = 0; index < items.Count;)
        {
            _starts.Add(index); var occupied = 0;
            while (index < items.Count && occupied < tracks)
            {
                var span = items.GetSpan(index, tracks - occupied, tracks);
                if (span > tracks - occupied) break;
                occupied += span; index++;
            }
        }
    }
    internal int LineOf(int index)
    {
        if (_starts == null) return index / _tracks;
        var found = _starts.BinarySearch(index); return found >= 0 ? found : ~found - 1;
    }
    internal Cell[] Cells(int line)
    {
        var start = _starts == null ? line * _tracks : _starts[line];
        var end = _starts == null ? Math.Min(_items.Count, start + _tracks) : line + 1 < Count ? _starts[line + 1] : _items.Count;
        var result = new Cell[end - start]; var slot = 0;
        for (var i = start; i < end; i++)
        {
            var span = _items.GetSpan(i, _tracks - slot, _tracks);
            result[i - start] = new(i, slot, span); slot += span;
        }
        return result;
    }
    internal bool SameStructure(GridLines other) => _tracks == other._tracks && _items.Count == other._items.Count &&
        (_starts == null ? other._starts == null : other._starts != null && _starts.SequenceEqual(other._starts));
}
