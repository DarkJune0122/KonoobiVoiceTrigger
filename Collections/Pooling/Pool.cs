using System.Runtime.CompilerServices;

namespace VoiceTrigger.Collections.Pooling;

public static class Pool
{
    public const int DefaultCapacity = 256;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T Rent<T>() where T : new() => Pool<T>.Shared.Rent();
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T Rent<T>(Func<T> ctor) => Pool<T>.Shared.Rent(ctor);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static void Return<T>(T value) => Pool<T>.Shared.Return(value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static void Return<T>(T value, Action<T> reset) => Pool<T>.Shared.Return(value, reset);
}

public abstract class Pool<T>
{
    /// <summary>
    /// Thread-safe pool for objects of type <see cref="T"/>.
    /// </summary>
    public static readonly Pool<T> Shared = new ConcurrentPool<T>();
    public abstract T Rent(Func<T> ctor);
    public abstract void Return(T value);
    public abstract void Return(T value, Action<T> reset);
}
