using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace NCoreUtils.Data;

internal static class EnumerableExtensions
{
    public static bool TryGetFirst<T>(this IEnumerable<T> source, Func<T, bool> predicate, [MaybeNullWhen(false)] out T match)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }
        if (predicate is null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }
        foreach (var item in source)
        {
            if (predicate(item))
            {
                match = item;
                return true;
            }
        }
        match = default;
        return false;
    }
}