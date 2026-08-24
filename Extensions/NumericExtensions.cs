using System.Runtime.CompilerServices;

namespace VoiceTrigger.Extensions;

public static class NumericExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsInfinite(this TimeSpan span) => span == Timeout.InfiniteTimeSpan;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsFinite(this TimeSpan span) => span != Timeout.InfiniteTimeSpan;
}
