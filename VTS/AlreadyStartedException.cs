using System.Runtime.CompilerServices;

namespace VoiceTrigger.VTS;

public sealed class AlreadyStartedException(string message) : Exception(message)
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowIf(bool state, object? target)
    {
        if (state) throw new AlreadyStartedException($"{target} Is already started!");
    }
}
