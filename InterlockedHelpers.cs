using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace VoiceTrigger;

// We inherit CLSCompliant attributes for the original methods.
#pragma warning disable CS3021 // Type or member does not need a CLSCompliant attribute because the assembly does not have a CLSCompliant attribute
public static class InterlockedHelpers
{
    /// <inheritdoc cref="Interlocked.CompareExchange(ref ulong, ulong, ulong)"/>
    [CLSCompliant(false)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Read(ref ulong field) => Interlocked.CompareExchange(ref field, default, default);

    /// <inheritdoc cref="Interlocked.CompareExchange(ref uint, uint, uint)"/>
    [CLSCompliant(false)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Read(ref uint field) => Interlocked.CompareExchange(ref field, default, default);

    /// <inheritdoc cref="Interlocked.CompareExchange(ref float, float, float)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Read(ref float field) => Interlocked.CompareExchange(ref field, default, default);

    /// <inheritdoc cref="Interlocked.CompareExchange(ref object?, object?, object?)"/>
    [return: NotNullIfNotNull(nameof(field))]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object? Read(ref object? field) => Interlocked.CompareExchange(ref field, default, default);

    /// <inheritdoc cref="Interlocked.CompareExchange(ref nuint, nuint, nuint)"/>
    [CLSCompliant(false)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nuint Read(ref nuint field) => Interlocked.CompareExchange(ref field, default, default);

    /// <inheritdoc cref="Interlocked.CompareExchange(ref nint, nint, nint)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nint Read(ref nint field) => Interlocked.CompareExchange(ref field, default, default);

    /// <inheritdoc cref="Interlocked.CompareExchange(ref short, short, short)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short Read(ref short field) => Interlocked.CompareExchange(ref field, default, default);

    /// <inheritdoc cref="Interlocked.CompareExchange(ref int, int, int)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Read(ref int field) => Interlocked.CompareExchange(ref field, default, default);

    /// <inheritdoc cref="Interlocked.CompareExchange(ref ushort, ushort, ushort)"/>
    [CLSCompliant(false)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort Read(ref ushort field) => Interlocked.CompareExchange(ref field, default, default);

    /// <inheritdoc cref="Interlocked.CompareExchange{T}(ref T?, T?, T?)"/>
    [return: NotNullIfNotNull(nameof(field))]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? Read<T>(ref T? field) => Interlocked.CompareExchange(ref field, default, default);

    /// <inheritdoc cref="Interlocked.CompareExchange(ref sbyte, sbyte, sbyte)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static sbyte Read(ref sbyte field) => Interlocked.CompareExchange(ref field, default, default);

    /// <inheritdoc cref="Interlocked.CompareExchange(ref byte, byte, byte)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte Read(ref byte field) => Interlocked.CompareExchange(ref field, default, default);

    /// <inheritdoc cref="Interlocked.CompareExchange(ref double, double, double)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Read(ref double field) => Interlocked.CompareExchange(ref field, default, default);

    /// <inheritdoc cref="Interlocked.CompareExchange(ref long, long, long)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long Read(ref long field) => Interlocked.CompareExchange(ref field, default, default);
}
#pragma warning restore CS3021 // Type or member does not need a CLSCompliant attribute because the assembly does not have a CLSCompliant attribute