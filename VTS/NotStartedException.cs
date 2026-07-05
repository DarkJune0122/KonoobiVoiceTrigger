using System.Runtime.CompilerServices;

namespace VoiceTrigger.VTS;

public sealed class NotStartedException(string message) : Exception(message)
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowIf(bool state, object? target)
    {
        if (state) throw new NotStartedException($"{target} Was not started!");
    }
}
