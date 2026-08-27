using SmartBi.Net.Collections;

namespace VoiceTrigger.Collections.Pooling;

public sealed class ConcurrentPool<T>(int max = Pool.DefaultCapacity) : Pool<T>
{
    readonly Stack<T> Stack = new(capacity: Math.Max(16, max / 4));
    SpinLock spinner = new();

    public override T Rent(Func<T> ctor)
    {
        bool isTaken = false;
        spinner.Enter(ref isTaken);
        try
        {
            if (Stack.TryPop(out var t))
                return t;
        }
        finally
        {
            if (isTaken) spinner.Exit();
        }
        return ctor(); // Initializes outside of a spin-lock.
    }

    public override void Return(T value)
    {
        if (value is null) return;

        bool isTaken = false;
        spinner.Enter(ref isTaken);
        try
        {
            if (Stack.Count < max)
                Stack.Push(value);
        }
        finally
        {
            if (isTaken) spinner.Exit();
        }
    }

    public override void Return(T value, Action<T> reset)
    {
        if (value is null) return;
        reset(value);

        bool isTaken = false;
        spinner.Enter(ref isTaken);
        try
        {
            if (Stack.Count < max)
                Stack.Push(value);
        }
        finally
        {
            if (isTaken) spinner.Exit();
        }
    }
}
