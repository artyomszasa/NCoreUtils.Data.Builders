using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace NCoreUtils.Data.Builders.Internal;

internal static class Check
{
#if NET6_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void NotNull<T>([NotNull] T? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        where T : notnull
        => ArgumentNullException.ThrowIfNull(argument, paramName);
#else
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void NotNull<T>([NotNull] T? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        where T : notnull
    {
        if (argument is null)
        {
            throw new ArgumentNullException(paramName);
        }
    }
#endif

#if NET8_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GreaterThanOrEqual(int value, int other, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        => ArgumentOutOfRangeException.ThrowIfLessThan(value,    other, paramName);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LessThan(int value, int other, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        => ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(value,    other, paramName);
#else
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GreaterThanOrEqual(int value, int other, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value < other)
        {
            throw new ArgumentOutOfRangeException(paramName);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LessThan(int value, int other, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value >= other)
        {
            throw new ArgumentOutOfRangeException(paramName);
        }
    }
#endif

}