using System.Runtime.CompilerServices;

namespace VoiceTrigger.Collections.Pooling;

public static class PoolExtensions
{
    /// <inheritdoc cref="Pool{T}.Rent(Func{T})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Rent<T>(this Pool<T> pool) where T : new() => pool.Rent(static () => new());
}