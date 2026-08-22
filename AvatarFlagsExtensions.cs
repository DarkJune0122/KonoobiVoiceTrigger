using System.Runtime.CompilerServices;

namespace VoiceTrigger;

public static class AvatarFlagsExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsInactive(this AvatarFlags flags) => (flags & AvatarFlags.Active) == default;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsActive(this AvatarFlags flags) => (flags & AvatarFlags.Active) != default;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNonTriggered(this AvatarFlags flags) => (flags & AvatarFlags.TriggeredNormal) == default;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsTriggered(this AvatarFlags flags) => (flags & AvatarFlags.TriggeredNormal) != default;
}
