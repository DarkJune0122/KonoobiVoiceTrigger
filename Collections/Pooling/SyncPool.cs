namespace VoiceTrigger.Collections.Pooling;

public sealed class SyncPool<T>(int max = Pool.DefaultCapacity) : Pool<T>
{
    readonly Stack<T> Stack = [];
    public override T Rent(Func<T> ctor) => Stack.TryPop(out var result) ? result : ctor();
    public override void Return(T? value)
    {
        if (value is null) return;
        if (Stack.Count < max) Stack.Push(value);
    }

    public override void Return(T value, Action<T> reset)
    {
        if (value is null) return;
        if (Stack.Count < max)
        {
            Stack.Push(value);
            reset(value);
        }
    }
}
