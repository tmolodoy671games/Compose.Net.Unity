using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Runtime.Adapters.Extensions;

internal static class StyleExtensions
{
    public static void AddFilter(this IStyle style, FilterFunction filter)
    {
        if (style.filter.value == null)
            style.filter = new List<FilterFunction>();
        var newFilters = new List<FilterFunction>(style.filter.value) { filter };
        style.filter = newFilters;
    }

    public static void RemoveFilter(this IStyle style, FilterFunction filter)
    {
        if (style.filter.value == null)
            style.filter = new List<FilterFunction>();
        var newFilters = new List<FilterFunction>(style.filter.value);
        newFilters.Remove(filter);
        style.filter = newFilters;
    }
    
    public static void AddFilters(this IStyle style, params FilterFunction[] filter)
    {
        if (style.filter.value == null)
            style.filter = new List<FilterFunction>();
        var newFilters = new List<FilterFunction>(style.filter.value);
        newFilters.AddRange(filter);
        style.filter = newFilters;
    }

    public static void RemoveFilters(this IStyle style, params FilterFunction[] filter)
    {
        if (style.filter.value == null)
            style.filter = new List<FilterFunction>();
        var newFilters = new List<FilterFunction>(style.filter.value);
        foreach (var f in filter)
            newFilters.Remove(f);
        style.filter = newFilters;
    }
    
    public static void AddBackdropFilter(this IStyle style, FilterFunction filter)
    {
        if (style.backdropFilter.value == null)
            style.backdropFilter = new List<FilterFunction>();
        var newFilters = new List<FilterFunction>(style.backdropFilter.value) { filter };
        style.backdropFilter = newFilters;
    }

    public static void RemoveBackdropFilter(this IStyle style, FilterFunction filter)
    {
        if (style.backdropFilter.value == null)
            style.backdropFilter = new List<FilterFunction>();
        var newFilters = new List<FilterFunction>(style.backdropFilter.value);
        newFilters.Remove(filter);
        style.backdropFilter = newFilters;
    }
}