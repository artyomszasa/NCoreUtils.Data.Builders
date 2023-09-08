using System;
using System.Buffers;

namespace NCoreUtils.Data;

internal static class StringExtensions
{
    public static string Uncapitalize(this string? source)
    {
        if (source is null || source.Length == 0)
        {
            return string.Empty;
        }
        if (char.IsLower(source[0]))
        {
            return source;
        }
        var buffer = ArrayPool<char>.Shared.Rent(source.Length);
        try
        {
            var bufferSpan = buffer.AsSpan(0, source.Length);
            source.AsSpan().CopyTo(bufferSpan);
            bufferSpan[0] = char.ToLowerInvariant(bufferSpan[0]);
            return new string(buffer, 0, source.Length);
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer);
        }
    }

    public static string Capitalize(this string? source)
    {
        if (source is null || source.Length == 0)
        {
            return string.Empty;
        }
        if (char.IsUpper(source[0]))
        {
            return source;
        }
        var buffer = ArrayPool<char>.Shared.Rent(source.Length);
        try
        {
            var bufferSpan = buffer.AsSpan(0, source.Length);
            source.AsSpan().CopyTo(bufferSpan);
            bufferSpan[0] = char.ToUpperInvariant(bufferSpan[0]);
            return new string(buffer, 0, source.Length);
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer);
        }
    }
}